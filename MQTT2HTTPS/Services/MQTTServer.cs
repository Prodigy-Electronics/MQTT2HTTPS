using MQTTnet.Server;

namespace MQTT2HTTPS.Services;

public sealed class MQTTServer
{
    public static Task OnClientConnectedAsync(ClientConnectedEventArgs args)
    {
        Log.Information($"Client '{args.ClientId}' connected");
        return Task.CompletedTask;
    }

    public static Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
    {
        Log.Information($"Accepting Client '{args.ClientId}'");
        return Task.CompletedTask;
    }
}
