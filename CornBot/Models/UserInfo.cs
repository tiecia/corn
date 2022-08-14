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

        public ulong UserId { get; private set; }
        public long CornCount { get; set; } = 0;
        public bool HasClaimedDaily { get; set; } = false;
        public double CornMultiplier
        {
            get
            {
                var timeSinceEdit = DateTime.UtcNow - _cornMultiplierLastEdit;
                _cornMultiplier = Math.Min(1.0, _cornMultiplier + timeSinceEdit.TotalSeconds * (1.0 / Constants.CORN_RECHARGE_TIME));
                return _cornMultiplier;
            }
            private set
            {
                _cornMultiplier = value;
            }
        }
        private double _cornMultiplier = 1.0;
        private DateTime _cornMultiplierLastEdit = DateTime.UtcNow;

        private readonly IServiceProvider _services;

        public UserInfo(ulong userId, long cornCount, bool hasClaimedDaily, double cornMultiplier, DateTime cornMultiplierLastEdit, IServiceProvider services)
        {
            UserId = userId;
            CornCount = cornCount;
            HasClaimedDaily = hasClaimedDaily;
            _cornMultiplier = cornMultiplier;
            _cornMultiplierLastEdit = cornMultiplierLastEdit;
            _services = services;
        }

        public UserInfo(ulong pUserId, IServiceProvider services)
        {
            UserId = pUserId;
            _services = services;
        }

        public int CompareTo(UserInfo? other)
        {
            if (other == null) return 1;
            return CornCount.CompareTo(other.CornCount);
        }

        public long PerformDaily()
        {
            var random = _services.GetRequiredService<Random>();
            var amount = random.Next(20, 31);
            CornCount += amount;
            HasClaimedDaily = true;
            return amount;
        }

        public long AddCornWithPenalty(long amount)
        {
            if (CornMultiplier <= 0.0)
            {
                // user is below cooldown threshold, don't give corn and max cooldown
                _cornMultiplier = -1.0;
                _cornMultiplierLastEdit = DateTime.UtcNow;
                return 0;
            }
            // set penalty before corn is modified
            var penalty = (double)amount / 15;
            // set corn to use the multiplier
            amount = (long)Math.Round(amount * CornMultiplier);
            // apply penalty
            _cornMultiplier -= penalty;
            _cornMultiplierLastEdit = DateTime.UtcNow;
            CornCount += amount;
            return amount;
        }

    }
}
