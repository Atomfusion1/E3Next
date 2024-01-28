using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public class ElapsedTimer
    {
        private Stopwatch _stopwatch;
        private long _targetTime;

        public ElapsedTimer()   {
            _stopwatch = new Stopwatch();
        }
        // SetTime(Hours, Minutes, Seconds, MilliSeconds)
        public void SetTime(int hours, int minutes, int seconds, int milliseconds)  {
            _targetTime = (hours * 3600000) + (minutes * 60000) + (seconds * 1000) + milliseconds;
            _stopwatch.Restart();
        }
        public bool IsElapsed() {
            return _stopwatch.ElapsedMilliseconds >= _targetTime;
        }

        public long TimeLeft() { 
            return _targetTime - _stopwatch.ElapsedMilliseconds;
        }
        public string TimeLeftString()  {
            long remaining = TimeLeft();
            if (remaining < 0) remaining = 0;
            int remainingHours = (int)(remaining / 3600000);
            remaining %= 3600000;
            int remainingMinutes = (int)(remaining / 60000);
            remaining %= 60000;
            int remainingSeconds = (int)(remaining / 1000);
            remaining %= 1000;
            int remainingMilliseconds = (int)remaining;
            return $"{remainingHours}h {remainingMinutes}m {remainingSeconds}s {remainingMilliseconds}ms";
        }
    }
}
