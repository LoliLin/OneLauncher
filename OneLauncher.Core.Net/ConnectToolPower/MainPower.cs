﻿using OneLauncher.Core.Helper;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Core.Net.ConnectToolPower;

/// <summary>
/// 负责与 Minecraft Connect Tool 的核心文件交互
/// </summary>
public class MainPower : IDisposable
{
    private const string CoreExecutableName = "main.exe";
    private const string CoreUrl = "https://gitee.com/linfon18/minecraft-connect-tool-api/raw/master/mainnew.exe";
    private const string CoreMd5 = "08160296509deac13e7d12c8754de9ef";

    private readonly string coreDirectory;
    private readonly string coreFilePath;
    private Process? coreProcess;

    public event Action<string>? CoreLog;

    private MainPower(string coreDirectory,string coreFileName)
    {
        this.coreDirectory = coreDirectory;
        coreFilePath = coreFileName;
    }
    public static async Task<MainPower> InitializationAsync(HttpClient? client = null)
    {
        var httpClient = client ?? new HttpClient();
        string coreDirectory = Path.Combine(Init.BasePath,"install");
        string coreFileName = Path.Combine(coreDirectory, CoreExecutableName);
        Directory.CreateDirectory(coreDirectory);
        // 下载核心组件
        if (File.Exists(coreFileName))
            goto WhenDone;
        var response = await httpClient.GetAsync(CoreUrl);
        response.EnsureSuccessStatusCode();
        using (var fs = new FileStream(coreFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            await response.Content.CopyToAsync(fs);
        // 校验
        string? currentMd5 = await Tools.GetFileMD5Async(coreFileName);
        if (currentMd5 == null)
            throw new OlanException("无法初始化联机模块","在对核心程序校验时发生意外错误",OlanExceptionAction.Error);
        if (currentMd5 != CoreMd5)
            throw new OlanException("无法初始化联机模块",$"【无法校验核心组件MD5】{Environment.NewLine}警告：您当前的网络环境可能不安全",OlanExceptionAction.FatalError);
        WhenDone:
        if (client == null)
            httpClient.Dispose();
        return new MainPower(coreDirectory,coreFileName);
    }

    /// <summary>
    /// 异步启动核心进程。
    /// </summary>
    /// <param name="arguments">启动参数。</param>
    public async Task LaunchCore(string arguments)
    {
        if (coreProcess != null && !coreProcess.HasExited)
        {
            throw new InvalidOperationException("P2P核心已经在运行中。");
        }

        coreProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = coreFilePath,
                Arguments = arguments,
                WorkingDirectory = coreDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };
        // 解决作者写的屎山代码认配置文件不认命令行的问题
        File.Delete(Path.Combine(coreDirectory,"config.json"));
        Directory.Delete(Path.Combine(coreDirectory,"log"),true);
        coreProcess.OutputDataReceived += (s, e) => 
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Debug.WriteLine(e.Data);
                CoreLog?.Invoke(e.Data);
            }
        };
        coreProcess.ErrorDataReceived += (s, e) => 
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Debug.WriteLine(e.Data);
                CoreLog?.Invoke(e.Data);
            }
        };

        coreProcess.Start();
        coreProcess.BeginOutputReadLine();
        coreProcess.BeginErrorReadLine();
        await coreProcess.WaitForExitAsync();
    }

    /// <summary>
    /// 停止核心进程。
    /// </summary>
    public void StopCore()
    {
        if (coreProcess != null && !coreProcess.HasExited)
        {
            try
            {
                coreProcess.Kill(true); 
                coreProcess.WaitForExit();
            }
            finally
            {
                coreProcess.Dispose();
                coreProcess = null;
            }
        }
    }

    public void Dispose() => StopCore();
}