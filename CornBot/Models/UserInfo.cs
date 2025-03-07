using CornBot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CornBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Rest;
using Microsoft.Extensions.Configuration;

namespace CornBot.Models
{
    public class UserInfo : IComparable<UserInfo>
    {

        public GuildInfo Guild { get; init; }

        public ulong UserId { get; private set; }
        public string Username { get; private set; }
        
        private long _cornCount;

        public long CornCount
        {
            get => _cornCount;
            set
            {
                if (Username != null)
                {
                    _services.GetRequiredService<MqttService>().SendCornChangedNotificationAsync(Username);
                }
                _cornCount = value;
            }
        }

        private bool _hasClaimedDaily;

        public bool HasClaimedDaily
        {
            get => _hasClaimedDaily;
            set
            {
                if (Username != null)
                {
                    _services.GetRequiredService<MqttService>().SendShuckStatusChangedNotificationAsync(Username);
                }
                _hasClaimedDaily = value;
            }
        }
        public DateTime CornMultiplierLastEdit { get; private set; }
        public double CornMultiplier
        {
            get
            {
                var timeSinceEdit = DateTime.UtcNow - CornMultiplierLastEdit;
                _cornMultiplier = Math.Min(1.0, _cornMultiplier + timeSinceEdit.TotalSeconds * (1.0 / Constants.CORN_RECHARGE_TIME));
                CornMultiplierLastEdit = DateTime.UtcNow;
                return _cornMultiplier;
            }
            private set
            {
                _cornMultiplier = value;
            }
        }
        private double _cornMultiplier;

        private readonly IServiceProvider _services;

        public UserInfo(GuildInfo guild, ulong userId, long cornCount, bool hasClaimedDaily, double cornMultiplier, DateTime cornMultiplierLastEdit, IServiceProvider services)
        {
            Guild = guild;
            UserId = userId;
            CornCount = cornCount;
            HasClaimedDaily = hasClaimedDaily;
            _cornMultiplier = cornMultiplier;
            CornMultiplierLastEdit = cornMultiplierLastEdit;
            _services = services;

            var restClient = new DiscordRestClient();
            restClient.LoginAsync(TokenType.Bot, CornClient.BOT_KEY).Wait();
            Username = restClient.GetUserAsync(userId).Result.Username;

        }

        public UserInfo(GuildInfo guild, ulong userId, IServiceProvider services)
            : this(guild, userId, 0, false, 1.0, DateTime.UtcNow, services)
        {
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public int CompareTo(UserInfo? other)
        {
            if (other == null) return 1;
            int result = CornCount.CompareTo(other.CornCount);
            return result == 0 ? 1 : result;
        }

        public async Task Save()
        {
            await Guild.GuildTracker.SaveUserInfo(this);
        }

        public async Task LogAction(UserHistory.ActionType type, long value)
        {
            await Guild.GuildTracker.LogAction(this, type, value);
        }

        public async Task<long> PerformDaily()
        {
            var currentEvent = Utility.GetCurrentEvent();
            var amount = (int)Math.Round(SimpleRNG.GetNormal(
                Constants.CORN_DAILY_MEAN, Constants.CORN_DAILY_STD_DEV));

            if (currentEvent == Constants.CornEvent.SHUCKING_STREAKS)
            {
                var history = await Guild.GuildTracker.GetHistory(UserId);
                amount += Math.Min(history.GetCurrentDailyStreak(Guild.GuildId) * 2, 10);
            }

            CornCount += amount;
            HasClaimedDaily = true;
            Guild.Dailies += 1;
            await Guild.Save();
            await LogAction(UserHistory.ActionType.DAILY, amount);
            if (Utility.GetCurrentEvent() == Constants.CornEvent.SHARED_SHUCKING &&
                Guild.Dailies <= Constants.SHARED_SHUCKING_MAX_BONUS)
                await ProcessSharedShuckingDaily();
            await Save();
            return amount;
        }

        public async Task<long> AddCornWithPenalty(long amount)
        {
            if (CornMultiplier <= 0.0)
            {
                // user is below cooldown threshold, don't give corn and max cooldown
                _cornMultiplier = -1.0;
                CornMultiplierLastEdit = DateTime.UtcNow;
                return 0;
            }
            // set penalty before corn is modified
            var penalty = (double)amount / 15;
            // set corn to use the multiplier
            amount = (long)Math.Round(amount * CornMultiplier);
            // apply penalty
            _cornMultiplier -= penalty;
            CornCount += amount;
            await LogAction(UserHistory.ActionType.MESSAGE, amount);
            await Save();
            return amount;
        }

        public async Task ProcessSharedShuckingDaily()
        {
            foreach (var user in Guild.Users.Values)
            {
                user.CornCount += 1;
                if (user != this)
                    await user.Save();
            }
        }

        public async Task UpdateForGambling(long investment, long returns)
        {
            CornCount -= investment;
            CornCount += returns;
            await LogAction(UserHistory.ActionType.CORNUCOPIA, -investment);
            await LogAction(UserHistory.ActionType.CORNUCOPIA, returns);
            await Save();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserId, CornCount, HasClaimedDaily, CornMultiplier, CornMultiplierLastEdit);
        }

        public override bool Equals(object? obj)
        {
            return obj is UserInfo other &&
                UserId == other.UserId &&
                CornCount == other.CornCount &&
                HasClaimedDaily == other.HasClaimedDaily &&
                CornMultiplier == other.CornMultiplier &&
                CornMultiplierLastEdit == other.CornMultiplierLastEdit;
        }

    }
}
