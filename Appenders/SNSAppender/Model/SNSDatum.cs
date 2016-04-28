using System.IO;

namespace SQSAppender.Model
{
    public class SNSDatum
    {
        public SNSDatum(string message)
        {
            Message = message;
        }

        public SNSDatum()
        {
        }

        public string Message { get; set; }
        public string Topic { get; set; }
        //public string ID { get; set; }
        //public int? DelaySeconds { get; set; }

        public override string ToString()
        {
            var s = new StringWriter();
            new SNSDatumRenderer().RenderObject(null, this, s);

            return s.ToString();
        }
    }
}