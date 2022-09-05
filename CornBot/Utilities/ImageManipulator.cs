using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Utilities
{
    public class ImageManipulator
    {

        private IServiceProvider _services;
        private FontCollection _fontCollection;
        public Font? CurrentFont { get; set; }
        public List<FontFamily> FallbackFonts { get; private set; }

        public ImageManipulator(IServiceProvider services)
        {
            _fontCollection = new FontCollection();
            CurrentFont = null;
            FallbackFonts = new List<FontFamily>();
            _services = services;
        }

        /*
         * I would love to fully take advantage of method chaining since C# is an
         * object-oriented language. Unfortunately, the fact that some of the calls
         * are async makes this hard. There seem to be some solutions, so this will
         * likely be something I will come back to.
         */
        public Image? AddTopText(Image image, string text)
        {
            if (CurrentFont is null) return null;

            TextOptions options = new(CurrentFont)
            {

                Origin = new Point(20, 20),
                WrappingLength = image.Width - 40,
                HorizontalAlignment = HorizontalAlignment.Left,
                LineSpacing = 1.1f,
                FallbackFontFamilies = FallbackFonts,
            };

            // get text size
            var textRect = TextMeasurer.Measure(text, options);

            // add white top
            Image<Rgba32> finalImage = new(
                image.Width,
                image.Height + 40 + (int)textRect.Height);

            finalImage.Mutate(x => x.Clear(Color.White));
            finalImage.Mutate(x => x.DrawImage(image, new Point(0, 40 + (int)textRect.Height), 1.0f));

            // render text
            finalImage.Mutate(x => x.DrawText(options, text, Brushes.Solid(Color.Black)));

            return finalImage;
        }

        public void TestAllFallback()
        {
            if (CurrentFont is null) return;

            var client = _services.GetRequiredService<CornClient>();
            List<FontFamily> broken = new();

            foreach (var fallbackFont in FallbackFonts)
            {
                TextOptions options = new(CurrentFont)
                {
                    Origin = new Point(20, 20),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    LineSpacing = 1.1f,
                    FallbackFontFamilies = new[] {fallbackFont},
                };

                try
                {
                    TextMeasurer.Measure("test", options);
                }
                catch (EndOfStreamException)
                {
                    client.Log(Discord.LogSeverity.Warning,
                        "FontTest", $"Removing broken font: {fallbackFont.Name}...");
                    broken.Add(fallbackFont);
                }
            }

            foreach (var brokenFont in broken)
                FallbackFonts.Remove(brokenFont);
        }

        public void LoadFont(string fileName, float size, FontStyle style)
        {
            try
            {
                FontFamily? fontFamily = _fontCollection.Add(fileName);
                CurrentFont = fontFamily?.CreateFont(size, style);
            }
            catch (FileNotFoundException) { };
            if (CurrentFont is null)
                _services.GetRequiredService<CornClient>().Log(Discord.LogSeverity.Error,
                    "LoadFont", $"Failed to load font at {fileName}...");
        }

        public void AddFallbackFontFamily(string fileName)
        {
            FontFamily? fontFamily = _fontCollection.Add(fileName);
            if (fontFamily is not null)
                FallbackFonts.Add((FontFamily)fontFamily);
            else
                _services.GetRequiredService<CornClient>().Log(Discord.LogSeverity.Error,
                    "LoadFont", $"Failed to load fallback font at {fileName}...");
        }

        public async Task Save(Image image, string fileName)
        {
            using FileStream output = new(fileName, FileMode.Create, FileAccess.Write);
            await image.SaveAsPngAsync(output);
        }

        public async Task<MemoryStream> GetBytes(Image image)
        {
            MemoryStream s = new();
            await image.SaveAsPngAsync(s);
            return s;
        }

    }
}
