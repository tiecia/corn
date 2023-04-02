using Discord.Interactions;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CornBot.Utilities;
using CornBot.Models;

namespace CornBot.Modules
{

    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {

        public InteractionService? Commands { get; set; }
        private readonly IServiceProvider _services;

        public AdminModule(IServiceProvider services)
        {
            _services = services;
            _services.GetRequiredService<CornClient>().Log(
                LogSeverity.Debug, "Modules", "Creating GeneralModule...");
        }

        [SlashCommand("set-announcement-channel", "Sets the guild's announcement channel (no channel specified removes the current channel).")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [EnabledInDm(false)]
        public async Task SetAnnouncementChannel(ITextChannel? channel = null)
        {
            var guildTracker = _services.GetRequiredService<GuildTracker>();
            var guild = guildTracker.LookupGuild(Context.Guild.Id);
            if (channel == null)
                guild.AnnouncementChannel = 0;
            else
                guild.AnnouncementChannel = channel.Id;
            await guildTracker.SaveGuildInfo(guild);
            await RespondAsync("The corn announcements channel has been successfully set to " +
                (channel == null ? "none" : channel!.Mention) + "!");
        }

    }
}
