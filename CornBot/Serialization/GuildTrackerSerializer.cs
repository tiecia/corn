using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CornBot.Models;
using Microsoft.Data.Sqlite;
using Discord.WebSocket;

namespace CornBot.Serialization
{
    public class GuildTrackerSerializer
    {

        private struct RawUserInfo
        {
            public ulong UserId;
            public ulong GuildId;
            public long CornCount;
            public bool HasClaimedDaily;
            public double CornMultiplier;
            public DateTime CornMultiplierLastEdit;
        }

        private readonly IServiceProvider _services;
        private SqliteConnection? _connection;

        public GuildTrackerSerializer(IServiceProvider services)
        {
            _services = services;
        }

        public void Initialize(string filename)
        {
            // insecure but all strings are statically supplied
            _connection = new SqliteConnection($"Data Source={filename}");
            _connection.Open();
        }

        public void Close()
        {
            _connection!.Close();
        }

        public async Task<Dictionary<ulong, GuildInfo>> Load(GuildTracker tracker)
        {
            await CreateTablesIfNotExist();

            Dictionary<ulong, GuildInfo> guilds = new();

            using (var gCommand = _connection!.CreateCommand())
            {
                gCommand.CommandText = @"SELECT id FROM guilds";
                var guildIterator = await gCommand.ExecuteReaderAsync();

                while (await guildIterator.ReadAsync())
                {
                    var guildId = (ulong)guildIterator.GetInt64(0);

                    GuildInfo guild = new(tracker, guildId, _services);

                    using (var uCommand = _connection!.CreateCommand())
                    {
                        uCommand.CommandText = @"SELECT * FROM users WHERE guild = @guildId;";
                        uCommand.Parameters.AddWithValue("@guildId", guildId);

                        var userIterator = await uCommand.ExecuteReaderAsync();

                        while (await userIterator.ReadAsync())
                        {
                            var userId = (ulong)userIterator.GetInt64(0);
                            UserInfo user = new(
                                guild: guild,
                                userId: userId,
                                cornCount: userIterator.GetInt64(2),
                                hasClaimedDaily: userIterator.GetInt32(3) != 0,
                                cornMultiplier: userIterator.GetDouble(4),
                                cornMultiplierLastEdit: DateTime.FromBinary(userIterator.GetInt64(5)),
                                _services
                            );
                            guild.AddUserInfo(user);
                        }
                    }

                    guilds.Add(guild.GuildId, guild);
                }
            }

            return guilds;
        }

