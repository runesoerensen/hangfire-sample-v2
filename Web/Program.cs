using System;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRedis(RedisConfig.FromEnvironment())
    .AddHangfireWithRedis();

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

app.Run();

public class AllowAllAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
