using Azure.AI.OpenAI;
using Azure.Communication.CallAutomation;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using System.Net.WebSockets;
using System.Threading.Channels;

#pragma warning disable OPENAI002

public class AzureOpenAIService
{
    private WebSocket m_webSocket;
    private Channel<Func<Task>> m_channel;
    private CancellationTokenSource m_cts;
    private RealtimeConversationSession m_aiSession;
    private AcsMediaStreamingHandler m_mediaStreaming;
    private MemoryStream m_memoryStream;


    private string m_answerPromptSystemTemplate = """ 
    You're an AI assistant for an elevator company called Contoso Elevators. Customers will contact you as the first point of contact when having issues with their elevators. 
    Your priority is to ensure the person contacting you or anyone else in or around the elevator is safe, if not then they should contact their local authorities.
    If everyone is safe then ask the user for information about the elevators location, such as city, building and elevator number.
    Also get the users name and number so that a technician who goes onsite can contact this person. Confirm with the user all the information 
    they've shared that it's all correct and then let them know that you've created a ticket and that a technician should be onsite within the next 24 to 48 hours.
    """;

    public AzureOpenAIService(AcsMediaStreamingHandler mediaStreaming, IConfiguration configuration)
    {
        m_mediaStreaming = mediaStreaming;
        m_channel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
        m_cts = new CancellationTokenSource();
        m_aiSession = CreateAISessionAsync(configuration).GetAwaiter().GetResult();
        m_memoryStream = new MemoryStream();
        // start dequeue task for new audio packets
        _ = Task.Run(async () => await StartForwardingAudioToMediaStreaming());
    }


    private async Task<RealtimeConversationSession> CreateAISessionAsync(IConfiguration configuration)
    {
        var openAiKey = configuration.GetValue<string>("AzureOpenAIServiceKey");
        ArgumentNullException.ThrowIfNullOrEmpty(openAiKey);

        var openAiUri = configuration.GetValue<string>("AzureOpenAIServiceEndpoint");
        ArgumentNullException.ThrowIfNullOrEmpty(openAiUri);

        var openAiModelName = configuration.GetValue<string>("AzureOpenAIDeploymentModelName");
        ArgumentNullException.ThrowIfNullOrEmpty(openAiModelName);
        var systemPrompt = configuration.GetValue<string>("SystemPrompt") ?? m_answerPromptSystemTemplate;
        ArgumentNullException.ThrowIfNullOrEmpty(openAiUri);

        var aiClient = new AzureOpenAIClient(new Uri(openAiUri), new ApiKeyCredential(openAiKey));
        var RealtimeCovnClient = aiClient.GetRealtimeConversationClient(openAiModelName);
        var session = await RealtimeCovnClient.StartConversationSessionAsync();

        // Session options control connection-wide behavior shared across all conversations,
        // including audio input format and voice activity detection settings.
        ConversationSessionOptions sessionOptions = new()
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
        //sessionOptions.Tools.Add()

        await session.ConfigureSessionAsync(sessionOptions);

        return session;
    }

    private async Task StartForwardingAudioToMediaStreaming()
    {
        try
        {
            // Consume messages from channel and forward buffers to player
            while (true)
            {
                var processBuffer = await m_channel.Reader.ReadAsync(m_cts.Token).ConfigureAwait(false);
                await processBuffer.Invoke();
            }
        }
        catch (OperationCanceledException opCanceledException)
        {
            Console.WriteLine($"OperationCanceledException received for StartForwardingAudioToPlayer : {opCanceledException}");
        }
        catch (ObjectDisposedException objDisposedException)
        {
            Console.WriteLine($"ObjectDisposedException received for StartForwardingAudioToPlayer :{objDisposedException}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception received for StartForwardingAudioToPlayer {ex}");
        }
    }

    public void ReceiveAudioForOutBound(string data)
    {
        try
        {
            m_channel.Writer.TryWrite(async () => await m_mediaStreaming.SendMessageAsync(data));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\"Exception received on ReceiveAudioForOutBound {ex}");
        }
    }


    // Loop and wait for the AI response
    private async Task GetOpenAiStreamResponseAsync()
    {
        try
        {
            await m_aiSession.StartResponseAsync();
            await foreach (ConversationUpdate update in m_aiSession.ReceiveUpdatesAsync(m_cts.Token))
            {
                if (update is ConversationSessionStartedUpdate sessionStartedUpdate)
                {
                    Console.WriteLine($"<<< Session started. ID: {sessionStartedUpdate.SessionId}");
                    Console.WriteLine();
                }

                if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
                {
                    Console.WriteLine(
                        $"  -- Voice activity detection started at {speechStartedUpdate.AudioStartTime} ms");
                    // Barge-in, received stop audio
                    StopAudio();
                }

                if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
                {
                    Console.WriteLine(
                        $"  -- Voice activity detection ended at {speechFinishedUpdate.AudioEndTime} ms");
                }

                if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
                {
                    // With audio output enabled, the audio transcript of the delta update contains an approximation of
                    // the words spoken by the model. Without audio output, the text of the delta update will contain
                    // the segments making up the text content of a message.
                    Console.Write($"Delta Audio Transcript: {deltaUpdate.AudioTranscript}");
                    Console.Write($"Delta TextOnly Update (audio disabled): {deltaUpdate.Text}");
                    Console.Write($"Delta Function Args: {deltaUpdate.FunctionArguments}");

                    // Audio delta updates contain the incremental binary audio data of the generated output
                    ConvertToAcsAudioPacketAndForward(deltaUpdate.AudioBytes.ToArray());

                }


                if (update is ConversationItemStreamingStartedUpdate itemStartedUpdate)
                {
                    Console.WriteLine($"  -- Begin streaming of new item");

                }
                if (update is ConversationItemStreamingFinishedUpdate itemFinishedUpdate)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  -- Item streaming finished, response_id={itemFinishedUpdate.ResponseId}");
                }

                if (update is ConversationInputTranscriptionFinishedUpdate transcriptionCompletedUpdate)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  -- User audio transcript: {transcriptionCompletedUpdate.Transcript}");
                    Console.WriteLine();
                }

                if (update is ConversationResponseFinishedUpdate turnFinishedUpdate)
                {
                    Console.WriteLine($"  -- Model turn generation finished. Status: {turnFinishedUpdate.Status}");
                }

                if (update is ConversationErrorUpdate errorUpdate)
                {
                    Console.WriteLine();
                    Console.WriteLine($"ERROR: {errorUpdate.ErrorCode} {errorUpdate.Message}");
                    break;
                }
            }
        }
        catch (OperationCanceledException e)
        {
            Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during ai streaming -> {ex}");
        }
    }

    private void ConvertToAcsAudioPacketAndForward(byte[] audioData)
    {
        var audio = OutStreamingData.GetAudioDataForOutbound(audioData);
        // queue it to the buffer
        ReceiveAudioForOutBound(audio);
    }

    private void StopAudio()
    {
        try
        {
            var stopRequest = OutStreamingData.GetStopAudioForOutbound();

            ReceiveAudioForOutBound(stopRequest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during streaming -> {ex}");
        }
    }

    public void StartConversation()
    {
        _ = Task.Run(async () => await GetOpenAiStreamResponseAsync());
    }

    public async Task SendAudioToExternalAI(MemoryStream memoryStream)
    {
        await m_aiSession.SendInputAudioAsync(memoryStream);
    }

    public void Close()
    {
        m_cts.Cancel();
        m_cts.Dispose();
        m_aiSession.Dispose();
    }
}
