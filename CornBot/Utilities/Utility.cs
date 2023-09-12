using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornBot.Utilities
{
    public class Utility
    {

        public static DateTimeOffset GetAdjustedTimestamp()
        {
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            return new(now + Constants.TZ_OFFSET, Constants.TZ_OFFSET);
        }

        public static Constants.CornEvent GetCurrentEvent()
        {
            return GetAdjustedTimestamp().Month switch
            {
                1 => Constants.CornEvent.SHARED_SHUCKING,
                2 => Constants.CornEvent.SHUCKING_STREAKS,
                3 => Constants.CornEvent.NORMAL_DISTRIBUTION_SHUCKING,
                6 => Constants.CornEvent.PRIDE,
                _ => Constants.CornEvent.NONE,
            };
        }

        public static string GetUserDisplayString(IUser user, bool includeUsername)
        {
            string displayName = user is SocketGuildUser guildUser ?
                guildUser.DisplayName :
                (user.GlobalName ?? user.Username);

            if (includeUsername)
            {
                return displayName == user.Username ? user.Username : $"{displayName} ({user.Username})";
            }
            else
            {
                return displayName;
            }
        } 

    }
}
