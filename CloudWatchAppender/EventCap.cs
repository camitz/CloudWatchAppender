using System;
using System.Linq;

namespace CloudWatchAppender
{
    public class EventCap
    {
        private const int RES = 10; //resolution in ms

        private int _max;
        private readonly int[] _tsBuffer;
        private int p = 0;
        private DateTime? _lastEventTime;
        private int _length = 1000 / RES;

        public EventCap(int max)
        {
            _max = max;

            _tsBuffer = new int[_length];
        }

        public EventCap()
        {
        }

        public bool Request(DateTime timeStamp)
        {
            if (_max <= 0)
                return true;

            if (!_lastEventTime.HasValue)
            {
                _lastEventTime = timeStamp;
                _tsBuffer[0] = 1;
                return true;
            }

            //A
            //Mutually redundant with B
            if (timeStamp - _lastEventTime >= TimeSpan.FromSeconds(1))
            {
                _lastEventTime = timeStamp;
                Array.Clear(_tsBuffer, 0, _length);
                _tsBuffer[0] = 1;
                p = 0;
                return true;
            }

            var newP = (timeStamp - _lastEventTime.Value).Milliseconds / RES + p;

            if (newP < _length)
                Array.Clear(_tsBuffer, p + 1, newP - p);

            else if (newP > p + _length)
            {
                //B
                Array.Clear(_tsBuffer, 0, _length);
            }
            else
            {
                Array.Clear(_tsBuffer, p + 1, _length - p - 1);
                Array.Clear(_tsBuffer, 0, newP % _length);
            }

            p = newP % _length;
            _tsBuffer[p]++;
            _lastEventTime = timeStamp;

            var sum = _tsBuffer.Sum();

            return sum <= 10;
        }
    }
}