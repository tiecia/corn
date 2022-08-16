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
    [EnabledInDm(true)]
    public class ImageCaptionModule : InteractionModuleBase<SocketInteractionContext>
    {
        
        public InteractionService? Commands { get; set; }
        private readonly IServiceProvider _services;

        public ImageCaptionModule(IServiceProvider services)
        {
            _services = services;
        }

        [SlashCommand("cool-corn", "Creates a cool corn with your caption")]
        public async Task CoolCorn([Summary(description: "what cool corn will say")] string text)
        {
            var coolCorn = _services.GetRequiredService<ImageStore>()["cool_corn"];
            var manipulator = _services.GetRequiredService<ImageManipulator>();
            var newImage = manipulator.AddTopText(coolCorn, text);
            if (newImage is null)
            {
                await RespondAsync("something brokey :( contact EmuMan#2495");
                return;
            }
            var bytes = await manipulator.GetBytes(newImage);
            await RespondWithFileAsync(bytes, "cool_corn.png");
        }

        [SlashCommand("sexy-corn", "Creates a sexy corn with your caption")]
        public async Task SexyCorn([Summary(description: "what sexy corn will say")] string text)
        {
            var sexyCorn = _services.GetRequiredService<ImageStore>()["sexy_corn"];
            var manipulator = _services.GetRequiredService<ImageManipulator>();
            var newImage = manipulator.AddTopText(sexyCorn, text);
            if (newImage is null)
            {
                await RespondAsync("something brokey :( contact EmuMan#2495");
                return;
            }
            var bytes = await manipulator.GetBytes(newImage);
            await RespondWithFileAsync(bytes, "sexy_corn.png");
        }

    }
}
