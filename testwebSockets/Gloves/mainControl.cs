using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGlove;

namespace testwebSockets.Gloves
{
    public class mainControl
    {
        List<Glove> availableGloves { get; set; }
        Glove RightGlove { get; set;}
        Glove LeftGlove { get; set; }
      
        public List<Glove> Gloves
        {
            get
            {
                if (availableGloves == null)
                {
                    availableGloves = ScanGloves();
                }
                return availableGloves;
            }
        }

        private static List<Glove> ScanGloves()
        {
            List<Glove> scannedGloves = new List<Glove>();
            var devices = SerialPort.GetPortNames(); ;
            foreach (var device in devices)
            {
                string deviceAddress = device;
                string comPort = device;
                string address = device;
                string name = device;

                Glove foundGlove = new Glove();
                foundGlove.BluetoothAddress = deviceAddress;
                foundGlove.Port = comPort;
                foundGlove.Name = name;
                foundGlove.Connected = false;
                foundGlove.LegacyGlove = new LegacyOpenGlove();
                scannedGloves.Add(foundGlove);
            }
            return scannedGloves;
        }

        public List<Glove> RefreshGloves()
        {
            this.availableGloves = ScanGloves();
            return availableGloves;
        }
    }
}
