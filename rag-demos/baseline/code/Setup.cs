#!import ../../utils/csharp/SemanticKernelService.cs 
#!import ../../utils/csharp/AzureAISearchSettings.cs 
#!import ../../utils/csharp/CosmosNoSqlService.cs 

#r "nuget: Azure.AI.OpenAI, 2.0.0-beta.3"
#r "nuget: Azure.Search.Documents, 11.6.0"
#r "nuget: Azure.Identity, 1.12.0"
#r "nuget: Microsoft.Azure.Cosmos, 3.44.0-preview"
#r "nuget: Microsoft.Data.Analysis, 0.21.0"
#r "nuget: System.Linq.Async, 6.0.1"
#r "nuget: CsvHelper, 33.0.1"

using System.Globalization;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Data.Analysis;
using CsvHelper;
using CsvHelper.Configuration;
using Azure;
using Azure.Identity;


using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Search.Documents.Indexes.Models;


var skService = new SemanticKernelService();
var cosmosNoSqlService = new CosmosNoSqlService();

var skBuilder = skService.GetMonitoredKernelBuilder();
var (searchEndpoint, searchKey, searchIndex) = AzureAISearchSettings.LoadFromFile("env.json");


var searchIndexClient = new SearchClient(new Uri(searchEndpoint), searchIndex, new AzureKeyCredential(searchKey));