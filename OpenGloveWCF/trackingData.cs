using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OpenGloveWCF
{
    public class trackingData
    {
        public Dictionary<int, int> FlexorsValues { get; set; }

        public  Dictionary <int, int> AccelerometerValues { get; set; }
    }
}
