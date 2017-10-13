using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace OpenGloveWCF
{
    public class WSbase
    {
        private static List<int> flexors = new List<int> {16,17,19 };

        public class Laputa : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                var msg = e.Data == "BALUS"
                          ? "I've been balused already..."
                          : "I'm not available now.";
                Send(msg);
            }
        }
    }
}
