using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotHub
{
    public class ReplyMessageModel
    {
        public ReplyMessageModel()
        {
            messages = new List<Message>();
        }
        public string replyToken { get; set; }
        public List<Message> messages { get; set; }
    }
    public class PushMessageModel
    {
        public PushMessageModel()
        {
            messages = new List<Message>();
        }
        public string to { get; set; }
        public List<Message> messages { get; set; }
    }
     
     
    public class Message
    {
        public string type { get; set; }
        public string text { get; set; }
        public string title { get; set; }
        public string address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }
    public   class TeparakDTO
    {
        public string ID { get; set; }
        public double T { get; set; }
        public double H { get; set; }
        public int Status { get; set; }
        public string Location { get; set; }
        public long ReportDate { get; set; }
    }
}
