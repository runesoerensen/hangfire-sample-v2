using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRedis(this IServiceCollection services, RedisConfig config)
    {
        services.AddSingleton(config.ToConfigurationOptions());
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<ConfigurationOptions>();
            return ConnectionMultiplexer.Connect(options);
        });

        return services;
    }

    public static IServiceCollection AddHangfireWithRedis(this IServiceCollection services)
    {
        return services.AddHangfire((serviceProvider, configuration) =>
        {
            var connectionMultiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            configuration.UseRedisStorage(connectionMultiplexer);
        });
    }

    private static ConfigurationOptions ToConfigurationOptions(this RedisConfig config)
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { { config.Host, config.Port } },
            Password = config.Password,
            Ssl = config.Ssl,
        };

        if (config.SkipCertificateValidation)
        {
            options.CertificateValidation += (sender, cert, chain, errors) => true;
        }

        return options;
    }
}
