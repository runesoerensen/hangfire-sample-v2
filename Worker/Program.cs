using Shared.Infrastructure;
using Hangfire;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddRedis(RedisConfig.FromEnvironment())
    .AddHangfireWithRedis()
    .AddHangfireServer();

var host = builder.Build();

var recurringJobs = host.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate("recurring-job", () => Console.WriteLine("Recurring job executed!"), Cron.Minutely);

host.Run();