        public async Task<bool> UserExists(UserInfo user)
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM users WHERE id = @userId AND guild = @guildId";
                command.Parameters.AddRange(new SqliteParameter[] {
                    new("@userId", user.UserId),
                    new("@guildId", user.Guild.GuildId),
                });
                await using (var userIterator = await command.ExecuteReaderAsync())
                {
                    if (await userIterator.ReadAsync()) return true;
                }
            }
            return false;
        }

        private async Task AddUserRaw(RawUserInfo userInfo)
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO users(id, guild, corn, daily, corn_multiplier, corn_multiplier_last_edit)
                    VALUES(@userId, @guildId, @cornCount, @daily, @cornMultiplier, @cornMultiplierLastEdit )";
                command.Parameters.AddRange(new SqliteParameter[] {
                    new("@userId", userInfo.UserId),
                    new("@guildId", userInfo.GuildId),
                    new("@cornCount", userInfo.CornCount),
                    new("@daily", userInfo.HasClaimedDaily ? 1 : 0),
                    new("@cornMultiplier", userInfo.CornMultiplier),
                    new("@cornMultiplierLastEdit", userInfo.CornMultiplierLastEdit.ToBinary()),
                });
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task AddUser(UserInfo user)
        {
            await AddUserRaw(new() {
                UserId = user.UserId,
                GuildId = user.Guild.GuildId,
                CornCount = user.CornCount,
                HasClaimedDaily = user.HasClaimedDaily,
                CornMultiplier = user.CornMultiplier,
                CornMultiplierLastEdit = user.CornMultiplierLastEdit,
            });
        }

        public async Task UpdateUser(UserInfo user)
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE users
                    SET corn = @cornCount,
                        daily = @daily,
                        corn_multiplier = @cornMultiplier,
                        corn_multiplier_last_edit = @cornMultiplierLastEdit
                    WHERE id = @userId AND guild = @guildId";
                command.Parameters.AddRange(new SqliteParameter[] {
                    new("@cornCount", user.CornCount),
                    new("@daily", user.HasClaimedDaily ? 1 : 0),
                    new("@cornMultiplier", user.CornMultiplier),
                    new("@cornMultiplierLastEdit", user.CornMultiplierLastEdit.ToBinary()),
                    new("@userId", user.UserId),
                    new("@guildId", user.Guild.GuildId),
                });
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task AddOrUpdateUser(UserInfo user)
        {
            if (await UserExists(user))
                await UpdateUser(user);
            else
                await AddUser(user);
        }

        public async Task ResetAllDailies()
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE users
                    SET daily = @daily";
                command.Parameters.AddWithValue("@daily", 0);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> GuildExists(GuildInfo guild)
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM guilds WHERE id = @guildId";
                command.Parameters.AddWithValue("@guildId", guild.GuildId);
                await using (var guildIterator = await command.ExecuteReaderAsync())
                {
                    if (await guildIterator.ReadAsync()) return true;
                }
            }
            return false;
        }

        public async Task AddGuild(GuildInfo guild)
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO guilds(id)
                    VALUES(@guildId)";
                command.Parameters.AddRange(new SqliteParameter[] {
                    new("@guildId", guild.GuildId),
                });
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateGuild(GuildInfo guild)
        {
            // there are currently no values in a guild that can be updated
            return;
        }

        public async Task AddOrUpdateGuild(GuildInfo guild)
        {
            if (await GuildExists(guild))
                await UpdateGuild(guild);
            else
                await AddGuild(guild);
        }

        public async Task LogAction(UserInfo user, UserHistory.ActionType type, long value, DateTimeOffset timestamp)
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO history(user, type, value, timestamp)
                    VALUES(@userId, @type, @value, @timestamp)";
                command.Parameters.AddRange(new SqliteParameter[] {
                    new("@userId", user.UserId),
                    new("@type", (int) type),
                    new("@value", value),
                    new("@timestamp", timestamp.ToString("o"))
                });
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<UserHistory> GetHistory(UserInfo user)
        {
            UserHistory history = new(user);

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM history WHERE user = @userId;";
                command.Parameters.AddWithValue("@userId", user.UserId);

                var historyIterator = await command.ExecuteReaderAsync();

                while (await historyIterator.ReadAsync())
                {
                    history.AddAction(new UserHistory.HistoryEntry{
                        Id = (ulong) historyIterator.GetInt64(0),
                        UserId = (ulong) historyIterator.GetInt64(1),
                        Type = (UserHistory.ActionType) historyIterator.GetInt32(2),
                        Value = historyIterator.GetInt64(3),
                        Timestamp = DateTimeOffset.Parse(historyIterator.GetString(4))
                    });
                }
            }

            return history;
        }

        private async Task CreateTablesIfNotExist()
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS users(
                        [id] INTEGER NOT NULL,
                        [guild] INTEGER NOT NULL,
                        [corn] INTEGER NOT NULL,
                        [daily] INTEGER NOT NULL,
                        [corn_multiplier] REAL NOT NULL,
                        [corn_multiplier_last_edit] INTEGER NOT NULL
                    )";
                await command.ExecuteNonQueryAsync();
            }

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS guilds(
                        [id] INTEGER NOT NULL PRIMARY KEY
                    )";
                await command.ExecuteNonQueryAsync();
            }

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS history(
                        [id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        [user] INTEGER NOT NULL,
                        [type] INTEGER NOT NULL,
                        [value] INTEGER NOT NULL,
                        [timestamp] TEXT NOT NULL
                    )";
                await command.ExecuteNonQueryAsync();
            }
        }

        /*
         * This method is a temporary tool to copy user data from a single guild onto every other guild
         * they are currently in. The reason it was added is as a solution to a bug in which user data
         * was stored globally instead of on a per-guild basis, so it should not be run in usual circumstances.
         */
        public async Task CopyUserData(DiscordSocketClient client)
        {
            await CreateTablesIfNotExist();

            Dictionary<ulong, List<ulong>> guildMembers = new();

            foreach (var guild in client.Guilds)
            {
                List<ulong> userIdList = new();
                await foreach (var userRequest in guild.GetUsersAsync())
                {
                    foreach (var user in userRequest)
                    {
                        userIdList.Add(user.Id);
                    }
                }
                guildMembers.Add(guild.Id, userIdList);
            }

            List<RawUserInfo> newUsers = new();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM users;";

                var userIterator = await command.ExecuteReaderAsync();

                while (await userIterator.ReadAsync())
                {
                    var userId = (ulong)userIterator.GetInt64(0);
                    var guildId = (ulong)userIterator.GetInt64(1);
                    var cornCount = userIterator.GetInt64(2);
                    var hasClaimedDaily = userIterator.GetInt32(3) != 0;
                    var cornMultiplier = userIterator.GetDouble(4);
                    var cornMultiplierLastEdit = DateTime.FromBinary(userIterator.GetInt64(5));

                    foreach (var entry in guildMembers)
                    {
                        if (entry.Key != guildId && entry.Value.Contains(userId))
                        {
                            newUsers.Add(new RawUserInfo()
                            {
                                UserId = userId,
                                GuildId = entry.Key,
                                CornCount = cornCount,
                                HasClaimedDaily = hasClaimedDaily,
                                CornMultiplier = cornMultiplier,
                                CornMultiplierLastEdit = cornMultiplierLastEdit,
                            });
                        }
                    }
                }
            }

            foreach (var newUser in newUsers)
            {
                await AddUserRaw(newUser);
            }

            await _connection.CloseAsync();
        }
    }
}
