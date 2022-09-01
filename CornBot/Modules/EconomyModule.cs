using CornBot.Models;
using CornBot.Utilities;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Modules
{

    public class EconomyModule : InteractionModuleBase<SocketInteractionContext>
    {
        
        public InteractionService? Commands { get; set; }
        private readonly IServiceProvider _services;

        public EconomyModule(IServiceProvider services)
        {
            _services = services;
        }

        [EnabledInDm(false)]
        [SlashCommand("corn", "Gets your total corn count")]
        public async Task Corn([Summary(description: "user to lookup")] IUser? user = null)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            var userInfo = economy.LookupGuild(Context.Guild).GetUserInfo(user ?? Context.User);
            var stringId = user is null ? "you have" :
                    user is not SocketGuildUser guildUser ? $"{user} has" :
                    $"{guildUser.DisplayName} ({guildUser}) has";
            await RespondAsync($"{Constants.CORN_EMOJI} {stringId} {userInfo.CornCount} corn {Constants.CORN_EMOJI}");
        }

        [EnabledInDm(false)]
        [SlashCommand("daily", "Performs your daily shucking of corn")]
        public async Task Daily()
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            var user = economy.LookupGuild(Context.Guild).GetUserInfo(Context.User);

            if (user.HasClaimedDaily)
                await RespondAsync("what are you trying to do, spam the daily command?");
            else
            {
                var amount = user.PerformDaily();
                await RespondAsync($"{Constants.CORN_EMOJI} you have shucked {amount} corn today. you now have {user.CornCount} corn {Constants.CORN_EMOJI}");
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("lb", "Alias for /leaderboards")]
        public async Task LeaderboardsAlias() => await Leaderboards();

        [EnabledInDm(false)]
        [SlashCommand("leaderboards", "Displays the top corn havers in the guild")]
        public async Task Leaderboards()
        {
            var economy = _services.GetRequiredService<GuildTracker>().LookupGuild(Context.Guild);
            var topUsers = await economy.GetLeaderboards();
            var response = new StringBuilder();

            for (int i = 0; i < topUsers.Count; i++)
            {
                var user = topUsers[i];
                var cornAmount = economy.GetUserInfo(user).CornCount;
                var stringId = user is not SocketGuildUser guildUser ?
                    user.ToString() :
                    $"{guildUser.DisplayName} ({guildUser})";
                response.AppendLine($"{i + 1} : {stringId} - {cornAmount} corn");
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithTitle("Top corn havers:")
                .WithDescription(response.ToString())
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

    }
}
