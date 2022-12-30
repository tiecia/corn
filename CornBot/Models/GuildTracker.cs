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
using CornBot.Utilities;

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

        public GuildInfo LookupGuild(ulong guildId)
        {
            if (!Guilds.ContainsKey(guildId))
                Guilds.Add(guildId, new(this, guildId, _services));
            return Guilds[guildId];
        }

        public GuildInfo LookupGuild(SocketGuild guild)
        {
            return LookupGuild(guild.Id);
        }

        public long GetTotalCorn()
        {
            return Guilds.Values.Sum(g => g.GetTotalCorn());
        }

        public long GetTotalCorn(IUser user)
        {
            return Guilds.Values.Where(g => g.UserExists(user)).Sum(g => g.GetUserInfo(user).CornCount);
        }

        public static DateTimeOffset GetAdjustedTimestamp()
        {
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            return new(now + Constants.TZ_OFFSET, Constants.TZ_OFFSET);
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

            var nextReset = GetAdjustedTimestamp().AddDays(1);
            nextReset = new(nextReset.Year, nextReset.Month, nextReset.Day, hour: 0, minute: 0, second: 0, Constants.TZ_OFFSET);
            while (true)
            {
                var timeUntilReset = nextReset - GetAdjustedTimestamp();
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
            await _serializer.LogAction(user, type, value, GetAdjustedTimestamp());
        }

        public async Task<UserHistory> GetHistory(ulong userId)
        {
            return await _serializer.GetHistory(userId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Guilds);
        }

        public override bool Equals(object? obj)
        {
            return obj is GuildTracker other && Guilds == other.Guilds;
        }

    }
}
