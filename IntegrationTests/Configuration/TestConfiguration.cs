using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Configuration;

public static class TestConfiguration
{
    private static readonly Lazy<IConfigurationRoot> LazyConfiguration = new(BuildConfiguration);

    public static IConfigurationRoot Current => LazyConfiguration.Value;

    private static IConfigurationRoot BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("AUTOMATION_ENV") ?? "Local";

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "AUTOMATION_")
            .Build();
    }
}