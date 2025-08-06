using MQTT2HTTPS.Services;
using MQTTnet;
using MQTTnet.AspNetCore;

namespace MQTT2HTTPS;

public class Program
{
    public static async Task Main(string[] args)
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
            options.ListenAnyIP(int.Parse(config["ASPNETCORE_HTTP_PORTS"] ?? "8080"));
        });
        builder.Services.AddControllers();
        builder.Services.AddOpenApiDocument();

        builder.Services.AddHostedMqttServer(options =>
        {
            options.WithoutDefaultEndpoint();
        });
        builder.Services.AddMqttConnectionHandler();
        builder.Services.AddConnections();

        builder.Services.AddSingleton<MQTTServer>();
        builder.Services.AddSingleton<MqttClientFactory>();

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

        app.UseSerilogRequestLogging();
        app.UseAuthorization();

        app.UseMqttServer(server =>
        {
            server.ValidatingConnectionAsync += MQTTServer.ValidateConnectionAsync;
            server.ClientConnectedAsync += MQTTServer.OnClientConnectedAsync;
        });

        app.MapControllers();

        await app.RunAsync();
    }
}
