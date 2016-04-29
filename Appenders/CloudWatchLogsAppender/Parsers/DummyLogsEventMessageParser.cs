namespace AWSAppender.CloudWatchLogs.Parsers
{
    public class DummyLogsEventMessageParser : LogsEventMessageParser
    {
        public DummyLogsEventMessageParser() : base(true)
        {
        }

        protected override bool ShouldLocalParse(string t0)
        {
            return false;
        }

        protected override bool IsSupportedName(string t0)
        {
            return false;
        }
    }
}