var builder = DistributedApplication.CreateBuilder(args);

//var cache = builder.AddRedis("cache");
var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
    : builder.AddConnectionString("openai");

var pythonPlugins = builder.AddPythonApp(
    name: "python-plugins",
    projectDirectory: Path.Combine("..", "Python.Plugins"),
    scriptPath: "-m",
    virtualEnvironmentPath: "env",
    scriptArgs: ["uvicorn", "main:app"])
       .WithEndpoint(targetPort: 62394, scheme: "http", env: "UVICORN_PORT");


var apiService = builder.AddProject<Projects.Showcase_ApiService>("apiservice")
    .WithReference(openai)
    .WithReference(pythonPlugins);

builder.AddProject<Projects.Showcase_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.AddProject<Projects.Showcase_GitHubCopilot_Agent>("showcase-githubcopilot-agent");

builder.Build().Run();
