using Microsoft.Extensions.AI;
using Showcase.Shared.AIExtensions;
using System.Reflection;

namespace Showcase.VoiceRagAgent;

public class AcsCallHandler : IAIToolHandler
{

    public AcsCallHandler()
    {

    }
    public IEnumerable<AIFunction> GetAITools()
    {
        var tools = GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<AIToolAttribute>() is not null)
            .Select(tool =>
            {
                var attribute = tool.GetCustomAttribute<AIToolAttribute>();

                return AIFunctionFactory.Create(tool, this, options: new()
                {
                    Name = attribute?.Name,
                    Description = attribute?.Description
                });
            });
        foreach (var tool in tools)
        {
            yield return tool;
        }
    }

    [AITool(name: "weather", description: "Gets the weather")]
    public static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
}
