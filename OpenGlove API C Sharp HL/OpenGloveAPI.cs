using OpenGlove_API_C_Sharp_HL.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebSocketSharp;

namespace OpenGlove_API_C_Sharp_HL
{
    public class OpenGloveAPI
    {
        /// <summary>
        /// Singleton instance of the API
        /// </summary>
        private static OpenGloveAPI instance;
        bool WebSocketActive;
        /// <summary>
        /// Client for the WCF service
        /// </summary>
        private OGServiceClient serviceClient;

        OpenGloveAPI()
        {
            NetHttpBinding binding = new NetHttpBinding();
            EndpointAddress address = new EndpointAddress("http://localhost:8733/Design_Time_Addresses/OpenGloveWCF/OGService/");
            serviceClient = new OGServiceClient(binding, address);
            WebSocketActive = false;
        }
        /// <summary>
        /// Gets the current API instance
        /// </summary>
        /// <returns>Current API instance</returns>
        public static OpenGloveAPI GetInstance()
        {
            if (instance == null)
            {
                instance = new OpenGloveAPI();
            }
            return instance;
        }

        public delegate void FingerMovement(int region, int value);
        public delegate void accelerometerValues(float ax, float ay, float az);
        public delegate void gyroscopeValues(float gx, float gy, float gz);
        public delegate void magnometerValues(float mx, float my, float mz);
        public delegate void allIMUValues(float ax, float ay, float az, float gx, float gy, float gz,float mx, float my, float mz);
        WebSocket WebSocketClient;
        public event FingerMovement fingersFunction;
        public event accelerometerValues accelerometerFunction;
        public event gyroscopeValues gyroscopeFunction;
        public event magnometerValues magnometerFunction;
        public event allIMUValues imu_ValuesFunction;

        public void readData( )
        {
            using (WebSocketClient)
            {
                int mapping, value;
                float valueX, valueY, valueZ;
                string[] words;

                WebSocketClient.OnMessage += (sender, e) => {
                    
                    if (e.Data != null)
                    {
                        words = e.Data.Split(',');
                        try
                        {
                            switch (words[0])
                            {
                                case "f":
                                    mapping = Int32.Parse(words[1]);
                                    value = Int32.Parse(words[2]);
                                    fingersFunction?.Invoke(mapping, value);
                                    break;
                                case "a":
                                    valueX = float.Parse(words[1], CultureInfo.InvariantCulture);
                                    valueY = float.Parse(words[2], CultureInfo.InvariantCulture);
                                    valueZ = float.Parse(words[3], CultureInfo.InvariantCulture);
                                    accelerometerFunction?.Invoke(valueX,valueY,valueZ);
                                    break;
                                case "g":
                                    valueX = float.Parse(words[1], CultureInfo.InvariantCulture);
                                    valueY = float.Parse(words[2], CultureInfo.InvariantCulture);
                                    valueZ = float.Parse(words[3], CultureInfo.InvariantCulture);
                                    gyroscopeFunction?.Invoke(valueX, valueY, valueZ);
                                    break;
                                case "m":
                                    valueX = float.Parse(words[1], CultureInfo.InvariantCulture);
                                    valueY = float.Parse(words[2], CultureInfo.InvariantCulture);
                                    valueZ = float.Parse(words[3], CultureInfo.InvariantCulture);
                                    magnometerFunction?.Invoke(valueX, valueY, valueZ);
                                    break;
                                case "z":
                                    imu_ValuesFunction?.Invoke(float.Parse(words[1], CultureInfo.InvariantCulture), float.Parse(words[2], CultureInfo.InvariantCulture), float.Parse(words[3], CultureInfo.InvariantCulture), float.Parse(words[4], CultureInfo.InvariantCulture), float.Parse(words[5], CultureInfo.InvariantCulture), float.Parse(words[6], CultureInfo.InvariantCulture), float.Parse(words[7], CultureInfo.InvariantCulture), float.Parse(words[8], CultureInfo.InvariantCulture), float.Parse(words[9], CultureInfo.InvariantCulture));
                                    break;
                                default:
                                    break;
                            }
                            
                        }
                        catch
                        {
                            Console.WriteLine("ERROR: BAD FORMAT");
                        }
                    }
                };
                WebSocketClient.Connect();
                WebSocketActive = true;
                while (WebSocketActive == true) { }
            }
        }

        Task mytask;

        public void startCaptureData(Glove selectedGlove)
        {
            WebSocketClient = new WebSocket("ws://localhost:"+selectedGlove.WebSocketPort + "/" + selectedGlove.Port);
            try
            {
                mytask = Task.Run(() =>
                {
                    readData();
                });
            }
            catch
            {

            }
        }

        public void stopCaptureData()
        {
            WebSocketClient.Close();
            WebSocketActive = false;
        }

        /// <summary>
        /// List of current connected devices
        /// </summary>
        private List<Glove> devices;

        /// <summary>
        /// Property for getting current devices
        /// </summary>
        public List<Glove> Devices
        {
            get
            {
                devices = serviceClient.GetGloves().ToList();
                return devices;
            }
        }

        /// <summary>
        /// Refreshes the current devices list
        /// </summary>
        /// <returns></returns>
        public List<Glove> UpdateDevices()
        {
            return serviceClient.RefreshGloves().ToList();
        }

        public int addFlexor(Glove selectedGlove, int flexor, int mapping)
        {
            return this.serviceClient.addFlexor(selectedGlove.BluetoothAddress, flexor, mapping);
        }

        public int removeFlexor(Glove selectedGlove, int mapping)
        {
            return this.serviceClient.removeFlexor(selectedGlove.BluetoothAddress, mapping);
        }

        public void calibrateFlexors(Glove selectedGlove)
        {
           this.serviceClient.calibrateFlexors(selectedGlove.BluetoothAddress);
        }

