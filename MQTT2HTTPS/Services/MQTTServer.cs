using MQTTnet.Server;
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MQTT2HTTPS.Services;

public sealed class MQTTServer(IConfiguration config) : IDisposable
{
    private readonly string _endPointUrl = config[EnviornmentKeys.MQTT_POST_ENDPOINT]!;
    private readonly HttpClient _client = new();

    public Task OnClientConnectedAsync(ClientConnectedEventArgs args)
    {
        Log.Information($"Client '{args.ClientId}' connected");
        return Task.CompletedTask;
    }

    public Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
    {
        Log.Information($"Accepting Client '{args.ClientId}'");
        return Task.CompletedTask;
    }

    public async Task MessagePublishedAsync(InterceptingPublishEventArgs args)
    {
        ReadOnlySequence<byte> payload = args.ApplicationMessage.Payload;
        using var stream = PipeReader.Create(payload).AsStream();
        try
        {
            JsonNode? node = JsonSerializer.Deserialize<JsonNode>(stream);
            var content = JsonContent.Create(node);
            var response = await _client.PostAsync(_endPointUrl, content);
            response.EnsureSuccessStatusCode();
        }
        catch (JsonException ex)
        {
            Log.Error("Recieved Message is not a json", ex);
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"Error on requesting {_endPointUrl}", ex);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
