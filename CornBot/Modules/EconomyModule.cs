﻿using CornBot.Models;
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
        [SlashCommand("beans", "Gets your total bean count")]
        public async Task Corn([Summary(description: "user to lookup")] IUser? user = null)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            var userInfo = economy.LookupGuild(Context.Guild).GetUserInfo(user ?? Context.User);
            var stringId = user is null ? "you have" :
                    user is not SocketGuildUser guildUser ? $"{user} has" :
                    $"{guildUser.DisplayName} ({guildUser}) has";
            await RespondAsync($"{Constants.BEAN_EMOJI} {stringId} {userInfo.CornCount} beans {Constants.BEAN_EMOJI}");
        }

        [EnabledInDm(false)]
        [SlashCommand("daily", "Performs your daily canning of beans")]
        public async Task Daily()
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            var user = economy.LookupGuild(Context.Guild).GetUserInfo(Context.User);

            if (user.HasClaimedDaily)
                await RespondAsync("what are you trying to do, spam the daily command?");
            else
            {
                var amount = await user.PerformDaily();
                await RespondAsync($"{Constants.BEAN_EMOJI} you have canned {amount} beans today. you now have {user.CornCount} beans {Constants.BEAN_EMOJI}");
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("lb", "Alias for /leaderboards")]
        public async Task LeaderboardsAlias() => await Leaderboards();

        [EnabledInDm(false)]
        [SlashCommand("leaderboards", "Displays the top bean havers in the guild")]
        public async Task Leaderboards()
        {
            var economy = _services.GetRequiredService<GuildTracker>().LookupGuild(Context.Guild);
            var topUsers = await economy.GetLeaderboards();
            var response = new StringBuilder();
            long lastCornAmount = 0;
            int lastPlacementNumber = 0;

            for (int i = 0; i < topUsers.Count; i++)
            {
                var user = topUsers[i];
                var userData = economy.GetUserInfo(user);
                var cornAmount = userData.CornCount;
                int placement = i + 1;
                if (cornAmount == lastCornAmount) placement = lastPlacementNumber;
                else lastPlacementNumber = placement;
                var stringId = user is not SocketGuildUser guildUser ?
                    user.ToString() :
                    $"{guildUser.DisplayName} ({guildUser})";
                var suffix = userData.HasClaimedDaily ? "" : $" {Constants.CALENDAR_EMOJI}";
                response.AppendLine($"{placement} : {stringId} - {cornAmount} beans{suffix}");
                lastCornAmount = cornAmount;
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.DarkOrange)
                .WithThumbnailUrl(Constants.BEAN_THUMBNAIL_URL)
                .WithTitle("Top bean havers:")
                .WithDescription(response.ToString())
                .WithCurrentTimestamp()
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

        [EnabledInDm(true)]
        [SlashCommand("total", "Gets the total bean count across all servers")]
        public async Task Total()
        {
            long total = _services.GetRequiredService<GuildTracker>().GetTotalCorn();
            await RespondAsync($"{Constants.BEAN_EMOJI} a total of {total:n0} beans have been canned across all servers {Constants.BEAN_EMOJI}");
        }

        [EnabledInDm(false)]
        [SlashCommand("stats", "Gets an overview of your recent bean canning")]
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
                .WithTitle($"{displayName}'s bean stats")
                .WithDescription("*server (global)*")
                .WithAuthor(author)
                .WithThumbnailUrl(Constants.BEAN_THUMBNAIL_URL)
                .WithCurrentTimestamp()
                .WithColor(Color.DarkOrange)
                .WithFields(fields)
                .Build();

            await RespondAsync(embeds: new Embed[] { embed });
        }

    }
}