        public void confirmCalibration(Glove selectedGlove)
        {
            this.serviceClient.confirmCalibration(selectedGlove.BluetoothAddress);
        }

        public void setThreshold(Glove selectedGlove, int value)
        {
            this.serviceClient.setThreshold(selectedGlove.BluetoothAddress,value);
        }

        public void resetFlexors(Glove selectedGlove)
        {
            this.serviceClient.resetFlexors(selectedGlove.BluetoothAddress);
        }

        public void startIMU(Glove selectedGlove)
        {
            this.serviceClient.startIMU(selectedGlove.BluetoothAddress);
        }

        public void setIMUStatus(Glove selectedGlove, bool value)
        {
            if (value == true)
            {
                this.serviceClient.setIMUStatus(selectedGlove.BluetoothAddress, 1);
            }else
            {
                this.serviceClient.setIMUStatus(selectedGlove.BluetoothAddress, 0);
            }
            
        }

        public void setRawData(Glove selectedGlove, bool value)
        {
            if (value == true)
            {
                this.serviceClient.setRawData(selectedGlove.BluetoothAddress, 1);
            }
            else
            {
                this.serviceClient.setRawData(selectedGlove.BluetoothAddress, 0);
            }

        }







        /// <summary>
        /// Establishes connection with a glove
        /// </summary>
        /// <param name="selectedGlove">A Glove object to be connected</param>
        /// <returns>Result code</returns>
        public int Connect(Glove selectedGlove)
        {
            try
            {
                return this.serviceClient.Connect(selectedGlove.BluetoothAddress);
            }
            catch (Exception)
            {

                return -1;
            }
        }

        /// <summary>
        /// Closes a connection with a glove
        /// </summary>
        /// <param name="selectedGlove">A Glove object to be connected</param>
        /// <returns>Result code</returns>
        public int Disconnect(Glove selectedGlove)
        {
            try
            {
                return this.serviceClient.Disconnect(selectedGlove.BluetoothAddress);
            }
            catch (Exception)
            {

                return -1;
            }
        }

        /// <summary>
        /// Activates a region with certain intensity
        /// </summary>
        /// <param name="selectedGlove"></param>
        /// <param name="region"></param>
        /// <param name="intensity"></param>
        public void Activate(Glove selectedGlove, int region, int intensity)
        {
            int actuator = -1;
            foreach (var item in selectedGlove.GloveConfiguration.GloveProfile.Mappings)
            {
                if (item.Key.Equals(region.ToString()))
                {
                    actuator = Int32.Parse(item.Value);
                    break;
                }
            }
            if (actuator == -1)
            {
                return;
            }
            this.serviceClient.Activate(selectedGlove.BluetoothAddress, actuator, intensity);

        }

        public void Activate(Glove selectedGlove, List<int> regions, List<int> intensityList)
        {
            List<int> actuators = new List<int>();

            foreach (var region in regions)
            {
                int actuator = -1;
                foreach (var item in selectedGlove.GloveConfiguration.GloveProfile.Mappings)
                {
                    if (item.Key.Equals(region.ToString()))
                    {
                        actuator = Int32.Parse(item.Value);
                        break;
                    }
                }
                if (actuator == -1)
                {
                    return;
                }
                actuators.Add(actuator);
            }

            this.serviceClient.ActivateMany(selectedGlove.BluetoothAddress, actuators.ToArray(), intensityList.ToArray());
        }
    }


    public enum PalmarRegion
    {
        FingerSmallDistal,
        FingerRingDistal,
        FingerMiddleDistal,
        FingerIndexDistal,

        FingerSmallMiddle,
        FingerRingMiddle,
        FingerMiddleMiddle,
        FingerIndexMiddle,

        FingerSmallProximal,
        FingerRingProximal,
        FingerMiddleProximal,
        FingerIndexProximal,

        PalmSmallDistal,
        PalmRingDistal,
        PalmMiddleDistal,
        PalmIndexDistal,

        PalmSmallProximal,
        PalmRingProximal,
        PalmMiddleProximal,
        PalmIndexProximal,

        HypoThenarSmall,
        HypoThenarRing,
        ThenarMiddle,
        ThenarIndex,

        FingerThumbProximal,
        FingerThumbDistal,

        HypoThenarDistal,
        Thenar,

        HypoThenarProximal
    }

    public enum DorsalRegion
    {
        FingerSmallDistal = 29,
        FingerRingDistal,
        FingerMiddleDistal,
        FingerIndexDistal,

        FingerSmallMiddle,
        FingerRingMiddle,
        FingerMiddleMiddle,
        FingerIndexMiddle,

        FingerSmallProximal,
        FingerRingProximal,
        FingerMiddleProximal,
        FingerIndexProximal,

        PalmSmallDistal,
        PalmRingDistal,
        PalmMiddleDistal,
        PalmIndexDistal,

        PalmSmallProximal,
        PalmRingProximal,
        PalmMiddleProximal,
        PalmIndexProximal,

        HypoThenarSmall,
        HypoThenarRing,
        ThenarMiddle,
        ThenarIndex,

        FingerThumbProximal,
        FingerThumbDistal,

        HypoThenarDistal,
        Thenar,

        HypoThenarProximal
    }

    public enum FlexorsRegion
    {
        ThumbInterphalangealJoint = 0,
        IndexInterphalangealJoint,
        MiddleInterphalangealJoint,
        RingInterphalangealJoint,
        SmallInterphalangealJoint,

        ThumbMetacarpophalangealJoint,
        IndexMetacarpophalangealJoint,
        MiddleMetacarpophalangealJoint,
        RingMetacarpophalangealJoint,
        SmallMetacarpophalangealJoint
    }
}
