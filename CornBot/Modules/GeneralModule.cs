using CornBot.Handlers;
using CornBot.Utilities;
using CornBot.Models;
using CornBot.Serialization;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;

namespace CornBot.Modules
{

    public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
    {
        
        public InteractionService? Commands { get; set; }
        private string? _helpString;
        private readonly IServiceProvider _services;

        public GeneralModule(IServiceProvider services)
        {
            _services = services;
            _services.GetRequiredService<CornClient>().Log(
                LogSeverity.Debug, "Modules", "Creating GeneralModule...");
        }

        [SlashCommand("help", "Gets information on commands")]
        public async Task Help()
        {
            if (_helpString == null)
                BuildHelp();

            var embed = new EmbedBuilder()
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithTitle("Corn's commands")
                .WithColor(Color.Gold)
                .WithDescription(_helpString)
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

        [SlashCommand("link", "Gets the link to add corn to your server")]
        public async Task Link()
        {
            await RespondAsync($"you can add corn here: {Constants.CORN_LINK}");
        }

        private void BuildHelp()
        {
            if (Commands == null) return;

            var help = new StringBuilder();
            foreach (var command in Commands.SlashCommands)
                help.AppendLine($"`/{command.Name}` - {command.Description}");

            _helpString = help.ToString();
        }

    }
}
