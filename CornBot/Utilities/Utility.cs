using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornBot.Utilities
{
    public class Utility
    {

        public static DateTimeOffset GetAdjustedTimestamp()
        {
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            return new(now + Constants.TZ_OFFSET, Constants.TZ_OFFSET);
        }

        public static Constants.CornEvent GetCurrentEvent()
        {
            return GetAdjustedTimestamp().Month switch
            {
                1 => Constants.CornEvent.SHARED_SHUCKING,
                2 => Constants.CornEvent.SHUCKING_STREAKS,
                _ => Constants.CornEvent.NONE,
            };
        }

    }
}
