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

            app.MapGet("/shuckstatus", (HttpContext context) =>
            {
                string? queryUser = context.Request.Query["user"];
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

            await app.RunAsync();
        }
    }
}
