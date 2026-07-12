using System.Net;
using System.Text;
using System.Text.Json;
using IntegrationTests.Contracts;

namespace IntegrationTests.Infrastructure;

public sealed class LocalFakeBackendServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _listenerTask;

    public LocalFakeBackendServer(string baseUrl)
    {
        var prefix = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _listener.Start();
        _listenerTask = Task.Run(() => ProcessRequestsAsync(_cts.Token));
    }

    public List<WebhookEnvelope> ReceivedWebhooks { get; } = new();

    private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleContextAsync(context, cancellationToken), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                if (context is not null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Close();
                }
            }
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/api/orders/123")
            {
                var payload = new OrderResponse("123", "Processed", 149.95m, "USD", DateTimeOffset.UtcNow);
                await WriteJsonAsync(response, payload, HttpStatusCode.OK, cancellationToken);
                return;
            }

            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/webhooks/salesforce")
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = await reader.ReadToEndAsync(cancellationToken);
                var envelope = JsonSerializer.Deserialize<WebhookEnvelope>(body, JsonOptions.Default)
                    ?? throw new InvalidOperationException("Invalid webhook payload");

                if (string.IsNullOrWhiteSpace(envelope.CorrelationId))
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Missing correlationId"), cancellationToken);
                    response.Close();
                    return;
                }

                lock (ReceivedWebhooks)
                {
                    ReceivedWebhooks.Add(envelope);
                }

                await WriteJsonAsync(response, new { accepted = true, correlationId = envelope.CorrelationId }, HttpStatusCode.Accepted, cancellationToken);
                return;
            }

            response.StatusCode = (int)HttpStatusCode.NotFound;
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Not Found"), cancellationToken);
            response.Close();
        }
        catch (JsonException)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid JSON payload"), cancellationToken);
            response.Close();
        }
        catch (Exception)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Close();
        }
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, object payload, HttpStatusCode statusCode, CancellationToken cancellationToken)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json";
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions.Default);
        await response.OutputStream.WriteAsync(bytes, cancellationToken);
        response.Close();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();

        try
        {
            _listenerTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }

        _cts.Dispose();
    }
}