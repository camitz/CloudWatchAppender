using System;
using System.Linq;

namespace CloudWatchAppender
{
    public class EventRateLimiter
    {
        private readonly int _maxEventsPerSecond;
        private double _tokens;
        private DateTime _timeBefore;

        public EventRateLimiter(int maxEventsPerSecond)
        {
            _maxEventsPerSecond = maxEventsPerSecond;
            _tokens = maxEventsPerSecond;
        }

        public EventRateLimiter()
        {
        }

        public bool Request(DateTime timeStamp)
        {
            var timePassed = timeStamp - _timeBefore;
            _timeBefore = timeStamp;

            _tokens += 1.0 * _maxEventsPerSecond * timePassed.Ticks / TimeSpan.FromSeconds(1).Ticks;

            if (_tokens > _maxEventsPerSecond)
                _tokens = _maxEventsPerSecond;

            if (_tokens >= 1)
            {
                _tokens--;
                return true;
            }

            return false;
        }
    }
}