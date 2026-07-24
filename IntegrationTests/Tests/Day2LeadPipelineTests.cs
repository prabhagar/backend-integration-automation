using Allure.NUnit;
using FluentAssertions;
using IntegrationTests.Contracts;
using IntegrationTests.Infrastructure.LeadFlow;

namespace IntegrationTests.Tests;

[TestFixture]
[AllureNUnit]
public sealed class Day2LeadPipelineTests
{
    private LeadIntegrationHarness _harness = null!;

    [SetUp]
    public void SetUp()
    {
        _harness = new LeadIntegrationHarness();
    }

    [Test]
    public async Task SalesforceLead_WhenPublished_ShouldFlowThroughQueueLambdaSqsAndServices()
    {
        var lead = new SalesforceLeadEvent(
            LeadId: "SF-LD-1001",
            FirstName: "Ava",
            LastName: "Patel",
            Company: "Acme Corp",
            Email: "ava.patel@acme.example",
            CorrelationId: "salesforce-unique-id-123",
            CreatedAtUtc: DateTimeOffset.Parse("2026-07-24T10:00:00Z"));

        await _harness.Producer.PublishAsync(lead);

        _harness.ProducerQueue.Count.Should().Be(1, "the Salesforce producer should enqueue the lead first");

        await _harness.QueueLambda.ForwardAsync();

        _harness.ProducerQueue.Count.Should().Be(0, "the queue lambda should drain the producer queue");
        _harness.SqsQueue.Count.Should().Be(1, "the queue lambda should place the message onto SQS");

        var result = await _harness.LeadService.ProcessNextAsync();

        result.Should().BeEquivalentTo(new LeadProcessingResult(
            LeadId: "SF-LD-1001",
            ProfileId: "profile-SF-LD-1001",
            ClientId: "client-SF-LD-1001",
            TicketId: "ticket-SF-LD-1001"));

        _harness.ProfileService.Requests.Should().ContainSingle(request =>
            request.LeadId == "SF-LD-1001" &&
            request.FullName == "Ava Patel" &&
            request.Email == "ava.patel@acme.example" &&
            request.CorrelationId == "salesforce-unique-id-123");

        _harness.ClientService.Requests.Should().ContainSingle(request =>
            request.LeadId == "SF-LD-1001" &&
            request.AccountName == "Acme Corp" &&
            request.Email == "ava.patel@acme.example" &&
            request.CorrelationId == "salesforce-unique-id-123");

        _harness.TicketService.Requests.Should().ContainSingle(request =>
            request.LeadId == "SF-LD-1001" &&
            request.Subject.Contains("Salesforce lead onboarding") &&
            request.CorrelationId == "salesforce-unique-id-123");
    }

    [Test]
    public async Task SalesforceLead_WhenEmailMissing_ShouldFailFastAtProducer()
    {
        var lead = new SalesforceLeadEvent(
            LeadId: "SF-LD-1002",
            FirstName: "No",
            LastName: "Email",
            Company: "Bad Data Inc",
            Email: string.Empty,
            CorrelationId: "salesforce-unique-id-456",
            CreatedAtUtc: DateTimeOffset.UtcNow);

        var action = async () => await _harness.Producer.PublishAsync(lead);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Email is required.*");
    }
}
