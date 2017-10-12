using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGlove;
using System.Runtime.Serialization;
using System.IO.Ports;

namespace testwebSockets.Gloves
{
    public class Glove{

        public string Name;
            
        public string Port;
            
        public Sides Side;
             
        public string BluetoothAddress;
            
        public bool Connected;
 
        public Configuration GloveConfiguration;

        public LegacyOpenGlove LegacyGlove { get; set; }
             
        public class Configuration
        {
            public int BaudRate;

            public List<int> AllowedBaudRates = new List<int> { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200 };
   
            public List<int> PositivePins;
                 
            public List<int> NegativePins;
                 
            public List<int> FlexPins;
                 
            public List<string> NegativeInit;
                 
            public List<string> PositiveInit;
             
            public List<string> FlexInit;
                 
            public String GloveHash;
                 
            public String GloveName;
                 
            public Profile GloveProfile;
                 
            public class Profile
            {                  
                public String ProfileName;
                     
                public String GloveHash;
                     
                public int AreaCount = 58;
                     
                public Dictionary<string, string> Mappings = new Dictionary<string, string>();
                     
                public Dictionary<int, int> FlexorsMappings = new Dictionary<int, int>();
                    
                public Dictionary<int, int> FlexorsMappingsValues = new Dictionary<int, int>();
            }
        }

        public enum Sides
        {
            [EnumMember]
            Right,
            [EnumMember]
            Left
        }

    }

}
