using CornBot.Utilities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CornBot.Models;

namespace CornBot.Handlers
{
    public class MessageHandler
    {

        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;

        private readonly WordDetector _cornDetector;

        public MessageHandler(DiscordSocketClient client, IServiceProvider services)
        {
            _client = client;
            _services = services;
            _cornDetector = new WordDetector("corn", "\U0001F33D");
        }

        public Task Initialize()
        {
            _client.MessageReceived += MessageReceivedAsync;
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            if (message.Author.IsBot) return;

            var channel = message.Channel as SocketGuildChannel;
            if (channel == null) return;

            var content = message.Content;
            var userInfo = _services.GetRequiredService<GuildTracker>().LookupGuild(channel.Guild).GetUserInfo(message.Author);

            WordDetector.DetectionLevel result = _cornDetector.Parse(content);

            // this is a little stupid but necessary i guess
            if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id) ||
                result == WordDetector.DetectionLevel.FULL)
            {
                if (_services.GetRequiredService<Random>().Next(0, Constants.ANGRY_CHANCE) == 0)
                {
                    await message.Channel.SendMessageAsync(Constants.CORN_ANGRY_DIALOGUE);
                    userInfo.CornCount -= 1000;
                }
                else
                {
                    await message.Channel.SendMessageAsync(Constants.CORN_NICE_DIALOGUE);
                    userInfo.AddCornWithPenalty(5);
                }
            }
            else if (result == WordDetector.DetectionLevel.PARTIAL)
            {
                await message.AddReactionAsync(new Emoji(Constants.CORN_EMOJI));
                userInfo.AddCornWithPenalty(1);
            }
        }

    }
}
