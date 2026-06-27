using IntegrationTests.Clients;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace IntegrationTests;

[SetUpFixture]
public sealed class Day1Fixture
{
    public static IPlaywright Playwright { get; private set; } = null!;
    public static TestRunContext Context { get; private set; } = null!;
    public static ILoggerFactory LoggerFactory { get; private set; } = null!;
    public static BackendApiClient Client { get; private set; } = null!;
    public static LocalFakeBackendServer FakeServer { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        Context = TestRunContext.Create();
        LoggerFactory = TestLoggerFactory.Create(Context.LoggingSettings);
        var logger = LoggerFactory.CreateLogger<BackendApiClient>();

        FakeServer = new LocalFakeBackendServer(Context.ApiSettings.BaseUrl);
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Client = new BackendApiClient(Context.ApiSettings, logger);
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        FakeServer.Dispose();
        LoggerFactory.Dispose();
        Playwright.Dispose();
    }
}