using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace testwebSockets
{
    class Program
    {
        static void Main(string[] args)
        {
            /* var wssv = new WebSocketServer("ws://dragonsnest.far");
             wssv.AddWebSocketService<Flexors>("/getflexors");
             wssv.Start();
             Console.ReadKey(true);
             wssv.Stop();*/
            Gloves.mainControl m = new Gloves.mainControl();

        }
    }
}
