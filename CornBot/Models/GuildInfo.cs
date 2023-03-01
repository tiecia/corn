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

        public async Task<List<IUser>> GetLeaderboards()
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
            var leaderboard = new List<IUser>(10);

            var client = _services.GetRequiredService<DiscordSocketClient>();
            var guild = client.GetGuild(GuildId);
            await guild.DownloadUsersAsync();

            foreach (var userInfo in allUsers.Reverse())
            {
                var user = guild.Users.First(u => u.Id == userInfo.UserId) ?? await client.GetUserAsync(userInfo.UserId);
                if (user != null)
                    leaderboard.Add(user);
                if (leaderboard.Count >= 10)
                    break;
            }

            return leaderboard;
        }

        public async Task Save()
        {
            await GuildTracker.SaveGuildInfo(this);
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
