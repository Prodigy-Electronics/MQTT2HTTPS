using Microsoft.AspNetCore.Mvc;
using MQTTnet;

namespace MQTT2HTTPS.Controllers;

[ApiController]
[Route("/api/mqtt")]
public class MQTTController(MqttClientFactory clientFactory) : ControllerBase, IDisposable
{
    private readonly IMqttClient _client = clientFactory.CreateMqttClient();
    private readonly MqttClientOptions _clientOptions = new MqttClientOptionsBuilder()
        .WithTcpServer("localhost", 1883)
        .Build();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [HttpPost]
    [Route("firmware/update")]
    public async Task<IActionResult> FirmwareUpdate(byte[] firmwareDatas)
    {
        _ = await _client.ConnectAsync(_clientOptions);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("prodigy/device/firmware/update")
            .WithPayload(firmwareDatas)
            .Build();
        await _client.PublishAsync(message, CancellationToken.None);
        await _client.DisconnectAsync();
        return Ok();
    }
}
