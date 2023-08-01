using CornBot.Models;
using Discord;
using Discord.Rest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



            // Gets if a user has done all their dailies
            // GET http://{baseurl}/shuckstatus?user={username}
            app.MapGet("/shuckstatus", (HttpContext context) =>
            {
                string? queryUser = context.Request.Query["user"];
                if(queryUser == null)
                {
                    context.Response.StatusCode = 400;
                    return false;
                }
                var economy = _services.GetRequiredService<GuildTracker>();
                int dailyCount = 0;
                foreach (var guild in economy.Guilds.Values)
                {
                    foreach (var user in guild.Users.Values)
                    {
                        if(user.Username == queryUser && !user.HasClaimedDaily)
                        {
                            return false;
                        } else if(user.Username == queryUser && user.HasClaimedDaily)
                        {
                            dailyCount++;
                        }
                    }
                }

                if(dailyCount == economy.Guilds.Count()) {
                    return true;
                } else
                {
                    return false;
                }
            });

            // Gets the users total corn in a guild, or all guilds if no guild is given
            // GET http://{baseurl}/corncount?user={username}
            // GET http://{baseurl}/shuckstatus?user={username}&guild={guildid}
            app.MapGet("/corncount", (HttpContext context) =>
            {
                string? queryUser = context.Request.Query["user"];
                string? queryGuild = context.Request.Query["guild"];

                if(queryUser == null)
                {
                    context.Response.StatusCode = 400;
                    return 0;
                }

                var economy = _services.GetRequiredService<GuildTracker>();
                long cornCount = 0;
                if(queryGuild == null)
                {
                    foreach (var guild in economy.Guilds.Values)
                    {
                        foreach (var user in guild.Users.Values)
                        {
                            if(user.Username == queryUser)
                            {
                                cornCount += user.CornCount;
                            }
                        }
                    }
                } else
                {
                    foreach (var guild in economy.Guilds.Values)
                    {
                        if(guild.GuildId == ulong.Parse(queryGuild))
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
            });

            await app.RunAsync();
        }
    }
}
