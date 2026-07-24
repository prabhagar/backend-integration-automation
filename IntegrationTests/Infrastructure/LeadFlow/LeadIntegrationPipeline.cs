using IntegrationTests.Contracts;
using IntegrationTests.Services;

namespace IntegrationTests.Infrastructure.LeadFlow;

public sealed class SalesforceLeadProducer
{
    private readonly InMemoryMessageQueue<SalesforceLeadEvent> _producerQueue;

    public SalesforceLeadProducer(InMemoryMessageQueue<SalesforceLeadEvent> producerQueue)
    {
        _producerQueue = producerQueue;
    }

    public Task PublishAsync(SalesforceLeadEvent leadEvent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(leadEvent.LeadId))
        {
            throw new ArgumentException("LeadId is required.", nameof(leadEvent));
        }

        if (string.IsNullOrWhiteSpace(leadEvent.Email))
        {
            throw new ArgumentException("Email is required.", nameof(leadEvent));
        }

        if (string.IsNullOrWhiteSpace(leadEvent.CorrelationId))
        {
            throw new ArgumentException("CorrelationId is required.", nameof(leadEvent));
        }

        _producerQueue.Enqueue(leadEvent);
        return Task.CompletedTask;
    }
}

public sealed class LeadQueueToSqsLambda
{
    private readonly InMemoryMessageQueue<SalesforceLeadEvent> _producerQueue;
    private readonly InMemoryMessageQueue<SqsLeadEnvelope> _sqsQueue;

    public LeadQueueToSqsLambda(
        InMemoryMessageQueue<SalesforceLeadEvent> producerQueue,
        InMemoryMessageQueue<SqsLeadEnvelope> sqsQueue)
    {
        _producerQueue = producerQueue;
        _sqsQueue = sqsQueue;
    }

    public Task ForwardAsync(CancellationToken cancellationToken = default)
    {
        while (_producerQueue.TryDequeue(out var leadEvent))
        {
            var envelope = new SqsLeadEnvelope(
                MessageId: Guid.NewGuid().ToString("N"),
                Lead: leadEvent,
                SourceQueue: "salesforce-leads-queue",
                EnqueuedAtUtc: DateTimeOffset.UtcNow);

            _sqsQueue.Enqueue(envelope);
        }

        return Task.CompletedTask;
    }
}

public sealed class LeadLambdaService
{
    private readonly InMemoryMessageQueue<SqsLeadEnvelope> _sqsQueue;
    private readonly IProfileService _profileService;
    private readonly IClientService _clientService;
    private readonly ITicketService _ticketService;

    public LeadLambdaService(
        InMemoryMessageQueue<SqsLeadEnvelope> sqsQueue,
        IProfileService profileService,
        IClientService clientService,
        ITicketService ticketService)
    {
        _sqsQueue = sqsQueue;
        _profileService = profileService;
        _clientService = clientService;
        _ticketService = ticketService;
    }

    public async Task<LeadProcessingResult> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_sqsQueue.TryDequeue(out var envelope))
        {
            throw new InvalidOperationException("No message found in SQS queue.");
        }

        var lead = envelope.Lead;
        var fullName = $"{lead.FirstName} {lead.LastName}".Trim();

        var profileRequest = new ProfileCreateRequest(
            LeadId: lead.LeadId,
            FullName: fullName,
            Email: lead.Email,
            CorrelationId: lead.CorrelationId);

        var clientRequest = new ClientCreateRequest(
            LeadId: lead.LeadId,
            AccountName: lead.Company,
            Email: lead.Email,
            CorrelationId: lead.CorrelationId);

        var ticketRequest = new TicketCreateRequest(
            LeadId: lead.LeadId,
            Subject: $"Salesforce lead onboarding: {lead.Company}",
            Description: $"Route Salesforce lead {lead.LeadId} into profile, client, and ticket services.",
            CorrelationId: lead.CorrelationId);

        var profileId = await _profileService.CreateProfileAsync(profileRequest, cancellationToken);
        var clientId = await _clientService.CreateClientAsync(clientRequest, cancellationToken);
        var ticketId = await _ticketService.CreateTicketAsync(ticketRequest, cancellationToken);

        return new LeadProcessingResult(
            LeadId: lead.LeadId,
            ProfileId: profileId,
            ClientId: clientId,
            TicketId: ticketId);
    }
}

public sealed class LeadIntegrationHarness
{
    public InMemoryMessageQueue<SalesforceLeadEvent> ProducerQueue { get; } = new();
    public InMemoryMessageQueue<SqsLeadEnvelope> SqsQueue { get; } = new();
    public RecordingProfileService ProfileService { get; } = new();
    public RecordingClientService ClientService { get; } = new();
    public RecordingTicketService TicketService { get; } = new();
    public SalesforceLeadProducer Producer { get; }
    public LeadQueueToSqsLambda QueueLambda { get; }
    public LeadLambdaService LeadService { get; }

    public LeadIntegrationHarness()
    {
        Producer = new SalesforceLeadProducer(ProducerQueue);
        QueueLambda = new LeadQueueToSqsLambda(ProducerQueue, SqsQueue);
        LeadService = new LeadLambdaService(SqsQueue, ProfileService, ClientService, TicketService);
    }
}
