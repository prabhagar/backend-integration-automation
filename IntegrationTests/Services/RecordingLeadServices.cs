using IntegrationTests.Contracts;

namespace IntegrationTests.Services;

public interface IProfileService
{
    Task<string> CreateProfileAsync(ProfileCreateRequest request, CancellationToken cancellationToken = default);
}

public interface IClientService
{
    Task<string> CreateClientAsync(ClientCreateRequest request, CancellationToken cancellationToken = default);
}

public interface ITicketService
{
    Task<string> CreateTicketAsync(TicketCreateRequest request, CancellationToken cancellationToken = default);
}

public sealed class RecordingProfileService : IProfileService
{
    private readonly List<ProfileCreateRequest> _requests = new();

    public IReadOnlyList<ProfileCreateRequest> Requests => _requests;

    public Task<string> CreateProfileAsync(ProfileCreateRequest request, CancellationToken cancellationToken = default)
    {
        _requests.Add(request);
        return Task.FromResult($"profile-{request.LeadId}");
    }
}

public sealed class RecordingClientService : IClientService
{
    private readonly List<ClientCreateRequest> _requests = new();

    public IReadOnlyList<ClientCreateRequest> Requests => _requests;

    public Task<string> CreateClientAsync(ClientCreateRequest request, CancellationToken cancellationToken = default)
    {
        _requests.Add(request);
        return Task.FromResult($"client-{request.LeadId}");
    }
}

public sealed class RecordingTicketService : ITicketService
{
    private readonly List<TicketCreateRequest> _requests = new();

    public IReadOnlyList<TicketCreateRequest> Requests => _requests;

    public Task<string> CreateTicketAsync(TicketCreateRequest request, CancellationToken cancellationToken = default)
    {
        _requests.Add(request);
        return Task.FromResult($"ticket-{request.LeadId}");
    }
}
