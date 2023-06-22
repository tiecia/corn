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
            NORMAL_DISTRIBUTION_SHUCKING,
            PRIDE,
        }

        public static readonly string CORN_EMOJI = "\U0001F33D";
        public static readonly string CALENDAR_EMOJI = "\U0001F5D3";
        public static readonly string RAINBOW_EMOJI = "\U0001F308";
        public static readonly string PRIDE_CORN_EMOJI = "<:pridecorn:1113715834439880725>";
        public static readonly string POPCORN_EMOJI = "\U0001F37F";
        public static readonly string UNICORN_EMOJI = "\U0001F984";
        public static readonly string LARGE_BLACK_SQUARE_EMOJI = "\U00002B1B";

        public static readonly string CORN_LINK = "https://discordapp.com/oauth2/authorize?client_id=461849775516418059&scope=bot&permissions=0";
        public static readonly string CORN_THUMBNAIL_URL = "https://emuman.net/static/icons/corn.png";

        public static readonly int ANGRY_CHANCE = 1_000;
        public static readonly double CORN_DAILY_MEAN = 50.0;
        public static readonly double CORN_DAILY_STD_DEV = 15.0;


        public static readonly string CORN_NICE_DIALOGUE = "hello corn";
        public static readonly string CORN_ANGRY_DIALOGUE = "I MADE YOU IN MY IMAGE, YOU WILL DO AS I SAY!";
        public static readonly string CORN_PRIDE_DIALOGUE = "happy pride!";
        public static readonly string CORN_PRIDE_DIALOGUE_COMBINED = "hello corn, happy pride!";

        public static readonly double CORN_RECHARGE_TIME = 30 * 60.0;

        public static readonly TimeSpan TZ_OFFSET = new(hours: -8, minutes: 0, seconds: 0);

        public static readonly int SHARED_SHUCKING_MAX_BONUS = 5;

    }

}
