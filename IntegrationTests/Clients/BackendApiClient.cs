using System.Net;
using System.Text.Json;
using IntegrationTests.Configuration;
using IntegrationTests.Contracts;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace IntegrationTests.Clients;

public sealed class BackendApiClient
{
    private readonly ApiSettings _settings;
    private readonly ILogger<BackendApiClient> _logger;

    public BackendApiClient(ApiSettings settings, ILogger<BackendApiClient> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<OrderResponse> GetOrderAsync(IAPIRequestContext api, string orderId)
    {
        var endpoint = $"/api/orders/{orderId}";
        _logger.LogInformation("GET {Endpoint}", endpoint);

        var response = await api.GetAsync(endpoint);
        if (response.Status != (int)HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Order fetch failed. Status={response.Status}");
        }

        var content = await response.TextAsync();
        return JsonSerializer.Deserialize<OrderResponse>(content, JsonOptions.Default)
            ?? throw new InvalidOperationException("Order response body was empty or invalid.");
    }

    public async Task<IAPIResponse> PostWebhookAsync(IAPIRequestContext api, WebhookEnvelope webhook)
    {
        _logger.LogInformation("POST /webhooks/salesforce correlationId={CorrelationId}", webhook.CorrelationId);

        return await api.PostAsync(
            "/webhooks/salesforce",
            new APIRequestContextOptions
            {
                DataObject = webhook,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                }
            });
    }

    public APIRequestNewContextOptions BuildRequestOptions()
    {
        var headers = new Dictionary<string, string>(_settings.DefaultHeaders)
        {
            ["X-Correlation-Id"] = $"day1-{Guid.NewGuid():N}"
        };

        return new APIRequestNewContextOptions
        {
            BaseURL = _settings.BaseUrl,
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true,
            Timeout = _settings.TimeoutSeconds * 1000
        };
    }
}