using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OneLauncher.Codes;
using OneLauncher.Core;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Minecraft;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
namespace OneLauncher.Views;

public partial class version : UserControl
{
    public version()
    {
        InitializeComponent();
        this.DataContext = viewmodel = new VersionPageViewModel();
    }
    internal readonly VersionPageViewModel viewmodel;
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
#if DEBUG
        if (Design.IsDesignMode)
            return;
#endif
        try
        {
            var tempVersoinList = new List<VersionItem>(Init.ConfigManger.config.VersionList.Count);
            for (int i = 0; i < Init.ConfigManger.config.VersionList.Count; i++)
            {
                tempVersoinList.Add(new VersionItem(
                    Init.ConfigManger.config.VersionList[i],
                    i
                    ));
            }
            navVL.ItemsSource = tempVersoinList;
        }
        catch (NullReferenceException ex)
        {
            throw new OlanException(
                "内部异常",
                "配置文件特定部分版本列表部分为空，这可能是新版和旧版配置文件不兼容导致的",
                OlanExceptionAction.FatalError,
                ex,
               () =>
               {
                   File.Delete(Path.Combine(Init.BasePath, "config.json"));
                   Init.Initialize();
               }
                );
        }
    }
    /// <summary>
    /// 真·一键启动游戏函数
    /// </summary>
    /// <returns>异步任务Task</returns>
    public static Task EasyGameLauncher(
        UserVersion LaunchGameInfo,
        bool UseGameTasker = false,
        UserModel loginUserModel = null,
        ServerInfo? serverInfo = null
        )
    {
        // 用多线程而不是异步，否则某些特定版本会阻塞
        MainWindow.mainwindow.ShowFlyout("正在启动游戏...");
        var game = new Game();
        game.GameStartedEvent += () => MainWindow.mainwindow.ShowFlyout("游戏已启动！");
        game.GameClosedEvent += () => MainWindow.mainwindow.ShowFlyout("游戏已关闭！");

       return Task.Run(() => game.LaunchGame(
            LaunchGameInfo.VersionID, 
            loginUserModel ?? Init.ConfigManger.config.DefaultUserModel,
            LaunchGameInfo.preferencesLaunchMode.LaunchModType,
            LaunchGameInfo.IsVersionIsolation, 
            UseGameTasker,serverInfo));
    }
}