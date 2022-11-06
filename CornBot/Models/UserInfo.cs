using CornBot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Models
{
    public class UserInfo : IComparable<UserInfo>
    {

        public GuildInfo Guild { get; init; }

        public ulong UserId { get; private set; }
        public long CornCount { get; set; }
        public bool HasClaimedDaily { get; set; }
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
        }

        public UserInfo(GuildInfo guild, ulong userId, IServiceProvider services)
            : this(guild, userId, 0, false, 1.0, DateTime.UtcNow, services)
        {
        }

        public int CompareTo(UserInfo? other)
        {
            if (other == null) return 1;
            return CornCount.CompareTo(other.CornCount);
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
            var random = _services.GetRequiredService<Random>();
            var amount = random.Next(20, 31);
            CornCount += amount;
            HasClaimedDaily = true;
            await LogAction(UserHistory.ActionType.DAILY, amount);
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

    }
}
