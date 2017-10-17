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

        public class FlexorsEndPoint : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                var msg = e.Data == "ayuda"
                          ? "Primero configura el guante y luego activa la obtención de datos..."
                          : "OpenGlove WebSockets aun no implementa funciones para datos entrantes, lo siento";
                Send(msg);
            }
        }
    }
}
