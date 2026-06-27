namespace IntegrationTests.Configuration;

public sealed class LoggingSettings
{
    public string LogsDirectory { get; set; } = "TestArtifacts/logs";
    public string MinimumLevel { get; set; } = "Information";
}