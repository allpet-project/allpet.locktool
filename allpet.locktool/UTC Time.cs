using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace allpet.locktool
{
    class UTC_Time
    {
        public UTC_Time(int year,int month,int day,int hour)
        {
            time = (new DateTime(year, month, day, hour, 0, 0)).ToLocalTime();
        }
        public DateTime time;
        public DateTime Time_UTC
        {
            get
            {
                return time.ToUniversalTime();
            }
        }
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public uint ToTimestamp()
        {
            return (uint)(time.ToUniversalTime() - unixEpoch).TotalSeconds;
        }
        public override string ToString()
        {
            return "utc:" + time.ToUniversalTime().ToLongDateString() + " - " + time.ToUniversalTime().ToLongTimeString();
        }

    }
}
