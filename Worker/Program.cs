using System;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddRedis(RedisConfig.FromEnvironment())
    .AddHangfireWithRedis()
    .AddHangfireServer();

var host = builder.Build();

host.Services
    .GetRequiredService<IRecurringJobManager>()
    .AddOrUpdate("recurring-job", () => Console.WriteLine("Recurring job executed!"), Cron.Minutely);

host.Run();
