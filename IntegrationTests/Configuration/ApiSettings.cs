namespace IntegrationTests.Configuration;

public sealed class ApiSettings
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:5088";
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}