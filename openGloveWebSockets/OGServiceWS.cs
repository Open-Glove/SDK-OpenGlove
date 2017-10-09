
using OpenGlove;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace openGloveWebSockets
{
    public class OGServiceWS : IOGServiceWS
    {
        Task IOGServiceWS.Hello(string message)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IOGServiceWSCallback>();
            return callback.OnHello("Haz dicho: " + message);
        }
    }
}
