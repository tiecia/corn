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
            var isAdmin = Context.User is IGuildUser gu &&
                gu.GuildPermissions.Has(GuildPermission.Administrator);

            var helpString = BuildHelp(isAdmin);

            var embed = new EmbedBuilder()
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithTitle("corn's commands")
                .WithColor(Color.Gold)
                .WithDescription(helpString)
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

        [SlashCommand("link", "Gets the link to add corn to your server")]
        public async Task Link()
        {
            await RespondAsync($"you can add corn here: {Constants.CORN_LINK}");
        }

        private string BuildHelp(bool includeAdmin)
        {
            if (Commands == null)
                return "No loaded commands found.";

            var help = new StringBuilder();
            foreach (var command in Commands.SlashCommands)
            {
                // skip admin only commands
                if (includeAdmin || !command.Preconditions.Any(precon =>
                    precon is RequireUserPermissionAttribute rupa &&
                    rupa.GuildPermission == GuildPermission.Administrator
                ))
                    help.AppendLine($"`/{command.Name}` - {command.Description}");
            }

            return help.ToString();
        }

    }
}
