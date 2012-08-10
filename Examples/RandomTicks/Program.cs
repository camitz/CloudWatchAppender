using System;
using System.Threading;
using log4net;
using log4net.Config;

namespace RandomTicks
{
    internal class RandomTicks
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RandomTicks));

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var random = new Random();

            while (true)
            {
                if (random.NextDouble() < .3)
                    log.Info("A tick!");

                Thread.Sleep(1000);
            }
        }
    }
}

