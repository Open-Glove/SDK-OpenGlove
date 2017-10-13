using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using OpenGlove;
using System.Threading;

namespace OpenGloveWCF
{
    public class webSocketsService
    {
        private static string WSAddress = "ws://localhost:9876";
        private static WebSocketServer wssv = new WebSocketServer("ws://localhost:9876");

        public static Glove RightGlove;
        public static Glove LeftGlove;
        static Thread broadcast;

        static string rightGloveTrackingURL = "/rightGlove";
        static string leftGloveTrackingURL = "/leftGlove";

        public void addEndPoint()
        {
            wssv.AddWebSocketService<WSbase.Laputa>(rightGloveTrackingURL);
            wssv.AddWebSocketService<WSbase.Laputa>(leftGloveTrackingURL);
        }
        public void startWS()
        {
            wssv.Start();
        }

        public void setRightGloveWS(Glove glove)
        {
            RightGlove = glove;
        }

        public void stopWS()
        {
            wssv.Stop();
        }

        public void broadCastFlexors()
        {
            string trackingURL = rightGloveTrackingURL;
            while (true)
            {
                LegacyOpenGlove instance = RightGlove.LegacyGlove;
                Dictionary<int, int> flexors = RightGlove.GloveConfiguration.GloveProfile.FlexorsMappings;
                foreach (KeyValuePair<int, int> mapping in flexors)
                {
                    wssv.WebSocketServices[trackingURL].Sessions.Broadcast(mapping.Key.ToString()+": "+instance.AnalogRead(mapping.Value));
                }
                Thread.Sleep(1000);
            }
            
        }

        public void startBroadcast()
        {
            broadcast = new Thread(broadCastFlexors);
            broadcast.Start();
        }
    }
}
