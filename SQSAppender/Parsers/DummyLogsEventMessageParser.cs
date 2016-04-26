namespace CloudWatchAppender.Parsers
{
    public class DummyLogsEventMessageParser : SQSMessageParser
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