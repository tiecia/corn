using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace CornBot.Utilities
{
    public class ImageStore : Dictionary<string, Image>
    {

        public async Task LoadImages()
        {
            this["cool_corn"] = await Image.LoadAsync("Assets/cool_corn.png");
            this["sexy_corn"] = await Image.LoadAsync("Assets/sexy_corn.png");
            this["cool_bean"] = await Image.LoadAsync("Assets/cool_bean.png");
            this["sexy_bean"] = await Image.LoadAsync("Assets/sexy_bean.png");
        }

    }
}
