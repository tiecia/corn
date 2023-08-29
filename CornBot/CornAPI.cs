using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CornBot.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace CornBot {
    public class CornAPI {
        private readonly IServiceProvider _services;
        private string ADMIN_SECRET = "";

        public CornAPI(IServiceProvider services) {
            _services = services;
        }

        public async Task RunAsync() {
            var client = new SecretClient(new Uri(CornClient.Configuration["KeyVaultUri"]), new DefaultAzureCredential());
            ADMIN_SECRET = client.GetSecret(CornClient.Configuration["KeyName"]).Value.Value;

            if(ADMIN_SECRET == "" || ADMIN_SECRET == null)
            {
                throw new Exception("Failed to get Admin secret fropm vault");
            }

            var builder = WebApplication.CreateBuilder();

            // Add services to the container.
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.MapGet("/", (HttpContext context) => {
                return "CORN!";
            });

            // Gets either global or guild specific info for a user.
            // GET http://{baseurl}/corncount?user={username}
            // GET http://{baseurl}/shuckstatus?user={username}&guild={guildid}
            /* Example Response:
            {
                "Username": "tiec",
                "ShuckStatus": false,
                "CornCount": 15
            }
             */
            app.MapGet("/shuckerinfo", (HttpContext context) =>
            {
                string? queryUser = context.Request.Query["user"];
                string? queryGuild = context.Request.Query["guild"];

                context.Response.ContentType = "application/json";

                if(queryUser == null)
                {
                    context.Response.StatusCode = 400;
                    return JsonConvert.SerializeObject(new ErrorResponse()
                    {
                        Error = "No user provided in query string."
                    });
                }

                return JsonConvert.SerializeObject(new ShuckerInfo()
                {
                    Username = queryUser,
                    CornCount = GetCornCount(queryUser, queryGuild),
                    ShuckStatus = GetShuckStatus(queryUser, queryGuild)
                });
            });

            app.MapGet("/leaderboards", async (HttpContext context) => {
                string? count = context.Request.Query["count"];
                string? queryGuild = context.Request.Query["guild"];

                context.Response.ContentType = "application/json";

                List<ShuckerLeaderboardEntry> leaderboard = await GetShuckerLeaderboardAsync(queryGuild, count == null ? 10 : int.Parse(count));

                return JsonConvert.SerializeObject(leaderboard);
            });

            //app.MapGet("/debugEconomy", (HttpContext context) =>
            //{
            //    context.Response.ContentType = "application/json";
            //    var econonmy = _services.GetRequiredService<GuildTracker>();
            //    return JsonConvert.SerializeObject(econonmy, new JsonSerializerSettings()
            //    {
            //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    });
            //});

            await app.RunAsync();
        }

        private async Task<List<ShuckerLeaderboardEntry>> GetShuckerLeaderboardAsync(string? queryGuild, int count) {
            if(queryGuild == null) {
                var userLb = new SortedSet<UserInfo>();
                foreach(var guild in _services.GetRequiredService<GuildTracker>().Guilds.Values) {
                    var disLb = await guild.GetLeaderboards(count);
                    foreach (var user in disLb) {
                        bool added = false;
                        var info = guild.GetUserInfo(user);
                        for (int i = 0; i<userLb.Count; i++) {
                            var lbEntry = userLb.ElementAt(i);
                            if (user.Username == lbEntry.Username) {
                                lbEntry.CornCount += info.CornCount;
                                lbEntry.HasClaimedDaily = lbEntry.HasClaimedDaily && info.HasClaimedDaily;
                                added = true;
                            }
                        }
                        if(!added) {
                            userLb.Add((UserInfo)info.Clone());
                        }
                    }
                }

                int pos = 0;
                var lb = new List<ShuckerLeaderboardEntry>();
                foreach(var user in userLb.Reverse()) {
                    lb.Add(new ShuckerLeaderboardEntry() {
                        Username = user.Username,
                        CornCount = user.CornCount,
                        ShuckStatus = user.HasClaimedDaily,
                        LeaderboardPosition = pos++
                    });
                }
                return lb;
            } else {
                var guild = GetGuild(ulong.Parse(queryGuild));
                return await GetLeaderboardForGuildAsync(guild, count);
            }
        }

        private async Task<List<ShuckerLeaderboardEntry>> GetLeaderboardForGuildAsync(GuildInfo? guild, int count) {
            
            var lb = new List<ShuckerLeaderboardEntry>();
            if (guild == null) {
                return lb;
            }
            int pos = 0;
            var disLb = await guild.GetLeaderboards(count);
            foreach (var user in disLb) {
                var info = guild.GetUserInfo(user);
                lb.Add(new ShuckerLeaderboardEntry() {
                    Username = user.Username,
                    CornCount = info.CornCount,
                    ShuckStatus = info.HasClaimedDaily,
                    LeaderboardPosition = pos++
                });
            }
            return lb;
        }

        private GuildInfo? GetGuild(ulong guildId) {
            var economy = _services.GetRequiredService<GuildTracker>();
            foreach(var guild in economy.Guilds.Values) {
                if (guild.GuildId == guildId) {
                    return guild;
                }
            }
            return null;
        }

        private bool GetShuckStatus(string queryUser, string? queryGuild)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            int dailyCount = 0;
            int serverCount = 0;
            foreach (var guild in economy.Guilds.Values)
            {
                foreach (var user in guild.Users.Values)
                {
                    if(user.Username == queryUser)
                    {
                        if (!user.HasClaimedDaily)
                        {
                            return false;
                        }
                        else
                        {
                            if(queryGuild != null && guild.GuildId == ulong.Parse(queryGuild))
                            {
                                return true;
                            }
                            dailyCount++;
                        }
                        serverCount++;
                    }
                }
            }

            if (dailyCount > 0 && dailyCount == serverCount && queryGuild == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private long GetCornCount(string queryUser, string? queryGuild)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            long cornCount = 0;
            if (queryGuild == null)
            {
                foreach (var guild in economy.Guilds.Values)
                {
                    foreach (var user in guild.Users.Values)
                    {
                        if (user.Username == queryUser)
                        {
                            cornCount += user.CornCount;
                        }
                    }
                }
            }
            else
            {

                var guild = GetGuild(ulong.Parse(queryGuild));
                foreach (var user in guild.Users.Values)
                {
                    if (user.Username == queryUser)
                    {
                        return user.CornCount;
                    }
                }
            }
            return cornCount;
        }
        private class ShuckerInfo
        {
            public string Username { get; set; } = "";
            public bool ShuckStatus { get; set; }
            public long CornCount { get; set; }

        }
        private class ShuckerLeaderboardEntry : ShuckerInfo 
        {
            public int LeaderboardPosition { get; set; }
        }

        private class ErrorResponse
        {
            public string Error { get; set; } = "";
        }
    }
}
