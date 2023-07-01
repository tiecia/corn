using CornBot.Utilities;
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
            DAILY,
            CORNUCOPIA,
        }

        public struct HistoryEntry
        {
            public ulong Id;
            public ulong UserId;
            public ulong GuildId;
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
                    GuildId == other.GuildId &&
                    Type == other.Type &&
                    Value == other.Value &&
                    Timestamp == other.Timestamp;
            }
        }

        public ulong UserId { get; init; }
        public List<HistoryEntry> Entries { get; init; }
        public Dictionary<ulong, HashSet<int>> Dailies { get; init; }

        public UserHistory(ulong userId, List<HistoryEntry> entries)
        {
            UserId = userId;
            Entries = new();
            Dailies = new();
            foreach (var entry in entries)
                AddAction(entry); // make sure dailies get properly added
        }

        public UserHistory(ulong userId) : this(userId, new())
        { }

        public void AddAction(HistoryEntry entry)
        {
            this.Entries.Add(entry);
            if (entry.Type == ActionType.DAILY)
            {
                if (!Dailies.ContainsKey(entry.GuildId))
                    Dailies[entry.GuildId] = new();
                Dailies[entry.GuildId].Add(entry.Timestamp.Day);
            }
        }

        public int GetDailyCount(ulong guildId)
        {
            return Entries.Where(e => e.Type == ActionType.DAILY && e.GuildId == guildId).Count();
        }

        public int GetGlobalDailyCount()
        {
            return Entries.Where(e => e.Type == ActionType.DAILY).Count();
        }

        public double GetDailyAverage(ulong guildId)
        {
            return GetDailyCount(guildId) == 0 ? 0.0 :
                Entries.Where(e => e.Type == ActionType.DAILY).Where(e => e.GuildId == guildId).Average(e => e.Value);
        }

        public double GetGlobalDailyAverage()
        {
            return GetGlobalDailyCount() == 0 ? 0.0 :
                Entries.Where(e => e.Type == ActionType.DAILY).Average(e => e.Value);
        }

        public double GetDailyTotal(ulong guildId)
        {
            return Entries.Where(e => e.Type == ActionType.DAILY).Where(e => e.GuildId == guildId).Sum(e => e.Value);
        }

        public double GetGlobalDailyTotal()
        {
            return Entries.Where(e => e.Type == ActionType.DAILY).Sum(e => e.Value);
        }

        public double GetMessageTotal(ulong guildId)
        {
            return Entries.Where(e => e.Type == ActionType.MESSAGE).Where(e => e.GuildId == guildId).Sum(e => e.Value);
        }

        public double GetGlobalMessageTotal()
        {
            return Entries.Where(e => e.Type == ActionType.MESSAGE).Sum(e => e.Value);
        }

        public int GetLongestDailyStreak(ulong guildId)
        {
            int longestStreak = 0;
            int currentStreak = 0;
            for (int i = 1; i < 33; i++)
            {
                if (DailyWasDone(guildId, i))
                    currentStreak++;
                else
                {
                    if (currentStreak > longestStreak)
                        longestStreak = currentStreak;
                    currentStreak = 0;
                }
            }
            return longestStreak;
        }

        public int GetGlobalLongestDailyStreak()
        {
            return GetLongestDailyStreak(0);
        }

        public int GetCurrentDailyStreak(ulong guildId)
        {
            var now = Utility.GetAdjustedTimestamp();
            // whether or not a daily was done today
            // should not affect the validity of the streak
            int currentStreak = DailyWasDone(guildId, now.Day) ? 1 : 0;
            for (int i = now.Day - 1; i > 0; i--)
            {
                if (!DailyWasDone(guildId, i))
                    break;
                currentStreak++;
            }
            return currentStreak;
        }

        public int GetGlobalCurrentDailyStreak()
        {
            return GetCurrentDailyStreak(0);
        }

        public bool DailyWasDone(ulong guildId, int day)
        {
            if (guildId == 0)
                return Dailies.Any(pair => pair.Value.Contains(day));
            else
            {
                if (!Dailies.ContainsKey(guildId))
                    Dailies[guildId] = new();
                return Dailies[guildId].Contains(day);
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserId, Entries);
        }

        public override bool Equals(object? obj)
        {
            return obj is UserHistory other &&
                UserId == other.UserId &&
                Entries == other.Entries;
        }

    }
}
