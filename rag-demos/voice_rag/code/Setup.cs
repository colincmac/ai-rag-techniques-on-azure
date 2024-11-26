#r "nuget: Azure.AI.OpenAI, 2.1.0-beta.1"
#r "nuget: Azure.Identity, 1.13.0-beta.2"
#r "nuget: NAudio, 2.2.1"
using System.Text.Json;
using System.IO;

var configFile = "env.json";
public record Config(string AcsPhoneNumber, string AcsConnectionString, string AzureOpenAIEndpoint, string AzureOpenAIKey, string AzureOpenAIRealtimeDeployName, string AzureOpenAIEmbeddingDeployName, string ApplicationInsightsConnectionString);
var fileContent = File.ReadAllText(configFile);
var configuration = JsonSerializer.Deserialize<Config>(fileContent);