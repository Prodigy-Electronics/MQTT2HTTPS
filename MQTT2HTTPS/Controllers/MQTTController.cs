using Microsoft.AspNetCore.Mvc;
using MQTTnet;

namespace MQTT2HTTPS.Controllers;

[ApiController]
[Route("/api/mqtt")]
public class MQTTController(IMqttClient mqttClient) : ControllerBase, IDisposable
{
    private readonly IMqttClient _mqttClient = mqttClient;
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [HttpPost]
    [Route("firmware/update")]
    public async Task<IActionResult> FirmwareUpdate(byte[] firmwareDatas)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("prodigy/device/firmware/update")
            .WithPayload(firmwareDatas)
            .Build();
        await _mqttClient.PublishAsync(message, CancellationToken.None);
        return Ok();
    }
}
