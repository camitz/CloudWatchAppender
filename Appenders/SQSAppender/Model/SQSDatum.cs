using System.IO;

namespace AWSAppender.SQS.Model
{
    public class SQSDatum
    {
        public SQSDatum(string message)
        {
            Message = message;
        }

        public SQSDatum()
        {
        }

        public string Message { get; set; }
        public string QueueName { get; set; }
        public string ID { get; set; }
        public int? DelaySeconds { get; set; }

        public override string ToString()
        {
            var s = new StringWriter();
            new SQSDatumRenderer().RenderObject(null, this, s);

            return s.ToString();
        }
    }
}