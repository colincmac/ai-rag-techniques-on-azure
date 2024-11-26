using Azure.AI.OpenAI;
using Azure.Communication.CallAutomation;
using Azure.Core;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenAI;
using System.ClientModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using Showcase.Shared.AIExtensions.Realtime;
using Showcase.VoiceRagAgent;
using OpenAI.RealtimeConversation;

var builder = WebApplication.CreateBuilder(args);

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

//Call Automation Client
var client = new CallAutomationClient(connectionString: acsConnectionString);

var app = builder.Build();

var appBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');

if (string.IsNullOrEmpty(appBaseUrl))
{
    var websiteHostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
    Console.WriteLine($"websiteHostName :{websiteHostName}");
    appBaseUrl = $"https://{websiteHostName}";
    Console.WriteLine($"appBaseUrl :{appBaseUrl}");
}

app.MapGet("/", () => "Hello ACS CallAutomation!");

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        Console.WriteLine($"Incoming Call event received.");

        // Handle system events
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the subscription validation event.
            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
        }

        var jsonObject = Helper.GetJsonObject(eventGridEvent.Data);
        var callerId = Helper.GetCallerId(jsonObject);
        var incomingCallContext = Helper.GetIncomingCallContext(jsonObject);
        var callbackUri = new Uri(new Uri(appBaseUrl), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
        logger.LogInformation($"Callback Url: {callbackUri}");
        var websocketUri = appBaseUrl.Replace("https", "wss") + "/ws";
        logger.LogInformation($"WebSocket Url: {callbackUri}");

        var mediaStreamingOptions = new MediaStreamingOptions(
                new Uri(websocketUri),
                MediaStreamingContent.Audio,
                MediaStreamingAudioChannel.Mixed,
                startMediaStreaming: true
                )
        {
            EnableBidirectional = true,
            AudioFormat = AudioFormat.Pcm24KMono
        };

        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            MediaStreamingOptions = mediaStreamingOptions,
        };

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
        logger.LogInformation($"Answered call for connection id: {answerCallResult.CallConnection.CallConnectionId}");
    }
    return Results.Ok();
});

// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger) =>
{

    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event, Formatting.Indented)}");
    }

    return Results.Ok();
});

app.UseWebSockets();
#pragma warning disable OPENAI002

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            try
            {
                const string answerPromptSystemTemplate = """ 
                    You're an AI assistant for an elevator company called Contoso Elevators. Customers will contact you as the first point of contact when having issues with their elevators. 
                    Your priority is to ensure the person contacting you or anyone else in or around the elevator is safe, if not then they should contact their local authorities.
                    If everyone is safe then ask the user for information about the elevators location, such as city, building and elevator number.
                    Also get the users name and number so that a technician who goes onsite can contact this person. Confirm with the user all the information 
                    they've shared that it's all correct and then let them know that you've created a ticket and that a technician should be onsite within the next 24 to 48 hours.
                    """;
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var configuration = builder.Configuration;
                var openAiKey = configuration.GetValue<string>("AzureOpenAIServiceKey");
                ArgumentNullException.ThrowIfNullOrEmpty(openAiKey);

                var openAiUri = configuration.GetValue<string>("AzureOpenAIServiceEndpoint");
                ArgumentNullException.ThrowIfNullOrEmpty(openAiUri);

                var openAiModelName = configuration.GetValue<string>("AzureOpenAIDeploymentModelName");
                ArgumentNullException.ThrowIfNullOrEmpty(openAiModelName);
                var systemPrompt = configuration.GetValue<string>("SystemPrompt") ?? answerPromptSystemTemplate;
                ArgumentNullException.ThrowIfNullOrEmpty(openAiUri);
                var completionsClient = new AzureOpenAIClient(new Uri(openAiUri), new ApiKeyCredential(openAiKey));
                var voiceClient = completionsClient.AsVoiceClient(openAiModelName, context.RequestServices.GetRequiredService<ILogger<VoiceClient>>());
                ConversationSessionOptions options = new()
                {
                    Instructions = systemPrompt,
                    Voice = ConversationVoice.Shimmer,
                    InputAudioFormat = ConversationAudioFormat.Pcm16,
                    OutputAudioFormat = ConversationAudioFormat.Pcm16,

                    //InputTranscriptionOptions = new()
                    //{
                    //// OpenAI realtime excepts raw audio in/out and uses another model for transcriptions in parallel
                    //// Currently, it only supports whisper v2 (named whisper-1) for transcription 
                    //// Note, this means that the transcription will be done by a different model than the one generating the response, which may lead to differences between the audio and transcription
                    //    Model = "whisper-1", 
                    //},
                    TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(0.5f, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500)),
                };
                await voiceClient.StartConversationAsync(new AcsAIOutboundHandler(webSocket, logger: context.RequestServices.GetRequiredService<ILogger<AcsAIOutboundHandler>>()), options, cancellationToken: context.RequestAborted);

                //var mediaService = new AcsMediaStreamingHandler(webSocket, builder.Configuration);

                // Set the single WebSocket connection
                //await mediaService.ProcessWebSocketAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received {ex}");
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.Run();