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
            var cornEmoji = Utility.GetCurrentEvent() == Constants.CornEvent.PRIDE ?
                Constants.PRIDE_CORN_EMOJI : Constants.CORN_EMOJI;
            var economy = _services.GetRequiredService<GuildTracker>();
            var userInfo = economy.LookupGuild(Context.Guild).GetUserInfo(user ?? Context.User);
            var stringId = user is null ? "you have" :
                    user is not SocketGuildUser guildUser ? $"{user} has" :
                    $"{guildUser.DisplayName} ({guildUser}) has";
            await RespondAsync($"{cornEmoji} {stringId} {userInfo.CornCount} corn {cornEmoji}");
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
                var cornEmoji = Utility.GetCurrentEvent() == Constants.CornEvent.PRIDE ?
                    Constants.PRIDE_CORN_EMOJI : Constants.CORN_EMOJI;
                var amount = await user.PerformDaily();
                await RespondAsync($"{cornEmoji} you have shucked {amount} corn today. you now have {user.CornCount} corn {cornEmoji}");
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

            var embed = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithTitle("Top corn havers:")
                .WithDescription(await economy.GetLeaderboardsString())
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

        [EnabledInDm(true)]
        [SlashCommand("total", "Gets the total corn count across all servers")]
        public async Task Total()
        {
            var cornEmoji = Utility.GetCurrentEvent() == Constants.CornEvent.PRIDE ?
                Constants.PRIDE_CORN_EMOJI : Constants.CORN_EMOJI;
            long total = _services.GetRequiredService<GuildTracker>().GetTotalCorn();
            await RespondAsync($"{cornEmoji} a total of {total:n0} corn has been shucked across all servers {cornEmoji}");
        }

        [EnabledInDm(false)]
        [SlashCommand("stats", "Gets an overview of your recent corn shucking")]
        public async Task Stats([Summary(description: "user to lookup")] IUser? user = null)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            user ??= Context.User;
            var guildInfo = economy.LookupGuild(Context.Guild);
            var userInfo = guildInfo.GetUserInfo(user);

            var history = await economy.GetHistory(userInfo.UserId);

            EmbedFieldBuilder[] fields = new EmbedFieldBuilder[]
            {
                new EmbedFieldBuilder()
                    .WithName("Daily Count")
                    .WithValue($"{history.GetDailyCount(guildInfo.GuildId):n0} " +
                        $"({history.GetGlobalDailyCount():n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Daily Average")
                    .WithValue($"{history.GetDailyAverage(guildInfo.GuildId):n2} " +
                        $"({history.GetGlobalDailyAverage():n2})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Daily Total")
                    .WithValue($"{history.GetDailyTotal(guildInfo.GuildId):n0} " +
                        $"({history.GetGlobalDailyTotal():n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Longest Daily Streak")
                    .WithValue($"{history.GetLongestDailyStreak(guildInfo.GuildId):n0} " +
                        $"({history.GetGlobalLongestDailyStreak():n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Current Daily Streak")
                    .WithValue($"{history.GetCurrentDailyStreak(guildInfo.GuildId):n0} " +
                        $"({history.GetGlobalCurrentDailyStreak():n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Message Total")
                    .WithValue($"{history.GetMessageTotal(guildInfo.GuildId):n0} " +
                        $"({history.GetGlobalMessageTotal():n0})")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Server Total")
                    .WithValue(userInfo.CornCount.ToString("n0"))
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
                .WithDescription("*server (global)*")
                .WithAuthor(author)
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithCurrentTimestamp()
                .WithColor(Color.Gold)
                .WithFields(fields)
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

        [EnabledInDm(false)]
        [SlashCommand("cornucopia", "play a game of slots to gamble your corn")]
        public async Task Cornucopia([Summary(description: "amount of corn to gamble")] long amount)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            var userInfo = economy.LookupGuild(Context.Guild).GetUserInfo(Context.User);
            var random = _services.GetRequiredService<Random>();

            if (amount < 1)
                await RespondAsync("you can't gamble less than 1 corn.");
            else if (amount > userInfo.CornCount)
                await RespondAsync("you don't have that much corn.");
            else
            {
                SlotMachine slotMachine = new(3, amount, random);

                var author = new EmbedAuthorBuilder()
                    .WithIconUrl(Context.User.GetAvatarUrl())
                    .WithName(Context.User.ToString());
                var embed = new EmbedBuilder()
                    .WithTitle($"**Cornucopia**")
                    .WithDescription(slotMachine.RenderToString())
                    .WithAuthor(author)
                    .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                    .WithCurrentTimestamp()
                    .WithColor(Color.Gold);

                await RespondAsync(embeds: new Embed[] { embed.Build() });
                userInfo.CornCount -= amount;
                userInfo.CornCount += slotMachine.GetWinnings();
                await userInfo.Save();
                while (slotMachine.RevealProgress < slotMachine.Size)
                {
                    await Task.Delay(1000);
                    slotMachine.RevealProgress++;
                    embed.Description = slotMachine.RenderToString();
                    await ModifyOriginalResponseAsync(m => m.Embed = embed.Build());
                }

            }
        }

    }
}
