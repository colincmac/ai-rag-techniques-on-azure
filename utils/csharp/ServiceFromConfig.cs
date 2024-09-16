using System.Text.Json;
using System.IO;
public class ServiceFromConfig<TConfig>
{
    public TConfig Configuration;
    public const string DEFAULT_CONFIG_FILE = "env.json";

    public ServiceFromConfig(string configFile = DEFAULT_CONFIG_FILE)
    {
        Configuration = LoadConfigFromFile(configFile);
    }

    private TConfig LoadConfigFromFile(string configFile)
    {
        var fileContent = File.ReadAllText(configFile);
        return JsonSerializer.Deserialize<TConfig>(fileContent);
    }
}