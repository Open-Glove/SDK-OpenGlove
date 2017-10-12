using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using testwebSockets.ServiceReference1;

namespace testwebSockets
{
   

    class WCF
    {
        private static WCF instance;
        private OGServiceClient serviceClient;
        WCF()
        {
            NetHttpBinding binding = new NetHttpBinding();
            EndpointAddress address = new EndpointAddress("http://localhost:8733/Design_Time_Addresses/OpenGloveWCF/OGService/");
            serviceClient = new OGServiceClient(binding, address);
        }

        public static WCF GetInstance()
        {
            if (instance == null)
            {
                instance = new WCF();
            }
            return instance;
        }
    }

}  
