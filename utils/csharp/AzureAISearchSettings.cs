using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public static class AzureAISearchSettings
{
    private const string SearchEndpoint = "AzureAISearchEndpoint";
    private const string SearchKey = "AzureAISearchKey";
    private const string SearchIndex = "AzureAISearchIndex";
    // Load settings from file
    public static (string searchEndpoint, string searchKey, string searchIndex)
        LoadFromFile(string configFile)
    {
        var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(configFile));
        string endpoint = config[SearchEndpoint];
        string key = config[SearchKey];
        string index = config[SearchIndex];
        
        return (endpoint, key, index);
    }

}