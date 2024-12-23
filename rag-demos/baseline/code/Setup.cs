#!import ../../utils/csharp/SemanticKernelService.cs 
#!import ../../utils/csharp/AzureAISearchSettings.cs 
#!import ../../utils/csharp/CosmosNoSqlService.cs 

#r "nuget: Azure.AI.OpenAI, 2.0.0"
#r "nuget: Azure.Search.Documents, 11.6.0"
#r "nuget: Azure.Identity, 1.12.0"
#r "nuget: Microsoft.Azure.Cosmos, 3.44.0-preview.1"
#r "nuget: Microsoft.Data.Analysis, 0.21.0"
#r "nuget: System.Linq.Async, 6.0.1"
#r "nuget: CsvHelper, 33.0.1"
#r "nuget: Plotly.NET.CSharp, 0.13.0"
#r "nuget: Plotly.NET, 5.1.0"
#r "nuget: Microsoft.ML, 4.0.0-preview.24378.1"
#r "nuget: Microsoft.ML.TimeSeries, 4.0.0-preview.24378.1"
#r "nuget: MathNet.Numerics, 6.0.0-beta1"
#r "nuget: QLNet, 1.13.1-preview.21"
#r "nuget: Microsoft.ML.Probabilistic.Compiler, 0.4.2403.801"
#pragma warning disable SKEXP0001, SKEXP0010
#nullable enable

using System.Globalization;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Data.Analysis;
using CsvHelper;
using CsvHelper.Configuration;
using Azure;
using Azure.Identity;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Linq;
using System.Text.RegularExpressions;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Search.Documents.Indexes.Models;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;


// using Microsoft.SemanticKernel.Data;  If using the current implementation of Semantic Kernel Vector or Memory data stores.
using IndexKind = Microsoft.Azure.Cosmos.IndexKind;
using System.Reflection;
using Azure.Core.Serialization;
using Container = Microsoft.Azure.Cosmos.Container;
using System.Collections.ObjectModel;

var skService = new SemanticKernelService();
var cosmosNoSqlService = new CosmosNoSqlService();

var skBuilder = skService.GetMonitoredKernelBuilder();
var (searchEndpoint, searchKey, searchIndex) = AzureAISearchSettings.LoadFromFile("env.json");


var searchIndexClient = new SearchClient(new Uri(searchEndpoint), searchIndex, new AzureKeyCredential(searchKey));