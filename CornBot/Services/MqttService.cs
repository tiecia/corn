using System.Diagnostics;
using Discord;
using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using MQTTnet.Client;

namespace CornBot.Services;

public class MqttService
{
    private IMqttClient MqttClient;
    private readonly IServiceProvider _services;
    
    public MqttService(IServiceProvider services)
    {
        _services = services;
        Log("Generating MQTT service");
        MqttClient = new MqttFactory().CreateMqttClient();
    }

    public async Task RunAsync()
    {
        var mqttUri = "broker.hivemq.com";
        
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttUri)
            .Build();

        await MqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        Log($"MQTT service connected to {mqttUri}");
    }

    public async void SendCornChangedNotificationAsync(string username)
    {
        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic("corn/changed")
            .WithPayload(username)
            .Build();

        var res = await MqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        Console.WriteLine(res.IsSuccess
            ? "Sent MQTT message on corn/changed"
            : $"Failed to send MQTT message to corn/changed. Reason: {res.ReasonCode}");
    }

    private void Log(string msg)
    {
        _services.GetRequiredService<CornClient>().Log(LogSeverity.Debug, "Services",  msg);
    }
}