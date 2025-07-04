﻿using OneLauncher.Core.Helper;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace OneLauncher.Core.Net.ConnectToolPower;

public class P2PMode : IConnectService
{
    const string defaultToken = "17073157824633806511";
    const string defaultAppName = "OneLauncherConnentService";
    const string defaultDestIP = "127.0.0.1";
    private MainPower mainPower;
    public P2PMode(MainPower mainPower)
    {
        if (Init.systemType != SystemType.windows)
            throw new OlanException("无法初始化联机模块","你的操作系统不受支持");
        this.mainPower = mainPower;
    }
    public void Dispose()
        => this.mainPower.StopCore();

    public Task Join(string? nodeName, string peerNodeName, int? sourcePort, int destPort, string? destIp, string? appName, string? token)
    {
        // 获取可用端口
        int localPort = sourcePort ?? Tools.GetFreeTcpPort();

        string localNodeName = nodeName ?? "OLANNODE" + RandomNumberGenerator.GetInt32(100000, 1000000000).ToString();
        string finalAppName = (appName ?? defaultAppName) + localPort;

        var args = new StringBuilder();
        args.Append($"-node \"{localNodeName}\" ");
        args.Append($"-appname \"{finalAppName}\" ");
        args.Append($"-peernode \"{peerNodeName}\" ");
        args.Append($"-dstip \"{destIp ?? defaultDestIP}\" ");
        args.Append($"-dstport {destPort} ");
        args.Append($"-srcport {localPort} ");
        args.Append($"-token \"{token ?? defaultToken}\" ");

        return mainPower.LaunchCore(args.ToString());
    }

    public Task StartAsHost(string? nodeName, string? token)
    {
        string? node = nodeName;
        if (nodeName == null)
                node = "OLANNODE" + RandomNumberGenerator.GetInt32(100000,1000000000).ToString();
        return mainPower.LaunchCore($"-node \"{node}\" -token \"{token ?? defaultToken}\"");
    }
}
