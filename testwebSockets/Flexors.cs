using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using WebSocketSharp;

namespace testwebSockets
{

    class Flexors : WebSocketBehavior
    {
        WCF service = WCF.GetInstance();

        public List<int> flexors { get; set; }
        
        private void getFlex(string address){
            
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var msg = e.Data == "BALUS"
                      ? "I've been balused already..."
                      : "I'm not available now.";

            Send(msg);
        }

    }
}
