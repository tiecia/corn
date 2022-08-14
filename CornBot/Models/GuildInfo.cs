using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Models
{
    public class GuildInfo
    {

        public Dictionary<ulong, UserInfo> Users { get; init; } = new();
        public SocketGuild Guild { get; init; }

        private readonly IServiceProvider _services;

        public GuildInfo(SocketGuild guild, IServiceProvider services)
        {
            Guild = guild;
            _services = services;
        }

        public GuildInfo(SocketGuild guild, Dictionary<ulong, UserInfo> users, IServiceProvider services)
        {
            Guild = guild;
            Users = users;
            _services = services;
        }

        public UserInfo GetUserInfo(IUser user)
        {
            if (!Users.ContainsKey(user.Id))
                Users.Add(user.Id, new(user.Id, _services));
            return Users[user.Id];
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
            await Guild.DownloadUsersAsync();

            foreach (var userInfo in allUsers.Reverse())
            {
                var user = Guild.Users.First(u => u.Id == userInfo.UserId) ?? await client.GetUserAsync(userInfo.UserId);
                if (user != null)
                    leaderboard.Add(user);
                if (leaderboard.Count >= 10)
                    break;
            }

            return leaderboard;
        }

    }
}
