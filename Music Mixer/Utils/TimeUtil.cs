using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music_Mixer
{
    internal enum Time {
        Dawn,
        Day,
        Dusk,
        Night
    }
    internal static class TimeUtil
    {
        public static Time GetDayCycle() {

            var currentTime = DateTime.Now.ToUniversalTime();

            var percentOfTwoHours = Math.Abs((((float)currentTime.Hour % 2) + ((float)currentTime.Minute / 60)) * 50 / 100);
            
            if (percentOfTwoHours < 0.041666666666667) // 00:05
                return Time.Dawn;
            else if (percentOfTwoHours < 0.625) // +00:70 (00:75)
                return Time.Day;
            else if (percentOfTwoHours < 0.66666666667) // +00:05 (00:80)
                return Time.Dusk;
            else
                return Time.Night;
        }
    }
}