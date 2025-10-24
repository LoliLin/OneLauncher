﻿using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneLauncher.Codes;
using OneLauncher.Core.Global;
using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Views.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OneLauncher.Views.Panes.PaneViewModels;

internal partial class EditGameDataPaneViewModel : BaseViewModel
{
    private readonly GameDataManager _gameDataManager;
    private readonly AccountManager _accountManager;
    private readonly GameData editingGameData;
    private readonly Action _onCloseCallback;
    [ObservableProperty] private string instanceName;
    [ObservableProperty] private Bitmap currentIcon;

    public EditGameDataPaneViewModel(
        GameData gameData,
        GameDataManager gameDataManager,
        AccountManager accountManager,
        Action onCloseCallback
        )
    {
        this._gameDataManager = gameDataManager;
        this._accountManager = accountManager;
        _onCloseCallback = onCloseCallback;
        editingGameData = gameData;
        InstanceName = gameData.Name;
        AvailableTags = _gameDataManager.Data.Tags.Values.ToList();
        Guid? tagId = _gameDataManager.Data.TagMap.GetValueOrDefault(editingGameData.InstanceId);
        if (tagId == null)
            SelectedTag = null;
        else SelectedTag = 
            _gameDataManager.Data.Tags.GetValueOrDefault((Guid)tagId); // 找到标签
        AvailableUsers = _accountManager.GetAllUsers().ToList();
        SelectedUser = _accountManager.GetUser(editingGameData.DefaultUserModelID);
        if (SelectedUser == null)
        {
            // 如果用户神奇消失了，则帮他回退到默认用户
            SelectedUser = _accountManager.GetDefaultUser();
            editingGameData.DefaultUserModelID = SelectedUser.UserID;
        }

        CurrentIcon = GameDataItem.GetGameDataIcon(gameData);
    }

    private void UpdateGameData()
    {
        try
        {
            _gameDataManager.Data.Instances[editingGameData.InstanceId] = editingGameData;
        }
        catch(KeyNotFoundException e)
        {
            throw new OlanException(
                "无法更新游戏数据",
                "尝试更新一个不存在的游戏数据实例",
                OlanExceptionAction.Error, e);
        }
    }
    #region 默认登入用户选项
    [ObservableProperty] List<UserModel> availableUsers;
    [ObservableProperty] UserModel? selectedUser;
    partial void OnSelectedUserChanged(UserModel? value)
    {
        if (value == null)
            return;
        editingGameData.DefaultUserModelID = SelectedUser!.UserID;
        _=_gameDataManager.Save(); 
    }
    #endregion
    #region 标签选项
    [ObservableProperty] List<GameDataTag> availableTags;
    [ObservableProperty] GameDataTag? selectedTag;
    [RelayCommand]
    private Task ResetLabelOption()
    {
        SelectedTag = null;
        return _gameDataManager.RemoveTagFromInstanceAsync(editingGameData.InstanceId);
    }
    partial void OnSelectedTagChanged(GameDataTag? value)
    {
        if (value == null)
            return;
        _=_gameDataManager.SetTagForInstance(editingGameData.InstanceId, SelectedTag.ID);
    }
    #endregion
    #region 高级选项
    [RelayCommand]
    private void OpenInstanceFolder()
    {
        string modsPath = Path.Combine(editingGameData.InstancePath);
        Tools.OpenFolder(modsPath);
    }
    [RelayCommand]
    private void AddToQuicklyPlay()
    {
        File.WriteAllTextAsync(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),$"快速启动 - {editingGameData.Name}"+
            #if WINDOWS
            ".bat"
            #else
            ".sh"
            #endif
            ), 
            $"{Environment.ProcessPath} --quicklyPlay {editingGameData.InstanceId}"
            );
    }
    [RelayCommand]
    private async Task CopyThisInstance()
    {
        GameData newGameData = new GameData(
                $"{editingGameData.Name} - 副本",
                editingGameData.VersionId,
                editingGameData.ModLoader,
                editingGameData.DefaultUserModelID);
        try
        {
            await _gameDataManager.AddGameDataAsync(newGameData);
            await Tools.CopyDirectoryAsync(editingGameData.InstancePath, newGameData.InstancePath, CancellationToken.None);
        }
        catch(OlanException e)
        {
            await OlanExceptionWorker.ForOlanException(e,() => _=CopyThisInstance());
        }
        catch(Exception e)
        {
            await OlanExceptionWorker.ForUnknowException(e,() => _=CopyThisInstance());
        }
        //WeakReferenceMessenger.Default.Send(
        //    new GameDataPageDisplayListRefreshMessage());
        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage("已以此拷贝实例！",NotificationType.Success));
    }
    #endregion
    #region 基本选项
    [RelayCommand]
    private async Task ChangeIcon()
    {
        var topLevel = TopLevel.GetTopLevel(MainWindow.mainwindow);
        if (topLevel?.StorageProvider is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择一个新的图标",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        var selectedFile = files?.FirstOrDefault();
        if (selectedFile == null) return;

        string imageDir = Path.Combine(editingGameData.InstancePath, ".olc");
        Directory.CreateDirectory(imageDir);
        string destPath = Path.Combine(imageDir, "customicon");

        { // 确保文件流被释放
            await using var sourceStream = await selectedFile.OpenReadAsync();
            await using var destStream = File.Create(destPath);
            await sourceStream.CopyToAsync(destStream);
        }

        CurrentIcon = new Bitmap(destPath); // 显示预览
        UpdateGameData();

        //WeakReferenceMessenger.Default.Send(
        //    new GameDataPageDisplayListRefreshMessage());

        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage($"已更改实例“{editingGameData.Name}”的图标！"));
    }
    [RelayCommand]
    private void Save()
    {
        editingGameData.Name = InstanceName;
        //_gameDataManager.Data.Instances.GetValueOrDefault(editingGameData.InstanceId);
        if (SelectedTag != null)
            _gameDataManager.SetTagForInstance(editingGameData.InstanceId, SelectedTag.ID);
        _=_gameDataManager.Save();

        //WeakReferenceMessenger.Default.Send(new GameDataPageDisplayListRefreshMessage());
        _onCloseCallback();
        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage($"实例“{InstanceName}”已保存"));
    }
    [RelayCommand]
    private void DeleteInstance()
    {
        // 未来可以加一个对话框确认
        _ = _gameDataManager.RemoveGameDataAsync(editingGameData.InstanceId);

        try
        {
            if (Directory.Exists(editingGameData.InstancePath))
                Directory.Delete(editingGameData.InstancePath, true);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(
                new MainWindowShowFlyoutMessage($"删除文件夹'{editingGameData.InstancePath}'失败: {ex.Message}", NotificationType.Error));
        }

        //WeakReferenceMessenger.Default.Send(
        //    new GameDataPageDisplayListRefreshMessage()); // 重新加载列表
        WeakReferenceMessenger.Default.Send(
            new MainWindowShowFlyoutMessage($"已删除实例“{editingGameData.Name}”！", NotificationType.Success));
        _onCloseCallback();
    }
    [RelayCommand]
    private void Cancel()
    {
        _onCloseCallback();
    }
    #endregion
}