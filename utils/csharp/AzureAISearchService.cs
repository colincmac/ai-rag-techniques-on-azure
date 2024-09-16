using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;

public class AzureAISearchService
{
    private const string SearchEndpoint = "AzureAISearchEndpoint";
    private const string SearchKey = "AzureAISearchKey";
    private const string SearchIndex = "AzureAISearchIndex";

    public SearchClient searchIndexClient;
    public AzureAISearchService(string searchEndpoint, string searchKey, string searchIndex)
    {
        searchIndexClient = new SearchClient(new Uri(searchEndpoint), searchIndex, new AzureKeyCredential(searchKey));

    }
    // Load settings from file
    public AzureAISearchService CreateFromEnvFile(string configFile)
    {
        var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(configFile));
        string endpoint = config[SearchEndpoint];
        string key = config[SearchKey];
        string index = config[SearchIndex];
        
        return new AzureAISearchService(endpoint, key, index);
    }

}