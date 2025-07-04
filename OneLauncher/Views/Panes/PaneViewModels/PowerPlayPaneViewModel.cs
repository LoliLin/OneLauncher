﻿using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Net.ConnectToolPower; // 确保这是你的命名空间
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels
{
    public enum PowerPlayMode { Host, Join }

    public partial class PowerPlayPaneViewModel : BaseViewModel
    {
        private readonly MainPower mainPower;
        private readonly IConnectService connectService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanStart))]
        [NotifyPropertyChangedFor(nameof(CanStop))]
        private bool isConnected = false;

        [ObservableProperty]
        private bool isHostModeChecked = true;

        [ObservableProperty]
        private bool isJoinModeChecked = false;

        [ObservableProperty] private string hostRoomCode = string.Empty;
        [ObservableProperty] private string joinRoomCode = string.Empty;
        [ObservableProperty] private string joinPort = string.Empty;
        [ObservableProperty] private string localServerAddress = string.Empty;
        [ObservableProperty] private string logOutput = string.Empty;

        [ObservableProperty]
        private UserVersion selectedHostVersion;
        [ObservableProperty]
        public List<UserVersion> installedVersions;

        private readonly StringBuilder logBuilder = new();

        public bool CanStart => !isConnected;
        public bool CanStop => isConnected;
        public PowerPlayPaneViewModel()
        {
            connectService = new P2PMode(Init.ConnentToolPower);
            mainPower = Init.ConnentToolPower; 
            InstalledVersions = Init.ConfigManger.config.VersionList;
            // 订阅日志事件
            mainPower.CoreLog += OnCoreLogReceived;

            // 模式切换逻辑
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsHostModeChecked) && IsHostModeChecked)
                {
                    SetProperty(ref isJoinModeChecked, false, nameof(IsJoinModeChecked));
                }
                else if (e.PropertyName == nameof(IsJoinModeChecked) && IsJoinModeChecked)
                {
                    SetProperty(ref isHostModeChecked, false, nameof(IsHostModeChecked));
                }
            };
        }

        [RelayCommand]
        private async Task Host()
        {
            try
            {
                if (SelectedHostVersion == null)
                {
                    // 异常处理修改: 抛出OlanException
                    throw new OlanException("未能创建房间", "请先在下拉框中选择一个要进行联机的游戏版本。", OlanExceptionAction.Warning);
                }

                string p2pNodeName = "OLANNODE" + RandomNumberGenerator.GetInt32(100000, 1000000000);
                string combinedInfo = $"{p2pNodeName}:{SelectedHostVersion.VersionID}";
                string finalRoomCode = TextHelper.Base64Encode(combinedInfo);

                HostRoomCode = finalRoomCode;
                IsConnected = true;
                _=version.EasyGameLauncher(SelectedHostVersion);
                await connectService.StartAsHost(p2pNodeName, null);
            }
            catch (OlanException olanEx)
            {
                await OlanExceptionWorker.ForOlanException(olanEx);
            }
            catch (Exception ex)
            {
                await OlanExceptionWorker.ForUnknowException(ex);
            }
        }

        [RelayCommand]
        private async Task JoinAndLaunch()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(JoinRoomCode))
                    throw new OlanException("输入无效", "必须输入房间码。", OlanExceptionAction.Warning);

                string decodedInfo = TextHelper.Base64Decode(JoinRoomCode);
                if (string.IsNullOrEmpty(decodedInfo) || !decodedInfo.Contains(':'))
                    throw new OlanException("加入失败", "无效的房间码格式。", OlanExceptionAction.Error);

                var parts = decodedInfo.Split(':', 2);
                string p2pNodeName = parts[0];
                string versionId = parts[1];

                // 数据源修改: 从全局配置中查找版本
                UserVersion? targetVersion = Init.ConfigManger.config.VersionList
                    .FirstOrDefault(v => v.VersionID == versionId);

                if (targetVersion == null)
                    throw new OlanException("加入失败", $"你没有安装房主所使用的游戏版本 ({versionId})。", OlanExceptionAction.Error);

                if (!int.TryParse(JoinPort, out int port) || port is < 1 or > 65535)
                    throw new OlanException("输入无效", "端口号必须是 1-65535 之间的数字。", OlanExceptionAction.Warning);

                // --- 业务逻辑 ---
                int localPort = Tools.GetFreeTcpPort();
                LocalServerAddress = $"127.0.0.1:{localPort}";
                IsConnected = true;
                _=version.EasyGameLauncher(targetVersion, serverInfo: new ServerInfo
                {
                    Ip = "127.0.0.1",
                    Port = localPort.ToString()
                });
                await connectService.Join(null, p2pNodeName, localPort, port, null, null, null);

                LogMessage($"P2P连接成功，准备启动游戏: {targetVersion.VersionID}");   
            }
            catch (OlanException olanEx)
            {
                await OlanExceptionWorker.ForOlanException(olanEx);
                Stop(); 
            }
            catch (Exception ex)
            {
                await OlanExceptionWorker.ForUnknowException(ex);
                Stop(); 
            }
        }
        [RelayCommand]
        private Task CopyCode() =>
            TopLevel.GetTopLevel(MainWindow.mainwindow)?.Clipboard?.SetTextAsync(IsHostModeChecked ? HostRoomCode : JoinRoomCode);
        
        [RelayCommand]
        private void Stop()
        {
            connectService.Dispose();
            IsConnected = false;
            HostRoomCode = string.Empty;
            LocalServerAddress = string.Empty;
            LogMessage("连接已断开。");
        }

        private void OnCoreLogReceived(string logMessage) => Dispatcher.UIThread.Post(() => LogMessage(logMessage));
        private void LogMessage(string message) { logBuilder.AppendLine(message); LogOutput = logBuilder.ToString(); }
    }
}