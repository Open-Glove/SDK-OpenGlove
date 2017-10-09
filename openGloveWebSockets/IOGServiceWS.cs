using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace openGloveWebSockets
{
    [ServiceContract(CallbackContract = typeof(IOGServiceWSCallback))]
    public interface IOGServiceWS
    {
        [OperationContract(IsOneWay = true)]
        Task Hello(string message);
    }
    [ServiceContract]
    public interface IOGServiceWSCallback {
        [OperationContract(IsOneWay = true)]
        Task OnHello(string message);
    }

}
