namespace Shared.Infrastructure;

public class RedisConfig
{
    public required string Host { get; set; }
    public required string Password { get; set; }
    public int Port { get; set; }
    public bool Ssl { get; set; }
    public bool SkipCertificateValidation { get; set; }

    public static RedisConfig FromEnvironment()
    {
        var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");

        if (string.IsNullOrWhiteSpace(redisUrl))
            throw new InvalidOperationException("REDIS_URL environment variable must be set.");

        if (!Uri.TryCreate(redisUrl, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Invalid REDIS_URL format.");

        var userInfo = uri.UserInfo.Split(':');
        if (userInfo.Length != 2)
            throw new InvalidOperationException("REDIS_URL must be in the format redis://user:password@host:port");

        var isSsl = uri.Scheme == "rediss";
        return new RedisConfig
        {
            Host = uri.Host,
            Port = uri.Port,
            Password = userInfo[1],
            Ssl = isSsl,
            SkipCertificateValidation = isSsl
        };
    }
}
