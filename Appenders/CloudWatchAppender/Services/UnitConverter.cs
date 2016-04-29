using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch;
using log4net.Util.TypeConverters;

namespace AWSAppender.CloudWatch.Services
{
    public class UnitConverter
    {
        private readonly double _value;
        private StandardUnit _from;
        private static Dictionary<StandardUnit, UnitGraphNode> _converterGraph;

        private UnitConverter(double value)
        {
            _value = value;
        }

        public static UnitConverter Convert(double value)
        {
            return new UnitConverter(value);
        }


        public UnitConverter From(StandardUnit standardUnit)
        {
            _from = standardUnit;
            return this;
        }

        public double To(StandardUnit to)
        {
            if (to == _from)
                return _value;

            return PerformConvert(to);
        }

        private double PerformConvert(StandardUnit to)
        {
            if (_converterGraph == null)
                GenerateGraph();

            var queue = new Queue<UnitGraphNode>();
            var visited = new HashSet<UnitGraphNode>();
            var multipliers = new Dictionary<UnitGraphNode, double>();

            var fromNode = _converterGraph[_from];
            queue.Enqueue(fromNode);
            multipliers.Add(fromNode, 1.0);

            while (queue.Any())
            {
                var n = queue.Dequeue();
                if (_converterGraph[to] == n)
                {
                    if (!fromNode.Links.Select(l => l.Value.OtherNode).Contains(n))
                        fromNode.LinkTo(n, multipliers[n]);
                    return multipliers[n] * _value;
                }

                if (!visited.Contains(n))
                {
                    visited.Add(n);
                    var multiplier = multipliers[n];
                    foreach (var link in n.Links.Values)
                    {
                        if (!visited.Contains(link.OtherNode))
                        {
                            queue.Enqueue(link.OtherNode);
                            if (!multipliers.ContainsKey(link.OtherNode))
                                multipliers.Add(link.OtherNode, multiplier * link.Multiplier);
                        }
                    }
                }
            }

            throw new ConversionNotSupportedException();
        }

        private static void GenerateGraph()
        {
            _converterGraph = new Dictionary<StandardUnit, UnitGraphNode>
                              {
                                  {StandardUnit.Seconds,UnitGraphNode.Seconds},
                                  {StandardUnit.Microseconds,UnitGraphNode.Microseconds},
                                  {StandardUnit.Milliseconds,UnitGraphNode.Milliseconds},
                                  {StandardUnit.Bytes,UnitGraphNode.Bytes},
                                  {StandardUnit.Kilobytes,UnitGraphNode.Kilobytes},
                                  {StandardUnit.Megabytes,UnitGraphNode.Megabytes},
                                  {StandardUnit.Gigabytes,UnitGraphNode.Gigabytes},
                                  {StandardUnit.Terabytes,UnitGraphNode.Terabytes},
                                  {StandardUnit.Bits,UnitGraphNode.Bits},
                                  {StandardUnit.Kilobits,UnitGraphNode.Kilobits},
                                  {StandardUnit.Megabits,UnitGraphNode.Megabits},
                                  {StandardUnit.Gigabits,UnitGraphNode.Gigabits},
                                  {StandardUnit.Terabits,UnitGraphNode.Terabits},
                                  {StandardUnit.Percent,UnitGraphNode.Percent},
                                  {StandardUnit.Count,UnitGraphNode.Count},
                                  {StandardUnit.BytesSecond,UnitGraphNode.BytesSecond},
                                  {StandardUnit.KilobytesSecond,UnitGraphNode.KilobytesSecond},
                                  {StandardUnit.MegabytesSecond,UnitGraphNode.MegabytesSecond},
                                  {StandardUnit.GigabytesSecond,UnitGraphNode.GigabytesSecond},
                                  {StandardUnit.TerabytesSecond,UnitGraphNode.TerabytesSecond},
                                  {StandardUnit.BitsSecond,UnitGraphNode.BitsSecond},
                                  {StandardUnit.KilobitsSecond,UnitGraphNode.KilobitsSecond},
                                  {StandardUnit.MegabitsSecond,UnitGraphNode.MegabitsSecond},
                                  {StandardUnit.GigabitsSecond,UnitGraphNode.GigabitsSecond},
                                  {StandardUnit.TerabitsSecond,UnitGraphNode.TerabitsSecond},
                                  {StandardUnit.CountSecond,UnitGraphNode.CountSecond},
                                  {StandardUnit.None,UnitGraphNode.None}
                              };

            UnitGraphNode.Seconds.LinkTo(UnitGraphNode.Milliseconds, 1000);
            UnitGraphNode.Milliseconds.LinkTo(UnitGraphNode.Microseconds, 1000);

            UnitGraphNode.Kilobytes.LinkTo(UnitGraphNode.Bytes, 1024);
            UnitGraphNode.Megabytes.LinkTo(UnitGraphNode.Kilobytes, 1024);
            UnitGraphNode.Gigabytes.LinkTo(UnitGraphNode.Megabytes, 1024);
            UnitGraphNode.Terabytes.LinkTo(UnitGraphNode.Gigabytes, 1024);
            UnitGraphNode.Kilobits.LinkTo(UnitGraphNode.Bits, 1024);
            UnitGraphNode.Megabits.LinkTo(UnitGraphNode.Kilobits, 1024);
            UnitGraphNode.Gigabits.LinkTo(UnitGraphNode.Megabits, 1024);
            UnitGraphNode.Terabits.LinkTo(UnitGraphNode.Gigabits, 1024);
            UnitGraphNode.KilobytesSecond.LinkTo(UnitGraphNode.BytesSecond, 1024);
            UnitGraphNode.MegabytesSecond.LinkTo(UnitGraphNode.KilobytesSecond, 1024);
            UnitGraphNode.GigabytesSecond.LinkTo(UnitGraphNode.MegabytesSecond, 1024);
            UnitGraphNode.TerabytesSecond.LinkTo(UnitGraphNode.GigabytesSecond, 1024);
            UnitGraphNode.KilobitsSecond.LinkTo(UnitGraphNode.BitsSecond, 1024);
            UnitGraphNode.MegabitsSecond.LinkTo(UnitGraphNode.KilobitsSecond, 1024);
            UnitGraphNode.GigabitsSecond.LinkTo(UnitGraphNode.MegabitsSecond, 1024);
            UnitGraphNode.TerabitsSecond.LinkTo(UnitGraphNode.GigabitsSecond, 1024);

            UnitGraphNode.Bytes.LinkTo(UnitGraphNode.Bits, 8);
            UnitGraphNode.BytesSecond.LinkTo(UnitGraphNode.BitsSecond, 8);
        }

