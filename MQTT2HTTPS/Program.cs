using MQTT2HTTPS.Services;
using MQTTnet.AspNetCore;

namespace MQTT2HTTPS;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;

        builder.WebHost.UseKestrel(options =>
        {
            options.ListenAnyIP(1883, o => o.UseMqtt());
            options.ListenAnyIP(8080);
            options.ListenAnyIP(8081, o => o.UseHttps());
        });
        builder.Services.AddControllers();
        builder.Services.AddOpenApiDocument();

        builder.Services.AddHostedMqttServer(options =>
        {
            options.WithoutDefaultEndpoint();
        });
        builder.Services.AddMqttConnectionHandler();
        builder.Services.AddConnections();
        var mqttServer = new MQTTServer(config);
        builder.Services.AddSingleton(mqttServer);

        builder.Services.AddSerilog((services, lc) =>
        {
            lc.ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseOpenApi();
            app.UseSwaggerUi();
        }

        app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();
        app.UseAuthorization();

        app.UseMqttServer(server =>
        {
            server.ValidatingConnectionAsync += mqttServer.ValidateConnectionAsync;
            server.ClientConnectedAsync += mqttServer.OnClientConnectedAsync;
            server.InterceptingPublishAsync += mqttServer.MessagePublishedAsync;
        });

        app.MapControllers();

        app.Run();
    }
}
