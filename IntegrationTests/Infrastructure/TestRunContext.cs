using IntegrationTests.Configuration;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Infrastructure;

public sealed class TestRunContext
{
    private TestRunContext(IConfigurationRoot configuration, ApiSettings apiSettings, LoggingSettings loggingSettings)
    {
        Configuration = configuration;
        ApiSettings = apiSettings;
        LoggingSettings = loggingSettings;
    }

    public IConfigurationRoot Configuration { get; }
    public ApiSettings ApiSettings { get; }
    public LoggingSettings LoggingSettings { get; }

    public static TestRunContext Create()
    {
        var configuration = TestConfiguration.Current;
        var api = configuration.BindRequiredSection<ApiSettings>("Api");
        var logging = configuration.BindRequiredSection<LoggingSettings>("Logging");
        return new TestRunContext(configuration, api, logging);
    }
}