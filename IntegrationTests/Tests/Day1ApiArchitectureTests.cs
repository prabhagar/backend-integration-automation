using FluentAssertions;
using Allure.NUnit;
using IntegrationTests.Assertions;
using IntegrationTests.Contracts;
using System.Net;
using System.Net.Http;
using System.Text;

namespace IntegrationTests.Tests;

[TestFixture]
[AllureNUnit]
public sealed class Day1ApiArchitectureTests
{
    [Test]
    public async Task ApiOrderContract_WhenOrderExists_ShouldReturnValidContract()
    {
        await using var api = await Day1Fixture.Playwright.APIRequest.NewContextAsync(Day1Fixture.Client.BuildRequestOptions());

        var order = await Day1Fixture.Client.GetOrderAsync(api, "123");

        ApiAssertions.AssertOrderContract(order);
    }

    [Test]
    public async Task WebhookIngestion_WhenPayloadSubmitted_ShouldBeAcceptedAndStored()
    {
        await using var api = await Day1Fixture.Playwright.APIRequest.NewContextAsync(Day1Fixture.Client.BuildRequestOptions());

        var correlationId = $"crm-sync-{Guid.NewGuid():N}";
        var webhook = new WebhookEnvelope(
            EventType: "crm.customer.updated",
            CorrelationId: correlationId,
            SourceSystem: "Salesforce",
            Payload: new Dictionary<string, object?>
            {
                ["customerId"] = "SF-1001",
                ["externalRef"] = "D365-9981",
                ["operation"] = "upsert"
            });

        var response = await Day1Fixture.Client.PostWebhookAsync(api, webhook);
        await ApiAssertions.AssertAcceptedAsync(response);

        Day1Fixture.FakeServer.ReceivedWebhooks.Should().ContainSingle(w => w.CorrelationId == correlationId);
    }

    [Test]
    public async Task ApiOrderContract_WhenOrderMissing_ShouldThrowWithStatusCode()
    {
        await using var api = await Day1Fixture.Playwright.APIRequest.NewContextAsync(Day1Fixture.Client.BuildRequestOptions());

        var action = async () => await Day1Fixture.Client.GetOrderAsync(api, "999");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Status=404*");
    }

    [Test]
    public async Task WebhookIngestion_WhenPayloadIsMalformed_ShouldReturnBadRequest()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(Day1Fixture.Context.ApiSettings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(Day1Fixture.Context.ApiSettings.TimeoutSeconds)
        };

        using var content = new StringContent("{\"eventType\":\"crm.customer.updated\",\"payload\":\"oops\"}", Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/webhooks/salesforce", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}