using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CornBot.Utilities;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Models
{
    public class GuildInfo
    {

        public GuildTracker GuildTracker { get; init; }
        public ulong GuildId { get; init; }
        public int Dailies { get; set; }
        public ulong AnnouncementChannel { get; set; }
        public Dictionary<ulong, UserInfo> Users { get; init; } = new();

        private readonly IServiceProvider _services;

        public GuildInfo(GuildTracker tracker, ulong guildId, int dailies,
            ulong announcementChannel, IServiceProvider services)
            : this(tracker, guildId, dailies, announcementChannel, new(), services)
        {
        }

        public GuildInfo(GuildTracker tracker, ulong guildId, int dailies, ulong announcementChannel,
            Dictionary<ulong, UserInfo> users, IServiceProvider services)
        {
            GuildTracker = tracker;
            GuildId = guildId;
            Dailies = dailies;
            AnnouncementChannel = announcementChannel;
            Users = users;
            _services = services;
        }

        public void AddUserInfo(UserInfo user)
        {
            if (!Users.ContainsKey(user.UserId))
                Users.Add(user.UserId, user);
        }

        public UserInfo GetUserInfo(ulong userId)
        {
            if (!Users.ContainsKey(userId))
            {
                var newUser = new UserInfo(this, userId, _services);
                if (Utility.GetCurrentEvent() == Constants.CornEvent.SHARED_SHUCKING)
                    newUser.CornCount += Math.Min(Dailies, Constants.SHARED_SHUCKING_MAX_BONUS);
                Users.Add(userId, newUser);
            }
            return Users[userId];
        }

        public UserInfo GetUserInfo(IUser user)
        {
            return GetUserInfo(user.Id);
        }

        public bool UserExists(IUser user)
        {
            return Users.ContainsKey(user.Id);
        }

        public long GetTotalCorn()
        {
            return Users.Values.Sum(u => u.CornCount);
        }

        public async Task<List<IUser>> GetLeaderboards(int count = 10)
        {
            /*
             * Downloads a list of all users in the guild, then match them to the top 10 users of corn
             * based on the local corn database.
             * 
             * I am not sure this is the best solution. The alternative is to simply request information
             * for the top 10 (or more if some are unavailable) users in the guild as they come up on the
             * leaderboards instead of requesting everything and looking them up locally. This would be
             * much more efficient in terms of bandwidth and memory, but it would have the downside of many
             * more requests.
             * 
             * At this point it is not a problem, especially with command limits, but I feel like this just
             * makes it more future-proof. I don't know help
             */
            var allUsers = new SortedSet<UserInfo>(Users.Values);
            var leaderboard = new List<IUser>(count);

            var client = _services.GetRequiredService<DiscordSocketClient>();
            var guild = client.GetGuild(GuildId);
            await guild.DownloadUsersAsync();

            foreach (var userInfo in allUsers.Reverse())
            {
                var user = guild.Users.First(u => u.Id == userInfo.UserId) ?? await client.GetUserAsync(userInfo.UserId);
                if (user != null)
                    leaderboard.Add(user);
                if (leaderboard.Count >= count)
                    break;
            }

            return leaderboard;
        }

        public async Task Save()
        {
            await GuildTracker.SaveGuildInfo(this);
        }

        public async Task<string> GetLeaderboardsString(int count = 10, bool addSuffix = true)
        {
            var topUsers = await GetLeaderboards(count);
            var response = new StringBuilder();
            long lastCornAmount = 0;
            int lastPlacementNumber = 0;

            for (int i = 0; i < topUsers.Count; i++)
            {
                var user = topUsers[i];
                var userData = GetUserInfo(user);
                var cornAmount = userData.CornCount;

                int placement = i + 1;
                if (cornAmount == lastCornAmount)
                    placement = lastPlacementNumber;
                else
                    lastPlacementNumber = placement;

                var stringId = user is not SocketGuildUser guildUser ?
                    user.ToString() :
                    $"{guildUser.DisplayName} ({guildUser})";
                
                var suffix = (!addSuffix) || userData.HasClaimedDaily ?
                    "" : $" {Constants.CALENDAR_EMOJI}";

                response.AppendLine($"{placement} : {stringId} - {cornAmount} corn{suffix}");

                lastCornAmount = cornAmount;
            }

            return response.ToString();
        }

        public async Task SendMonthlyRecap()
        {
            // no announcements channel has been set
            if (AnnouncementChannel == 0) return;

            var client = _services.GetRequiredService<DiscordSocketClient>();
            var guild = client.GetGuild(GuildId);
            var channel = guild?.GetChannel(AnnouncementChannel);
            // the specified channel could not be found in the guild
            if (channel == null) return;
            if (channel is not ITextChannel textChannel) return;

            var leaderboards = await GetLeaderboardsString(3, false);
            var serverShucks = GetTotalCorn();
            var globalShucks = GuildTracker.GetTotalCorn();

            UserHistory? bestLuck = null;
            string? bestLuckName = null;
            UserHistory? worstLuck = null;
            string? worstLuckName = null;

            foreach (UserInfo user in Users.Values)
            {
                var userHistory = await GuildTracker.GetHistory(user.UserId);

                // for statistical significance
                if (userHistory.GetDailyTotal(GuildId) < 5) continue;

                if (bestLuck == null || 
                    userHistory.GetDailyAverage(GuildId) > bestLuck.GetDailyAverage(GuildId))
                {
                    IUser userObj = guild?.GetUser(user.UserId) ?? await client.GetUserAsync(user.UserId);
                    if (userObj != null)
                    {
                        bestLuck = userHistory;
                        bestLuckName = userObj is not SocketGuildUser guildUser ?
                            user.ToString() :
                            $"{guildUser.DisplayName} ({guildUser})";
                    }
                }

                if (worstLuck == null ||
                    userHistory.GetDailyAverage(GuildId) < worstLuck.GetDailyAverage(GuildId))
                {
                    // TODO: remove nasty copied code
                    IUser userObj = guild?.GetUser(user.UserId) ?? await client.GetUserAsync(user.UserId);
                    if (userObj != null)
                    {
                        worstLuck = userHistory;
                        worstLuckName = userObj is not SocketGuildUser guildUser ?
                            user.ToString() :
                            $"{guildUser.DisplayName} ({guildUser})";
                    }
                }
            }

            var response = new StringBuilder();
            response.AppendLine($"A total of {serverShucks} corn was shucked in this server, out of {globalShucks} globally!");
            response.AppendLine($"");
            response.AppendLine($"Top 3 shuckers:");
            response.AppendLine(leaderboards);
            if (bestLuckName != null && bestLuck != null)
            {
                response.AppendLine($"The most lucky shucker in the server was {bestLuckName} with a daily average of {bestLuck.GetDailyAverage(GuildId)}!");
                response.AppendLine($"");
            }
            if (worstLuckName != null && worstLuck != null)
            {
                response.AppendLine($"Unfortunately, there was also {worstLuckName} with a daily average of {worstLuck.GetDailyAverage(GuildId)}.");
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithThumbnailUrl(Constants.CORN_THUMBNAIL_URL)
                .WithTitle($"{Constants.CORN_EMOJI} Monthly recap {Constants.CORN_EMOJI}")
                .WithDescription(response.ToString())
                .WithCurrentTimestamp()
                .Build();

            await textChannel.SendMessageAsync(embeds: new Embed[] { embed });
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GuildId, Users);
        }

        public override bool Equals(object? obj)
        {
            return obj is GuildInfo other &&
                GuildId == other.GuildId &&
                Users == other.Users;
        }

    }
}
