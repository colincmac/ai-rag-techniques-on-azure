namespace Showcase.GitHubCopilot.Agent;

public class AgentHub //: Hub
{

    //public async IAsyncEnumerable<string> Chat(

    //    [EnumeratorCancellation] CancellationToken cancellationToken
    //    )
    //{
    //    var chatOptions = new ChatOptions()
    //    {
    //        Tools = [AIFunctionFactory.Create(GetWeather)]
    //    };
    //    var msg2 = """data: {"choices":[{"delta":{},"finish_reason":"stop","index":0,"logprobs":null}],"created":1730492083,"id":"chatcmpl-AOsKZuK8xxYScPVSO1oAVyOXw6aSa","model":"gpt-4o-2024-08-06","object":"chat.completion.chunk","system_fingerprint":"fp_d54531d9eb"}\n\n""";
    //    var msg1 = """data: {"choices":[{"delta":{"content":"Hello There"},"finish_reason":null,"index":0,"logprobs":null}],"created":1730492083,"id":"chatcmpl-AOsKZuK8xxYScPVSO1oAVyOXw6aSa","model":"gpt-4o-2024-08-06","object":"chat.completion.chunk","system_fingerprint":"fp_d54531d9eb"}\n\n""";
    //    yield return msg1;
    //    yield return msg2;
    //}
    //[Description("Gets the weather")]
    //static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
}
