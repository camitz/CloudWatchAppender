using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace CloudWatchAppender.Services
{
    public static class ServiceTasks
    {
        public static  ConcurrentDictionary<int, Task> Tasks;

        public static bool HasPendingRequests
        {
            get { return Tasks!=null && Tasks.Values.Any(t => !t.IsCompleted); }
        }

        public static void WaitForPendingRequests(TimeSpan timeout)
        {
            var startedTime = DateTime.UtcNow;
            var timeConsumed = TimeSpan.Zero;
            while (HasPendingRequests && timeConsumed < timeout)
            {
                Task.WaitAll(Tasks.Values.ToArray(), timeout - timeConsumed);
                timeConsumed = DateTime.UtcNow - startedTime;
            }
        }

        public static void WaitForPendingRequests()
        {
            while (HasPendingRequests)
                Task.WaitAll(Tasks.Values.ToArray());
        }
    }
}