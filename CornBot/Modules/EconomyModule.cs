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
            _services.GetRequiredService<CornClient>().Log(
                LogSeverity.Debug, "Modules", "Creating EconomyModule...");
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
                var amount = await user.PerformDaily();
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
                var userData = economy.GetUserInfo(user);
                var cornAmount = userData.CornCount;
                var stringId = user is not SocketGuildUser guildUser ?
                    user.ToString() :
                    $"{guildUser.DisplayName} ({guildUser})";
                var suffix = userData.HasClaimedDaily ? "" : $" {Constants.CALENDAR_EMOJI}";
                response.AppendLine($"{i + 1} : {stringId} - {cornAmount} corn{suffix}");
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

        [EnabledInDm(true)]
        [SlashCommand("total", "Gets the total corn count across all servers")]
        public async Task Total()
        {
            long total = _services.GetRequiredService<GuildTracker>().GetTotalCorn();
            await RespondAsync($"{Constants.CORN_EMOJI} a total of {total:n0} corn has been shucked across all servers {Constants.CORN_EMOJI}");
        }

        [EnabledInDm(false)]
        [SlashCommand("stats", "Gets an overview of your recent corn shucking")]
        public async Task Stats([Summary(description: "user to lookup")] IUser? user = null)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            user ??= Context.User;
            var userInfo = economy.LookupGuild(Context.Guild).GetUserInfo(user);

            var history = await economy.GetHistory(userInfo);

            EmbedFieldBuilder[] fields = new EmbedFieldBuilder[]
            {
                new EmbedFieldBuilder()
                    .WithName("Daily Count")
                    .WithValue(history.GetDailyCount().ToString("n0"))
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Daily Average")
                    .WithValue(history.GetDailyAverage().ToString("n2"))
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Daily Total")
                    .WithValue(history.GetDailyTotal().ToString("n0"))
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Message Total")
                    .WithValue(history.GetMessageTotal().ToString("n0"))
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Global Total")
                    .WithValue(economy.GetTotalCorn(user).ToString("n0"))
                    .WithIsInline(true),
            };

            var displayName = user is SocketGuildUser guildUser ? guildUser.DisplayName : user.Username;

            var author = new EmbedAuthorBuilder()
                .WithIconUrl(user.GetAvatarUrl())
                .WithName(user.ToString());

            var embed = new EmbedBuilder()
                .WithTitle($"{displayName}'s corn stats")
                .WithAuthor(author)
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithCurrentTimestamp()
                .WithColor(Color.Gold)
                .WithFields(fields)
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

    }
}
