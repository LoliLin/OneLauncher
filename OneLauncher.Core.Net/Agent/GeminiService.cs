using System;
using System.Collections.Generic;
using System.Text;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Identity.Client;
using OneLauncher.Core.Global;
namespace OneLauncher.Core.Net.Agent;
public class GeminiService
{
    private const string ModelName = "gemini-3.0-flash";
    private readonly Client client;
    public GeminiService()
    {
        if (Init.ConfigManger.Data.OlanSettings.ApiKey == null)
            throw new OlanException("无法调用Gemini", "没有APIKEY");
        client = new Client(apiKey:Init.ConfigManger.Data.OlanSettings.ApiKey);
    }
    public async IAsyncEnumerable<string> SeedMessage()
    {
        await foreach(var c in client.Models.GenerateContentStreamAsync(
            ModelName,new Content
            {

            },
            new GenerateContentConfig
            {
                SystemInstruction = new Content { Parts = new List<Part> { new Part { Text = AgentPrompts.FixProblemInstruction } } },
                Tools = new List<Tool>
                {

                }
            }
            ))
            yield return c.Candidates[0].Content.Parts[0].Text;
        yield break;
    }
}
