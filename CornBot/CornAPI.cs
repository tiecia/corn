using CornBot.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace CornBot {
    public class CornAPI {
        private readonly IServiceProvider _services;

        public CornAPI(IServiceProvider services) {
            _services = services;
        }

        public async Task RunAsync() {
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

                return JsonConvert.SerializeObject(new ShuckerInfoResponse()
                {
                    Username = queryUser,
                    CornCount = GetCornCount(queryUser, queryGuild),
                    ShuckStatus = GetShuckStatus(queryUser, queryGuild)
                });
            });

            await app.RunAsync();
        }

        private bool GetShuckStatus(string queryUser, string? queryGuild)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            int dailyCount = 0;
            foreach (var guild in economy.Guilds.Values)
            {
                foreach (var user in guild.Users.Values)
                {
                    if (user.Username == queryUser && !user.HasClaimedDaily)
                    {
                        return false;
                    }
                    else if (user.Username == queryUser && user.HasClaimedDaily)
                    {
                        if(queryGuild != null && guild.GuildId == ulong.Parse(queryGuild))
                        {
                            return true;
                        }
                        dailyCount++;
                    }
                }
            }

            if (dailyCount > 0 && dailyCount == economy.Guilds.Count && queryGuild == null)
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
                foreach (var guild in economy.Guilds.Values)
                {
                    if (guild.GuildId == ulong.Parse(queryGuild))
                    {
                        foreach (var user in guild.Users.Values)
                        {
                            if (user.Username == queryUser)
                            {
                                return user.CornCount;
                            }
                        }
                    }
                }
            }
            return cornCount;
        }
        private class ShuckerInfoResponse
        {
            public string Username { get; set; } = "";
            public bool ShuckStatus { get; set; }
            public long CornCount { get; set; }
        }

        private class ErrorResponse
        {
            public string Error { get; set; } = "";
        }
    }
}
