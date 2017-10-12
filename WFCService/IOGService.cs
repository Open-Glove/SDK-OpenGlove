
using OpenGlove;
using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Management;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;

namespace WFCService
{
    [ServiceContract]
    public interface IOGService
    {
        [OperationContract]
        [WebInvoke(Method = "GET",
                    ResponseFormat = WebMessageFormat.Json,
                    RequestFormat = WebMessageFormat.Json,
                    UriTemplate = "GetGloves")]
        List<Glove> GetGloves();

        [OperationContract]
        [WebInvoke(Method = "GET",
                    ResponseFormat = WebMessageFormat.Json,
                    RequestFormat = WebMessageFormat.Json,
                    UriTemplate = "RefreshGloves")]
        List<Glove> RefreshGloves();

        [OperationContract]
        [WebInvoke(Method = "PUT",
                    ResponseFormat = WebMessageFormat.Json,
                    RequestFormat = WebMessageFormat.Json,
                    UriTemplate = "SaveGlove")]
        void SaveGlove(Glove glove);

        [OperationContract]
        [WebInvoke(Method = "POST",
                    ResponseFormat = WebMessageFormat.Json,
                    RequestFormat = WebMessageFormat.Json,
                    BodyStyle = WebMessageBodyStyle.Bare,
                    UriTemplate = "Activate?gloveAddress={gloveAddress}&actuator={actuator}&intensity={intensity}")]
        int Activate(string gloveAddress, int actuator, int intensity);

        [OperationContract]
        [WebInvoke(Method = "PUT",
                    ResponseFormat = WebMessageFormat.Json,
                    RequestFormat = WebMessageFormat.Json,
                    BodyStyle = WebMessageBodyStyle.Bare,
                    UriTemplate = "Connect?gloveAddress={gloveAddress}")]
        int Connect(string gloveAddress);

        [OperationContract]
        [WebInvoke(Method = "PUT",
                    ResponseFormat = WebMessageFormat.Json,
                    RequestFormat = WebMessageFormat.Json,
                    BodyStyle = WebMessageBodyStyle.Bare,
                    UriTemplate = "Disconnect?gloveAddress={gloveAddress}")]
        int Disconnect(string gloveAddress);

        [OperationContract]
        [WebInvoke(Method = "*",
                    ResponseFormat = WebMessageFormat.Json,
                    RequestFormat = WebMessageFormat.Json,
                    BodyStyle = WebMessageBodyStyle.Wrapped,
                    UriTemplate = "ActivateMany?gloveAddress={gloveAddress}")]
        int ActivateMany(string gloveAddress, List<int> actuators, List<int> intensityList);
    }

    [ServiceContract]
    public interface IgloveFlexorsServiceCallBack
    {
        [OperationContract(IsOneWay = true)]
        void funcion();
    }

    [ServiceContract(CallbackContract = typeof(IgloveFlexorsServiceCallBack))]
    public interface IgloveFlexorsService
    {
        [OperationContract(IsOneWay = true)]
        void StartSendingQuotes();
    }


    [ServiceContract(CallbackContract = typeof(IWSChatCallback))]
    public interface IWSChatService
    {
        [OperationContract(IsOneWay = true)]
        Task SendMessageToServer(string msg);
    }

    [ServiceContract]
    interface IWSChatCallback
    {
        [OperationContract(IsOneWay = true)]
        Task SendMessageToClient(string msg);
    }
    /*
    [DataContract]
    public class ActivateManyData {
        [DataMember]
        string gloveAddress;

        [DataMember]
        List<int> actuators;

        [DataMember]
        List<int> intensityList;
    }
    */

    [DataContract]
    public class Glove
    {
        /// <summary>
        /// Private singleton field containing all active gloves, connected or disconnected in the system.
        /// </summary>
        private static List<Glove> gloves;

        /// <summary>
        /// Gets the current list of gloves connected to the system. If it is the first 
        /// execution since the service start, it will refresh the list.
        /// </summary>
        public static List<Glove> Gloves
        {
            get
            {
                if (gloves == null)
                {
                    gloves = ScanGloves();

                }
                return gloves;
            }
        }

        /// <summary>
        /// Same behaviour as Gloves, but always refreshes the glove list.
        /// </summary>
        /// <returns></returns>
        public static List<Glove> RefreshGloves()
        {
            gloves = ScanGloves();
            return gloves;
        }

        /// <summary>
        /// Scans the system using 32Feet.NET for OpenGlove devices. Currently it only filters by the bluetooth device Name, so
        /// any device containing "OpenGlove" on their name would be picked. Hardware limitation.
        /// </summary>
        /// <returns></returns>
        private static List<Glove> ScanGloves()
        {

            List<Glove> scannedGloves = new List<Glove>();

            // var bluetoothClient = new BluetoothClient();

            //var devices = bluetoothClient.DiscoverDevices();

            var devices = SerialPort.GetPortNames(); ;
            foreach (var device in devices)

            {
                /* if (device.DeviceName.Contains("OpenGlove"))
                 {*/
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
                // }
            }
            return scannedGloves;
        }

        [DataMember]
        public string Name;

        [DataMember]
        public string Port;

        [DataMember]
        public Sides Side;

        [DataMember]
        public string BluetoothAddress;

        [DataMember]
        public bool Connected;

        [DataMember]
        public Configuration GloveConfiguration;

        public LegacyOpenGlove LegacyGlove { get; set; }


        [DataContract]
        public class Configuration
        {

            [DataMember]
            public int BaudRate;

            [DataMember]
            public List<int> AllowedBaudRates = new List<int> { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200 };

            [DataMember]
            public List<int> PositivePins;

            [DataMember]
            public List<int> NegativePins;

            [DataMember]
            public List<int> FlexPins;

            [DataMember]
            public List<string> NegativeInit;

            [DataMember]
            public List<string> PositiveInit;

            [DataMember]
            public List<string> FlexInit;

            [DataMember]
            public String GloveHash;

            [DataMember]
            public String GloveName;

            [DataMember]
            public Profile GloveProfile;

            [DataContract]
            public class Profile
            {
                [DataMember]
                public String ProfileName;

                [DataMember]
                public String GloveHash;

                [DataMember]
                public int AreaCount = 58;

                [DataMember]
                public Dictionary<string, string> Mappings = new Dictionary<string, string>();

                [DataMember]
                public Dictionary<int, int> FlexorsMappings = new Dictionary<int, int>();
            }
        }


    }

    [DataContract(Name = "Side")]
    public enum Sides
    {
        [EnumMember]
        Right,
        [EnumMember]
        Left
    }
}
