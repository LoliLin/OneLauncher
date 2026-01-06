using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLauncher.Core.Helper.Models;
public class ServerConfig
{
    [JsonPropertyName("server")]
    public string ServerIP {  get; set; }
    [JsonPropertyName("port")]
    public string ServerPort { get; set; }
    [JsonPropertyName("default_instance")]
    public string DefaultInstanceId { get; set; }
    public ServerConfig(string serverIP, string serverPort, string defaultInstanceId)
    {
        ServerIP = serverIP;
        ServerPort = serverPort;
        DefaultInstanceId = defaultInstanceId;
    }
}
