using CornBot.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Models
{
    public class GuildTracker
    {

        private readonly GuildTrackerSerializer _serializer;
        private readonly IServiceProvider _services;

        public Dictionary<ulong, GuildInfo> Guilds { get; private set; } = new();

        public GuildTracker(GuildTrackerSerializer serializer, IServiceProvider services)
        {
            _serializer = serializer;
            _services = services;
        }

        public GuildTracker(Dictionary<ulong, GuildInfo> guilds, GuildTrackerSerializer serializer, IServiceProvider services)
            : this(serializer, services)
        {
            Guilds = guilds;
        }

        public GuildInfo LookupGuild(SocketGuild guild)
        {
            if (!Guilds.ContainsKey(guild.Id))
                Guilds.Add(guild.Id, new(this, guild.Id, _services));
            return Guilds[guild.Id];
        }

        public async Task ResetDailies()
        {
            foreach (var guild in Guilds.Values)
            {
                foreach (var user in guild.Users.Values)
                {
                    user.HasClaimedDaily = false;
                }
            }
            await _serializer.ResetAllDailies();
        }

        public async Task StartDailyResetLoop()
        {
            var client = _services.GetRequiredService<CornClient>();

            // a little messy but the best way i've come up with for getting the start of the next day
            TimeSpan offset = new(hours: -7, minutes: 0, seconds: 0);
            DateTimeOffset nextReset = new(DateTime.Now, offset);
            nextReset = nextReset.AddDays(1);
            nextReset = new(nextReset.Year, nextReset.Month, nextReset.Day, hour: 0, minute: 0, second: 0, offset);
            while (true)
            {
                var timeUntilReset = nextReset - new DateTimeOffset(DateTime.Now, offset);
                await client.Log(new LogMessage(LogSeverity.Info, "DailyReset",
                    $"Time until next reset: {timeUntilReset}"));
                await Task.Delay(timeUntilReset);
                await ResetDailies();
                await client.Log(new LogMessage(LogSeverity.Info, "DailyReset", "Daily reset performed successfully!"));
                nextReset = nextReset.AddDays(1);
            }
        }

        public async Task LoadFromSerializer()
        {
            Guilds = await _serializer.Load(this);
        }

        public async Task SaveUserInfo(UserInfo user)
        {
            await _serializer.AddOrUpdateGuild(user.Guild);
            await _serializer.AddOrUpdateUser(user);
        }

        public async Task SaveGuildInfo(GuildInfo guild)
        {
            await _serializer.AddOrUpdateGuild(guild);
        }

        public async Task LogAction(UserInfo user, UserHistory.ActionType type, long value)
        {
            await _serializer.LogAction(user, type, value);
        }

    }
}
