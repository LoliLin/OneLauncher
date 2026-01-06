using OneLauncher.Core.Global;
using OneLauncher.Core.Helper;
using OneLauncher.Core.Helper.Models;
using OneLauncher.Core.Launcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Console;
public class Boot
{
    public static async Task RunBoot(string[] args)
    {
        try
        {
            await Init.Initialize();
            switch (args[0])
            {
                case "--quicklyPlay":
                    await new GameLauncher().Play(args[1]);
                    break;
                case "--joinServer":
                    var server = Init.ConfigManger.Data.FavoriteServers.GetValueOrDefault(args[1]);
                    if (server == null) break;
                    var game = Init.GameDataManger.Data.Instances.GetValueOrDefault(server.DefaultInstanceId);
                    if (game == null) break;
                    await new GameLauncher().Play(game, serverInfo: new ServerInfo()
                    {
                        Ip = server.ServerIP,
                        Port = server.ServerPort
                    });
                    break;
                case "--releaseMemory":
                    await ReleaseMemory.OptimizeAsync();
                    break;
            }
        }
        catch (Exception e) { 
            Environment.FailFast(e.ToString());
        }
    }
}
