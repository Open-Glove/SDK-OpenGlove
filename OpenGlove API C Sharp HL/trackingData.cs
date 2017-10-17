using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OpenGlove_API_C_Sharp_HL
{
    public class trackingData
    {
        public Dictionary<string, int> FlexorsValues { get; set; }

        public  Dictionary <int, int> AccelerometerValues { get; set; }
    }
}
