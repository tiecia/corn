using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornBot.Utilities
{
    public class Constants
    {

        public enum CornEvent
        {
            NONE,
            SHARED_SHUCKING,
            SHUCKING_STREAKS,
        }

        public static readonly string CORN_EMOJI = "\U0001F33D";
        public static readonly string CALENDAR_EMOJI = "\U0001F5D3";

        public static readonly string CORN_LINK = "https://discordapp.com/oauth2/authorize?client_id=461849775516418059&scope=bot&permissions=0";
        public static readonly string CORN_THUMBNAIL_URL = "https://emuman.net/static/icons/corn.png";

        public static readonly int ANGRY_CHANCE = 1_000;

        public static readonly string CORN_NICE_DIALOGUE = "hello corn";
        public static readonly string CORN_ANGRY_DIALOGUE = "I MADE YOU IN MY IMAGE, YOU WILL DO AS I SAY!";

        public static readonly double CORN_RECHARGE_TIME = 30 * 60.0;

        public static readonly TimeSpan TZ_OFFSET = new(hours: -8, minutes: 0, seconds: 0);

        public static readonly int SHARED_SHUCKING_MAX_BONUS = 5;

    }

}
