using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using OpenGlove;

namespace OpenGloveWCF
{
    class webSocketsService
    {
        static Glove RightGlove { get; set; }
        static Glove LeftGlove { get; set; }

        private static string WSAddress = "ws://localhost:9876";
        private static WebSocketServer wssv = new WebSocketServer(WSAddress);

        public void addEndPoint()
        {
            wssv.AddWebSocketService<WSbase.Laputa>("/rightGlove");
            wssv.AddWebSocketService<WSbase.Laputa>("/leftGlove");
        }
        public void startWS()
        {
            wssv.Start();
        }

        public void stopWS()
        {
            wssv.Stop();
        }

        public void broadCastFlexors(Glove selectedGlove)
        {

        }

    }
}
