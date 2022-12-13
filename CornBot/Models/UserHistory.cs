using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornBot.Models
{
    public class UserHistory
    {

        public enum ActionType
        {
            MESSAGE,
            DAILY
        }

        public struct HistoryEntry
        {
            public ulong Id;
            public ulong UserId;
            public ActionType Type;
            public long Value;
            public DateTimeOffset Timestamp;

            public override int GetHashCode()
            {
                return HashCode.Combine(Id, UserId, Type, Value, Timestamp);
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj is HistoryEntry other &&
                    Id == other.Id &&
                    UserId == other.UserId &&
                    Type == other.Type &&
                    Value == other.Value &&
                    Timestamp == other.Timestamp;
            }
        }

        public UserInfo User { get; init; }
        public List<HistoryEntry> Entries { get; init; }

        public UserHistory(UserInfo user, List<HistoryEntry> entries)
        {
            User = user;
            Entries = entries;
        }

        public UserHistory(UserInfo user) : this(user, new())
        { }

        public void AddAction(HistoryEntry entry)
        {
            this.Entries.Add(entry);
        }

        public int GetDailyCount()
        {
            return Entries.Where(e => e.Type == ActionType.DAILY).Count();
        }

        public double GetDailyAverage()
        {
            return GetDailyCount() == 0 ? 0.0 :
                Entries.Where(e => e.Type == ActionType.DAILY).Average(e => e.Value);
        }

        public double GetDailyTotal()
        {
            return Entries.Where(e => e.Type == ActionType.DAILY).Sum(e => e.Value);
        }

        public double GetMessageTotal()
        {
            return Entries.Where(e => e.Type == ActionType.MESSAGE).Sum(e => e.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(User, Entries);
        }

        public override bool Equals(object? obj)
        {
            return obj is UserHistory other &&
                User == other.User &&
                Entries == other.Entries;
        }

    }
}
