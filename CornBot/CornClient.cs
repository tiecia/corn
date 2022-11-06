using CornBot.Handlers;
using CornBot.Models;
using CornBot.Utilities;
using CornBot.Serialization;
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
                .AddSingleton<GuildTrackerSerializer>()
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

            _services.GetRequiredService<GuildTrackerSerializer>().Initialize("userdata.db");
            await _services.GetRequiredService<GuildTracker>().LoadFromSerializer();

            var imageManipulator = _services.GetRequiredService<ImageManipulator>();
            imageManipulator.LoadFont("Assets/Consolas.ttf", 72, FontStyle.Regular);
            imageManipulator.AddFallbackFontFamily("Assets/NotoEmoji-Bold.ttf");
            string[] notoSansFiles = Directory.GetFiles("Assets/notosans", "*.ttf", SearchOption.TopDirectoryOnly);
            await Log(new LogMessage(LogSeverity.Info, "MainAsync", $"Loading {notoSansFiles.Length} Noto Sans files..."));
            foreach (var file in notoSansFiles)
                imageManipulator.AddFallbackFontFamily(file);
            imageManipulator.TestAllFallback();

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

        public Task Log(LogSeverity severity, string source, string message, Exception? exception = null)
        {
            return Log(new LogMessage(severity, source, message, exception));
        }

        private async Task AsyncOnReady()
        {
            await Log(new LogMessage(LogSeverity.Info, "OnReady", "corn has been created"));
            // TODO: verify that this definitely works and properly broadcasts exceptions
            _ = _services.GetRequiredService<GuildTracker>().StartDailyResetLoop()
                .ContinueWith(t => Log(new LogMessage(LogSeverity.Critical, "ResetLoop", "Daily reset loop failed, aborting.", t.Exception)),
                              TaskContinuationOptions.OnlyOnFaulted);
        }

    }
}
