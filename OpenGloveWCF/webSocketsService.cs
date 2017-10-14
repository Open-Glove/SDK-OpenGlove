using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using OpenGlove;
using System.Threading;
using System.Runtime.Serialization;

namespace OpenGloveWCF
{
    public class webSocketsService
    {
        
        private static string WSAddress = "ws://localhost:9876";
        private static WebSocketServer wssv = new WebSocketServer(WSAddress);
        public static int delay = 60;
        public static Glove RightGlove, LeftGlove;
        static Thread broadcastRG, broadcastLG;
        private static Boolean rightFlag, leftFlag;

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

        public void setLeftGloveWS(Glove glove)
        {
            LeftGlove = glove;
        }

        public void stopWS()
        {
            wssv.Stop();
        }

        public void broadCastFlexors(string trackingURL)
        {
            Boolean flag;
            Dictionary<int, int> flexors;
            LegacyOpenGlove instance;
            if (trackingURL.Equals(rightGloveTrackingURL))
            {
                instance = RightGlove.LegacyGlove;
                flexors = RightGlove.GloveConfiguration.GloveProfile.FlexorsMappings;
                rightFlag = true;
                flag = rightFlag;
            }
            else
            {
                instance = LeftGlove.LegacyGlove;
                flexors = LeftGlove.GloveConfiguration.GloveProfile.FlexorsMappings;
                leftFlag = true;
                flag = leftFlag;
            }
            TimeSpan stop, start;
            
            while (true)
            {
                start = new TimeSpan(DateTime.Now.Ticks);
                foreach (KeyValuePair<int, int> mapping in flexors)
                {
                    wssv.WebSocketServices[trackingURL].Sessions.Broadcast(mapping.Value.ToString()+": "+instance.AnalogRead(mapping.Value));
                }
                stop = new TimeSpan(DateTime.Now.Ticks);
                double a = stop.Subtract(start).TotalMilliseconds;
                wssv.WebSocketServices[trackingURL].Sessions.Broadcast("tiempo: "+a);
                Thread.Sleep(delay);
            }
        }

        public void startBroadcast(Sides side)
        {
            if (side == Sides.Right)
            {
                broadcastRG = new Thread(() => broadCastFlexors(rightGloveTrackingURL));
                broadcastRG.Start();
            }else
            {
                broadcastLG = new Thread(() => broadCastFlexors(leftGloveTrackingURL));
                broadcastLG.Start();
            }
        }

        public void stopBroadcast(Sides side)
        {
            if (side == Sides.Right)
            {
               // rightFlag = false;
                broadcastRG.Abort();
            }
            else
            {
               // leftFlag = false;
                broadcastLG.Abort();
            }     
        }

    }
}
