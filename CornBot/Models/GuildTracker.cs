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

        private readonly IServiceProvider _services;

        public Dictionary<ulong, GuildInfo> Guilds { get; private set; } = new();

        private JsonSerializerOptions _serializeOptions;
        private JsonSerializerOptions _deserializeOptions;

        public GuildTracker(IServiceProvider services)
        {
            _services = services;

            _serializeOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                Converters =
                {
                    new GuildTrackerJsonConverter(_services),
                    new GuildInfoJsonConverter(_services),
                    new UserInfoJsonConverter(_services)
                }
            };

            _deserializeOptions = new JsonSerializerOptions();
            _deserializeOptions.Converters.Add(new GuildTrackerJsonConverter(_services));
            _deserializeOptions.Converters.Add(new GuildInfoJsonConverter(_services));
            _deserializeOptions.Converters.Add(new UserInfoJsonConverter(_services));
        }

        public GuildTracker(Dictionary<ulong, GuildInfo> guilds, IServiceProvider services) : this(services)
        {
            Guilds = guilds;
        }

        public GuildInfo LookupGuild(SocketGuild guild)
        {
            if (!Guilds.ContainsKey(guild.Id))
                Guilds.Add(guild.Id, new(guild, _services));
            return Guilds[guild.Id];
        }

        public bool LoadFromFile(string fileName)
        {
            // read from file
            if (!File.Exists(fileName)) return false;
            string jsonString = File.ReadAllText(fileName);

            // add converters to deserialization options
            var deserializeOptions = new JsonSerializerOptions();
            deserializeOptions.Converters.Add(new GuildTrackerJsonConverter(_services));
            deserializeOptions.Converters.Add(new GuildInfoJsonConverter(_services));
            deserializeOptions.Converters.Add(new UserInfoJsonConverter(_services));

            // deserialize and check for failure
            GuildTracker? guildTracker = JsonSerializer.Deserialize<GuildTracker>(jsonString, deserializeOptions);
            if (guildTracker == null) return false;

            // set current values to new values (janky, but importantly, it works with _services)
            Guilds = guildTracker.Guilds;

            return true;
        }

        public async Task SaveToFile(string fileName)
        {
            using FileStream createStream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(createStream, this, _serializeOptions);
            await createStream.DisposeAsync();
        }

        public async Task StartSaveLoop()
        {
            while (true)
            {
                await SaveToFile("data.json");
                await Task.Delay(10 * 1000);
            }
        }

        public void ResetDailies()
        {
            foreach (var guild in Guilds.Values)
            {
                foreach (var user in guild.Users.Values)
                {
                    user.HasClaimedDaily = false;
                }
            }
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
                ResetDailies();
                await client.Log(new LogMessage(LogSeverity.Info, "DailyReset", "Daily reset performed successfully!"));
                nextReset = nextReset.AddDays(1);
            }
        }

    }
}
