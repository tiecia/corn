using CornBot.Handlers;
using CornBot.Models;
using CornBot.Utilities;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SixLabors.Fonts;

namespace CornBot
{
    public class CornClient
    {

        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMembers |
                             GatewayIntents.GuildEmojis |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.DirectMessages,
            AlwaysDownloadUsers = true,
        };

        public CornClient()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("cornfig.json", false, false)
                .Build();

            _services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(_configuration)
                .AddSingleton(_socketConfig)
                .AddSingleton(new Random((int)DateTime.UtcNow.Ticks))
                .AddSingleton<GuildTracker>()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<MessageHandler>()
                .AddSingleton<InteractionHandler>()
                .AddSingleton<ImageManipulator>()
                .AddSingleton<ImageStore>()
                .BuildServiceProvider();
        }

        public async Task MainAsync()
        {
            var client = _services.GetRequiredService<DiscordSocketClient>();

            client.Log += Log;
            client.Ready += AsyncOnReady;

            await _services.GetRequiredService<MessageHandler>().Initialize();
            await _services.GetRequiredService<InteractionHandler>().InitializeAsync();
            _services.GetRequiredService<ImageManipulator>().LoadFont("Assets/consolas.ttf", 72, FontStyle.Regular);
            await _services.GetRequiredService<ImageStore>().LoadImages();

            await client.LoginAsync(TokenType.Bot, _configuration["discord_token"]);
            await client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        public Task Log(LogMessage msg)
        {
            if (msg.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{msg.Severity}] {cmdException.Command.Aliases.First()}"
                                 + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{msg.Severity}] {msg}");
            return Task.CompletedTask;
        }

        private async Task AsyncOnReady()
        {
            var guildTracker = _services.GetRequiredService<GuildTracker>();
            guildTracker.LoadFromFile("data.json");
            await Log(new LogMessage(LogSeverity.Info, "OnReady", "corn has been created"));
            // TODO: verify that this definitely works and properly broadcasts exceptions
            _ = guildTracker.StartSaveLoop()
                .ContinueWith(t => Log(new LogMessage(LogSeverity.Critical, "SaveLoop", "Save loop failed, aborting.", t.Exception)),
                              TaskContinuationOptions.OnlyOnFaulted);
            _ = guildTracker.StartDailyResetLoop()
                .ContinueWith(t => Log(new LogMessage(LogSeverity.Critical, "ResetLoop", "Daily reset loop failed, aborting.", t.Exception)),
                              TaskContinuationOptions.OnlyOnFaulted);
        }

    }
}
