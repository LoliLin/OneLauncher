﻿using OneLauncher.Core.Global;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Minecraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Downloader.DownloadMinecraftProviders;

public partial class DownloadInfo
{
    // 基本信息
    public string ID => VersionDownloadInfo.ID ?? VersionInstallInfo.VersionID ?? UserInfo.VersionId;
    public string GameRootPath { get; init; }
    
    // 核心下载组件与配置
    public Download DownloadTool { get; init; }
    public VersionInfomations VersionMojangInfo { get; init; }

    // 用户意图与版本选择
    public UserVersion VersionInstallInfo { get; init; } 
    public GameData UserInfo { get; init; } 
    public VersionBasicInfo VersionDownloadInfo { get; init; }

    // 可选参数
    // 下面三个是预留的参数
    public string SpecifiedFabricVersion { get; init; }
    public string SpecifiedForgeVersion { get; init; }
    public string SpecifiedNeoForgeVersion { get; init; }

    public bool IsAllowToUseBetaNeoforge { get; init; }
    public bool IsUseRecommendedToInstallForge { get; init; }
    public bool IsDownloadFabricWithAPI { get; init; }
    public bool AndJava { get; init; }
}