using Microsoft.SemanticKernel;
using Showcase.ServiceDefaults.Clients.Python;
using System.ComponentModel;

namespace Showcase.ApiService.Plugins;

public class Forecasting(PythonPluginsClient pythonPlugins)
{

    [KernelFunction]
    [Description("Classify text using a Python plugin.")]
    public async Task<string> ClassifyTextAsync(string text, IEnumerable<string> candidateLabels)
    {
        return await pythonPlugins.ClassifyTextAsync(text, candidateLabels);
    }
}
