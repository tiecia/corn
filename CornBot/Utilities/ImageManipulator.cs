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

namespace CornBot.Utilities
{
    public class ImageManipulator
    {

        private FontCollection _fontCollection;
        public FontFamily? CurrentFontFamily { get; set; }
        public Font? CurrentFont { get; set; }

        public ImageManipulator()
        {
            _fontCollection = new FontCollection();
            CurrentFontFamily = null;
            CurrentFont = null;
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

        public void LoadFont(string fileName, float size, FontStyle style)
        {
            CurrentFontFamily = _fontCollection.Add(fileName);
            CurrentFont = CurrentFontFamily?.CreateFont(size, style);
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
