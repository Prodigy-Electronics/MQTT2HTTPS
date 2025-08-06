using MQTTnet;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MQTT2HTTPS.Services;

public sealed class HTTPBridgeService(
    IHttpClientFactory httpClientFactory,
    MqttClientFactory mqttClientFactory,
    IConfiguration config
    )
    : BackgroundService
{
    private IMqttClient _mqttClient = default!;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("HTTPBridge");

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        MQTTServer.OnServerStarted += (args) => OnMQTTServerStarted(args, stoppingToken);
        return Task.CompletedTask;
    }

    private async Task OnMQTTServerStarted(EventArgs args, CancellationToken stoppingToken)
    {
        _mqttClient = mqttClientFactory.CreateMqttClient();
        MqttClientOptions clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .Build();
        _ = await _mqttClient.ConnectAsync(clientOptions, stoppingToken);
        _ = await _mqttClient.SubscribeAsync(new()
        {
            SubscriptionIdentifier = 0,
            TopicFilters = [new() {
                QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                Topic = "prodigy"
            }]
        }, stoppingToken);
        _mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
    }

    private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        ReadOnlySequence<byte> payload = args.ApplicationMessage.Payload;
        MemoryStream payloadStream = new(payload.ToArray());
        JsonNode? jsonNode = await JsonSerializer.DeserializeAsync<JsonNode>(payloadStream);
        Log.Information($"{args.ClientId} Recieved: {jsonNode}");
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(config[EnvironmentKeys.MQTT_POST_URL], jsonNode);
        _ = response.EnsureSuccessStatusCode();
    }
}
