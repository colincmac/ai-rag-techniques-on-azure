using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Net.Http.Headers;
using Showcase.GitHubCopilot.Agent;
using Showcase.GitHubCopilot.Extensions;
using Showcase.Shared.AIExtensions;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddOptions<GitHubCopilotAgentOptions>(GitHubCopilotAgentOptions.ConfigurationSection)
    .Bind(builder.Configuration.GetSection(GitHubCopilotAgentOptions.ConfigurationSection))
    .ValidateDataAnnotations();

builder.Services.AddSingleton<IGitHubAgentFactory, GitHubAgentFactory>();
builder.Services.AddSingleton<IAIToolRegistry, AIToolRegistry>();
builder.Services.AddSingleton<IGitHubAgentHandler, Plugins>();

builder.Services.AddSingleton<IAuthorizationHandler, VerifyCopilotSignatureHandler>();
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy(VerifyCopilotSignatureRequirement.RequirementName, policy =>
    {
        policy.Requirements.Add(new VerifyCopilotSignatureRequirement());
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.Use();
    //app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();

}

app.UseAuthorization();

app.MapGet("/", () => Results.Ok("Ok"));

app.MapPost("/", async ([FromHeader(Name = "x-github-token")] string gitHubToken, IGitHubAgentFactory gitHubAgentFactory, HttpContext ctx, CancellationToken cancellationToken) =>
{
    ctx.Response.Headers.Append(HeaderNames.ContentType, "text/event-stream");
    ctx.Response.Headers.Append(HeaderNames.Connection, "keep-alive");
    ctx.Response.Headers.Append(HeaderNames.KeepAlive, "timeout=10");
    //var chatOptions = new ChatOptions()
    //{

    //    Tools = [AIFunctionFactory.Create(Plugins.GetWeather)]
    //};
    var chatRequest = await JsonSerializer.DeserializeAsync<CopilotRequest>(ctx.Request.Body, AIJsonUtilities.DefaultOptions, cancellationToken);

    if (chatRequest is null)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.CompleteAsync();
        return;
    }

    var gitHubAgent = gitHubAgentFactory.CreateAgent(gitHubToken);
    var response = gitHubAgent.CompleteStreamingAsync(chatRequest.Messages.Select(x => (ChatMessage)x).ToList(), cancellationToken: cancellationToken);
    var sseEvents = response
        .Where(x => x.RawRepresentation is OpenAI.Chat.StreamingChatCompletionUpdate && !string.IsNullOrEmpty(x.CompletionId))
        .Select(s => s.RawRepresentation as OpenAI.Chat.StreamingChatCompletionUpdate);

    await AIJsonUtilitiesExtensions.SerializeAndSendAsSseDataAsync(ctx.Response.Body, sseEvents, options: GitHubCopilotJsonContext.Default.StreamingChatCompletionUpdate.Options, cancellationToken: cancellationToken);


    await ctx.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("data: [DONE]\n\n"), cancellationToken);

    await ctx.Response.CompleteAsync();
})
.WithName("GitHubAgent")
.RequireAuthorization(VerifyCopilotSignatureRequirement.RequirementName)
.WithOpenApi();

app.Run();




