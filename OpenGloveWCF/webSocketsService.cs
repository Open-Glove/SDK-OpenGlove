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
using NetJSON;

namespace OpenGloveWCF
{
    public class webSocketsService
    {
        
        private static string WSAddress = "ws://localhost:9876";
        private static WebSocketServer wssv = new WebSocketServer(WSAddress);
        public static int delay = 60;
        public static Glove RightGlove, LeftGlove;
        static Thread broadcastRG, broadcastLG;

        static string rightGloveTrackingURL = "/rightGlove";
        static string leftGloveTrackingURL = "/leftGlove";

        public void addEndPoint()
        {
            wssv.AddWebSocketService<WSbase.FlexorsEndPoint>(rightGloveTrackingURL);
            wssv.AddWebSocketService<WSbase.FlexorsEndPoint>(leftGloveTrackingURL);
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
            Dictionary<int, int> flexors;
            LegacyOpenGlove instance;
            if (trackingURL.Equals(rightGloveTrackingURL))
            {
            //    RightGlove = Glove.getRightlove();
                instance = RightGlove.LegacyGlove;
                flexors = RightGlove.GloveConfiguration.GloveProfile.FlexorsMappings;
            }
            else
            {
            //    LeftGlove = Glove.getLeftGlove();
                instance = LeftGlove.LegacyGlove;
                flexors = LeftGlove.GloveConfiguration.GloveProfile.FlexorsMappings;
            }
            TimeSpan stop, start;
            Boolean iguales = Object.ReferenceEquals(RightGlove, Glove.getRightlove());
            trackingData data = new trackingData();
            data.FlexorsValues = new Dictionary<int, int>();
            data.AccelerometerValues = new Dictionary<int, int>();
            while (true)
            {
                start = new TimeSpan(DateTime.Now.Ticks);
                foreach (KeyValuePair<int, int> mapping in flexors)
                {
                    data.FlexorsValues.Add(mapping.Key, Int32.Parse(instance.AnalogRead(mapping.Value)));
                }
                var json = NetJSON.NetJSON.Serialize(data);
                data.FlexorsValues.Clear();

                // wssv.WebSocketServices[trackingURL].Sessions.Broadcast(mapping.Value.ToString() + ": " + instance.AnalogRead(mapping.Value));
                wssv.WebSocketServices[trackingURL].Sessions.Broadcast(json);
                stop = new TimeSpan(DateTime.Now.Ticks);
                double a = stop.Subtract(start).TotalMilliseconds;
               // wssv.WebSocketServices[trackingURL].Sessions.Broadcast("tiempo: "+a);
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
                broadcastRG.Abort();
            }
            else
            {
                broadcastLG.Abort();
            }     
        }

    }
}
