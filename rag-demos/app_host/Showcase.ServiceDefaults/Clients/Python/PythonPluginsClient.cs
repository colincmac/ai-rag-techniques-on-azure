using System.Net.Http.Json;

namespace Showcase.ServiceDefaults.Clients.Python;
public class PythonPluginsClient(HttpClient http)
{
    public async Task<string?> ClassifyTextAsync(string text, IEnumerable<string> candidateLabels)
    {
        var response = await http.PostAsJsonAsync("/classify",
            new { text, candidate_labels = candidateLabels });
        var label = await response.Content.ReadFromJsonAsync<string>();
        return label;
    }

    //private async Task<string> SendRequestAsync(string uri, HttpMethod method, HttpContent? requestContent, CancellationToken cancellationToken)
    //{
    //    using var request = new HttpRequestMessage(method, uri) { Content = requestContent };
    //    request.Headers.Add("User-Agent", HttpHeaderConstant.Values.UserAgent);
    //    request.Headers.Add(HttpHeaderConstant.Names.SemanticKernelVersion, HttpHeaderConstant.Values.GetAssemblyVersion(typeof(HttpPlugin)));
    //    using var response = await this._client.SendWithSuccessCheckAsync(request, cancellationToken).ConfigureAwait(false);
    //    return await response.Content.ReadAsStringWithExceptionMappingAsync().ConfigureAwait(false);
    //}
}
