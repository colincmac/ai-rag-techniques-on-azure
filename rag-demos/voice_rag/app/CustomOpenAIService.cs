using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace CallAutomation_AzOpenAI_Voice;

public class CustomOpenAIService
{
    private string m_prompt; // Prompt stored as a class variable
    private Channel<Func<Task>> m_channel;
    private CancellationTokenSource m_cts;
    private AcsMediaStreamingHandler m_mediaStreaming;
    private IConfiguration m_configuration;
    private MemoryStream m_memoryStream;

    public CustomOpenAIService(AcsMediaStreamingHandler mediaStreaming, IConfiguration configuration)
    {
        m_mediaStreaming = mediaStreaming;
        m_configuration = configuration;
        m_channel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions { SingleReader = true });
        m_cts = new CancellationTokenSource();
        m_memoryStream = new MemoryStream();

        // Initialize the default prompt
        m_prompt = """
            Scenario: A merchant has questions about their Clover Mini not turning on and a fee for $149.
            IMPORTANT: If you hear any background conversations or comments from the customer that are not relevant to the current inquiry, do not respond. Remain silent and only respond if the customer addresses you directly or the context is relevant to the conversation, as they may be having side conversations.
            
            """;

        // Start dequeuing tasks to forward audio packets
        _ = Task.Run(async () => await StartForwardingAudioToMediaStreaming());
    }

    private async Task ConnectToOpenAIWebSocketAsync()
    {
        string openAiEndpoint = m_configuration.GetValue<string>("AzureOpenAIServiceEndpoint");
        string apiKey = m_configuration.GetValue<string>("AzureOpenAIServiceKey");

        using var client = new ClientWebSocket();
        client.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        client.Options.SetRequestHeader("Content-Type", "application/json");

        try
        {
            await client.ConnectAsync(new Uri($"{openAiEndpoint}/realtime"), CancellationToken.None);
            Console.WriteLine("Connected to OpenAI WebSocket");

            // Start handling WebSocket communication
            await Task.WhenAll(ReceiveWebSocketMessagesAsync(client), SendWebSocketMessagesAsync(client));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to OpenAI WebSocket: {ex.Message}");
        }
    }

    private async Task ReceiveWebSocketMessagesAsync(ClientWebSocket client)
    {
        var buffer = new byte[2048];
        while (client.State == WebSocketState.Open)
        {
            try
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received from OpenAI: {message}");
                    // Forward OpenAI response to media streaming
                    await m_mediaStreaming.SendMessageAsync(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("WebSocket connection closed by OpenAI.");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving WebSocket message: {ex.Message}");
                break;
            }
        }
    }

    private async Task SendWebSocketMessagesAsync(ClientWebSocket client)
    {
        // Include the prompt in the payload
        var payload = new
        {
            type = "session.create",
            session = new
            {
                instructions = m_prompt,
                temperature = 0.7,
                max_tokens = 1000
            }
        };

        string message = JsonSerializer.Serialize(payload);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        try
        {
            await client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Sent to OpenAI: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending WebSocket message: {ex.Message}");
        }
    }

    private async Task StartForwardingAudioToMediaStreaming()
    {
        try
        {
            while (true)
            {
                var processBuffer = await m_channel.Reader.ReadAsync(m_cts.Token).ConfigureAwait(false);
                await processBuffer.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in StartForwardingAudioToMediaStreaming: {ex.Message}");
        }
    }

    public async Task SendAudioToExternalAI(MemoryStream memoryStream)
    {
        // Convert audio data to a JSON payload and send it
        string audioPayload = ConvertAudioToJson(memoryStream);
        await m_channel.Writer.WriteAsync(async () => await m_mediaStreaming.SendMessageAsync(audioPayload), m_cts.Token);
    }

    private string ConvertAudioToJson(MemoryStream memoryStream)
    {
        var audioPayload = new
        {
            type = "audio",
            data = Convert.ToBase64String(memoryStream.ToArray())
        };
        return JsonSerializer.Serialize(audioPayload);
    }

    public void StartConversation()
    {
        _ = Task.Run(async () => await ConnectToOpenAIWebSocketAsync());
    }

    public void UpdatePrompt(string newPrompt)
    {
        // Method to update the prompt dynamically
        m_prompt = newPrompt;
    }

    public string GetPrompt()
    {
        // Method to retrieve the current prompt
        return m_prompt;
    }

    public void Close()
    {
        m_cts.Cancel();
        m_cts.Dispose();
        m_memoryStream.Dispose();
    }
}
