using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMailServices
{
    public class MailRequest
    {
        public string to { get; set; }
        public string subject { get; set; }
        public string message { get; set; }
    }
}
