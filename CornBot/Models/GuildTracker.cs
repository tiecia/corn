using CornBot.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CornBot.Services;
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
                Guilds.Add(guildId, new(this, guildId, 0, 0, _services));
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

        public async Task ResetDailies()
        {
            foreach (var guild in Guilds.Values)
            {
                guild.Dailies = 0;
                foreach (var user in guild.Users.Values)
                {
                    user.HasClaimedDaily = false;
                }
            }
            await _serializer.ResetAllDailies();
        }

        public async Task SendAllMonthlyRecaps()
        {
            foreach (var guild in Guilds.Values)
                await guild.SendMonthlyRecap();
        }

        public async Task StartDailyResetLoop()
        {
            var client = _services.GetRequiredService<CornClient>();

            var lastReset = Utility.GetAdjustedTimestamp();
            var nextReset = lastReset.AddDays(1);
            nextReset = new(nextReset.Year, nextReset.Month, nextReset.Day, hour: 0, minute: 0, second: 0, Constants.TZ_OFFSET);
            while (true)
            {
                // wait until the next day
                var timeUntilReset = nextReset - Utility.GetAdjustedTimestamp();
                await client.Log(new LogMessage(LogSeverity.Info, "DailyReset",
                    $"Time until next reset: {timeUntilReset}"));
                await Task.Delay(timeUntilReset);

                // create a backup (with date info corresponding to the previous day)
                await _serializer.BackupDatabase($"./backups/{lastReset.Year}/{lastReset.Month}/backup-{lastReset.Day}.db");

                // either reset dailies or the entire leaderboard (depending on whether end of month)
                if (lastReset.Month == nextReset.Month)
                {
                    await ResetDailies();
                    await client.Log(new LogMessage(LogSeverity.Info, "DailyReset", "Daily reset performed successfully!"));
                }
                else
                {
                    await SendAllMonthlyRecaps();
                    
                    await _serializer.ClearDatabase();
                    Guilds = new();
                    await client.Log(new LogMessage(LogSeverity.Info, "DailyReset", "Monthly reset performed successfully!"));
                    await client.Log(new LogMessage(LogSeverity.Info, "DailyReset", "CORN HAS BEEN RESET FOR THE MONTH!"));
                }
                
                // update next and last reset in lockstep
                lastReset = nextReset;
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
            await _serializer.LogAction(user, type, value, Utility.GetAdjustedTimestamp());
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
