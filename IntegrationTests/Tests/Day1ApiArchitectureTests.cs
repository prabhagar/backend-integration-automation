using FluentAssertions;
using IntegrationTests.Assertions;
using IntegrationTests.Contracts;

namespace IntegrationTests.Tests;

[TestFixture]
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
}