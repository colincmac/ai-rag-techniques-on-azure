
#r "nuget: Microsoft.SemanticKernel, 1.19.0"
#r "nuget: Microsoft.SemanticKernel.Connectors.OpenAI, 1.19.0"
// #r "nuget: Microsoft.SemanticKernel.Plugins.Memory, 1.18.1-alpha"
#r "nuget: Microsoft.SemanticKernel.Planners.OpenAI, 1.19.0-preview"
#r "nuget: Microsoft.ML.Tokenizers, 0.22.0-preview.24378.1"
#r "nuget: Azure.Monitor.OpenTelemetry.Exporter, 1.3.0"
#r "nuget: Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL, 1.19.0-alpha"

#!import ../../utils/csharp/ServiceFromConfig.cs 

// Disabling Experimental warnings
#pragma warning disable SKEXP0001,SKEXP0010

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.ML.Tokenizers;
using Kernel = Microsoft.SemanticKernel.Kernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL;
using Microsoft.Azure.Cosmos;

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

public class SemanticKernelService : ServiceFromConfig<SemanticKernelService.Config>
{
    private const string AzureAIServiceKey = "AzureOpenAI";
    private IKernelBuilder defaultKernelBuilder;
    public Tokenizer s_tokenizer;
    public OpenAIPromptExecutionSettings functionCallingPromptExecutionSettings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
    
    private static readonly ActivitySource s_activitySource = new("SemanticKernel");


    public SemanticKernelService(string configFile = DEFAULT_CONFIG_FILE): base(configFile)
    {
        s_tokenizer = TiktokenTokenizer.CreateForModel(Configuration.AzureOpenAIEmbeddingModelName);
        defaultKernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                endpoint: Configuration.AzureOpenAIEndpoint,
                apiKey: Configuration.AzureOpenAIKey,
                deploymentName: Configuration.AzureOpenAIChatCompletionDeployName,
                serviceId: $"{AzureAIServiceKey}:{Configuration.AzureOpenAIChatCompletionDeployName}")
            .AddAzureOpenAITextEmbeddingGeneration(
                endpoint: Configuration.AzureOpenAIEndpoint,
                apiKey: Configuration.AzureOpenAIKey,
                deploymentName: Configuration.AzureOpenAIEmbeddingDeployName,
                serviceId: $"{AzureAIServiceKey}:{Configuration.AzureOpenAIEmbeddingDeployName}");
    }


    public IKernelBuilder GetDefaultKernelBuilder()
    {
        return defaultKernelBuilder;
    }

    public IKernelBuilder GetMonitoredKernelBuilder(LogLevel minimumLogLevel = LogLevel.Information)
    {
        // https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Demos/TelemetryWithAppInsights/Program.cs
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("TelemetryExample");
        using var traceProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("Microsoft.SemanticKernel*")
            .AddSource("Telemetry.Example")
            .AddAzureMonitorTraceExporter(options => options.ConnectionString = Configuration.ApplicationInsightsConnectionString)
            .Build();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("Microsoft.SemanticKernel*")
            .AddAzureMonitorMetricExporter(options => options.ConnectionString =  Configuration.ApplicationInsightsConnectionString)
            .Build();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            // Add OpenTelemetry as a logging provider
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                options.AddAzureMonitorLogExporter(options => options.ConnectionString =  Configuration.ApplicationInsightsConnectionString);
                // Format log messages. This is default to false.
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var builder = GetDefaultKernelBuilder();

        builder.Services.AddSingleton(loggerFactory);
        return builder;
    }
    public record Config(string AzureOpenAIEndpoint, string AzureOpenAIKey, string AzureOpenAIChatCompletionDeployName, string AzureOpenAIEmbeddingDeployName, string AzureOpenAIEmbeddingModelName, string ApplicationInsightsConnectionString);
}
