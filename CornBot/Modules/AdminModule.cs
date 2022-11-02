using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using CornBot.Models;

namespace CornBot.Modules
{

    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {

        public InteractionService? Commands { get; set; }
        private readonly IServiceProvider _services;

        public AdminModule(IServiceProvider services)
        {
            _services = services;
            _services.GetRequiredService<CornClient>().Log(
                LogSeverity.Debug, "Modules", "Creating AdminModule...");
        }

        [RequireOwner]
        [SlashCommand("add", "Adds to a user's corn")]
        public async Task AddCorn(
            [Summary(description: "user to add corn to")] IUser user,
            [Summary(description: "amount of corn to add")] int amount)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            var userInfo = economy.LookupGuild(Context.Guild).GetUserInfo(user);
            userInfo.CornCount += amount;
            await RespondAsync($"{amount} corn has been added to {user}'s balance for a total of {userInfo.CornCount} corn");
        }

        [RequireOwner]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("set", "Sets a user's corn")]
        public async Task SetCorn(
            [Summary(description: "user to set corn of")] IUser user,
            [Summary(description: "amount of corn to set")] int amount)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            var userInfo = economy.LookupGuild(Context.Guild).GetUserInfo(user);
            userInfo.CornCount = amount;
            await RespondAsync($"{user}'s corn has been set to {userInfo.CornCount}");
        }

        [RequireOwner]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("reset-daily", "Forces a daily reset")]
        public async Task ResetDaily([Summary(description: "optional specific user to reset")] IUser? user = null)
        {
            var economy = _services.GetRequiredService<GuildTracker>();
            if (user == null)
            {
                economy.ResetDailies();
                await RespondAsync($"all dailies have been reset!");
            }
            else
            {
                economy.LookupGuild(Context.Guild).GetUserInfo(user).HasClaimedDaily = false;
                await RespondAsync($"{user}'s daily has been reset!");
            }
        }

    }

}
