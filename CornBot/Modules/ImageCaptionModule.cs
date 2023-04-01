using CornBot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Modules
{
    public class ImageCaptionModule : InteractionModuleBase<SocketInteractionContext>
    {
        
        public InteractionService? Commands { get; set; }
        private readonly IServiceProvider _services;

        public ImageCaptionModule(IServiceProvider services)
        {
            _services = services;
            _services.GetRequiredService<CornClient>().Log(
                Discord.LogSeverity.Debug, "Modules", "Creating ImageCaptionModule...");
        }

        [SlashCommand("cool-bean", "Creates a cool bean with your caption")]
        public async Task CoolCorn([Summary(description: "what cool bean will say")] string text)
        {
            var coolCorn = _services.GetRequiredService<ImageStore>()["cool_bean"];
            var manipulator = _services.GetRequiredService<ImageManipulator>();
            var newImage = manipulator.AddTopText(coolCorn, text);
            if (newImage is null)
            {
                await RespondAsync("something brokey :( contact EmuMan#2495");
                return;
            }
            var bytes = await manipulator.GetBytes(newImage);
            await RespondWithFileAsync(bytes, "cool_bean.png");
        }

        [SlashCommand("sexy-bean", "Creates a sexy bean with your caption")]
        public async Task SexyCorn([Summary(description: "what sexy bean will say")] string text)
        {
            var sexyCorn = _services.GetRequiredService<ImageStore>()["sexy_bean"];
            var manipulator = _services.GetRequiredService<ImageManipulator>();
            var newImage = manipulator.AddTopText(sexyCorn, text);
            if (newImage is null)
            {
                await RespondAsync("something brokey :( contact EmuMan#2495");
                return;
            }
            var bytes = await manipulator.GetBytes(newImage);
            await RespondWithFileAsync(bytes, "sexy_bean.png");
        }

    }
}
