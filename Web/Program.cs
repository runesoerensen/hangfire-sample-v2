using System;
using System.ComponentModel;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRedis(RedisConfig.FromEnvironment())
    .AddHangfireWithRedis();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<WorkerMessageTool>();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AllowAllAuthorizationFilter()]
});

app.MapGet("/", () => "Hello from .NET + Hangfire!");
app.MapGet("/enqueue", () =>
{
    BackgroundJob.Enqueue(() => Console.WriteLine("Hello from Hangfire!"));
    return "Job enqueued!";
});
app.MapMcp("/mcp");

app.Run();

[McpServerToolType]
public sealed class WorkerMessageTool
{
    [McpServerTool, Description("Send a message to a worker.")]
    public static string SendMessage(
        IBackgroundJobClient backgroundJobClient,
        [Description("The message for the worker")] string message)
    {
        backgroundJobClient.Enqueue(() => Console.WriteLine($"Message from MCP client: {message}"));

        return $"Worker message queued: {message}";
    }
}

public class AllowAllAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