        public class UnitGraphNode
        {
            /// <summary>Constant Bits for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Bits = new UnitGraphNode("Bits");


            /// <summary>Constant BitsSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode BitsSecond = new UnitGraphNode("Bits/Second");


            /// <summary>Constant Bytes for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Bytes = new UnitGraphNode("Bytes");


            /// <summary>Constant BytesSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode BytesSecond = new UnitGraphNode("Bytes/Second");


            /// <summary>Constant Count for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Count = new UnitGraphNode("Count");


            /// <summary>Constant CountSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode CountSecond = new UnitGraphNode("Count/Second");


            /// <summary>Constant Gigabits for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Gigabits = new UnitGraphNode("Gigabits");


            /// <summary>Constant GigabitsSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode GigabitsSecond = new UnitGraphNode("Gigabits/Second");


            /// <summary>Constant Gigabytes for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Gigabytes = new UnitGraphNode("Gigabytes");


            /// <summary>Constant GigabytesSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode GigabytesSecond = new UnitGraphNode("Gigabytes/Second");


            /// <summary>Constant Kilobits for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Kilobits = new UnitGraphNode("Kilobits");


            /// <summary>Constant KilobitsSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode KilobitsSecond = new UnitGraphNode("Kilobits/Second");


            /// <summary>Constant Kilobytes for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Kilobytes = new UnitGraphNode("Kilobytes");


            /// <summary>Constant KilobytesSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode KilobytesSecond = new UnitGraphNode("Kilobytes/Second");


            /// <summary>Constant Megabits for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Megabits = new UnitGraphNode("Megabits");


            /// <summary>Constant MegabitsSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode MegabitsSecond = new UnitGraphNode("Megabits/Second");


            /// <summary>Constant Megabytes for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Megabytes = new UnitGraphNode("Megabytes");


            /// <summary>Constant MegabytesSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode MegabytesSecond = new UnitGraphNode("Megabytes/Second");


            /// <summary>Constant Microseconds for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Microseconds = new UnitGraphNode("Microseconds");


            /// <summary>Constant Milliseconds for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Milliseconds = new UnitGraphNode("Milliseconds");


            /// <summary>Constant None for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode None = new UnitGraphNode("None");


            /// <summary>Constant Percent for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Percent = new UnitGraphNode("Percent");


            /// <summary>Constant Seconds for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Seconds = new UnitGraphNode("Seconds");


            /// <summary>Constant Terabits for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Terabits = new UnitGraphNode("Terabits");


            /// <summary>Constant TerabitsSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode TerabitsSecond = new UnitGraphNode("Terabits/Second");


            /// <summary>Constant Terabytes for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode Terabytes = new UnitGraphNode("Terabytes");


            /// <summary>Constant TerabytesSecond for UnitGraphNode
            /// </summary>
            public static readonly UnitGraphNode TerabytesSecond = new UnitGraphNode("Terabytes/Second");

            private StandardUnit _unit;
            private Dictionary<UnitGraphNode, UnitGraphLink> _links;

            private UnitGraphNode(string s)
            {
                _unit = new StandardUnit(s);
            }

            public void LinkTo(UnitGraphNode otherNode, double multiplier)
            {
                Links.Add(otherNode, new UnitGraphLink(otherNode, multiplier));
                otherNode.Links.Add(this, new UnitGraphLink(this, 1.0 / multiplier));
            }

            public Dictionary<UnitGraphNode, UnitGraphLink> Links
            {
                get { return (_links ?? (_links = new Dictionary<UnitGraphNode, UnitGraphLink>())); }
            }

            public StandardUnit Unit
            {
                get { return _unit; }
            }
        }
        public class UnitGraphLink
        {
            private readonly UnitGraphNode _otherNode;
            private readonly double _multiplier;

            public UnitGraphLink(UnitGraphNode otherNode, double multiplier)
            {
                _otherNode = otherNode;
                _multiplier = multiplier;
            }

            public UnitGraphNode OtherNode
            {
                get { return _otherNode; }
            }

            public double Multiplier
            {
                get { return _multiplier; }
            }
        }
    }
}