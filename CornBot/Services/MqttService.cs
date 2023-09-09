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
            .WithTopic("corn/changed/corncount")
            .WithPayload(username)
            .Build();

        var res = await MqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        Log(res.IsSuccess
            ? "Sent MQTT message on corn/changed/corncount"
            : $"Failed to send MQTT message to corn/changed/corncount. Reason: {res.ReasonCode}");
    }

    public async void SendShuckStatusChangedNotificationAsync(string username)
    {
        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic("corn/changed/shuckstatus")
            .WithPayload(username)
            .Build();

        var res = await MqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        Log(res.IsSuccess
            ? "Sent MQTT message on corn/changed/shuckstatus"
            : $"Failed to send MQTT message to corn/changed/shuckstatus. Reason: {res.ReasonCode}");
    }

    private void Log(string msg)
    {
        _services.GetRequiredService<CornClient>().Log(LogSeverity.Debug, "Services",  msg);
    }
}