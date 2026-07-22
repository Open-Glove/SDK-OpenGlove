using OpenGlove_API_C_Sharp_HL.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace OpenGlove_API_C_Sharp_HL
{
    public class DataReceiver
    {
        public string WebSocketPort;
        public string SerialPort;
        WebSocket WebSocketClient;
        public bool WebSocketActive = false;
        Task webSocketTask;

        public DataReceiver(string WebSocketPort, string SerialPort)
        {
            this.WebSocketPort = WebSocketPort;
            this.SerialPort = SerialPort;
            WebSocketClient = new WebSocket("ws://localhost:" + WebSocketPort + "/" + SerialPort);
            try
            {
                webSocketTask = Task.Run(() =>
                {
                    readData();
                });
            }
            catch
            {
                Console.WriteLine("Error");
            }
        }

        public delegate void FingerMovement(int region, int value);
        public delegate void accelerometerValues(float ax, float ay, float az);
        public delegate void gyroscopeValues(float gx, float gy, float gz);
        public delegate void magnometerValues(float mx, float my, float mz);
        public delegate void attitudeValues(float yx, float yy, float yz);
        public delegate void allIMUValues(float ax, float ay, float az, float gx, float gy, float gz, float mx, float my, float mz);
        public delegate void quaternionValues(float qx, float qy, float qz);
        /// <summary>Brazo (w,x,y,z), Antebrazo (w,x,y,z). Formato ESP32: w1,x1,y1,z1,w2,x2,y2,z2</summary>
        public delegate void dualQuaternionValues(float w1, float x1, float y1, float z1, float w2, float x2, float y2, float z2);

        public event FingerMovement fingersFunction;
        public event accelerometerValues accelerometerFunction;
        public event gyroscopeValues gyroscopeFunction;
        public event magnometerValues magnometerFunction;
        public event allIMUValues imu_ValuesFunction;
        public event attitudeValues imu_attitudeFunction;
        public event quaternionValues quaternionFunction;
        public event dualQuaternionValues dualQuaternionFunction;

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
                                case "r":
                                    imu_attitudeFunction?.Invoke(float.Parse(words[1], CultureInfo.InvariantCulture), float.Parse(words[2], CultureInfo.InvariantCulture), float.Parse(words[3], CultureInfo.InvariantCulture));
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
                                case "q":
                                    quaternionFunction?.Invoke(
                                        float.Parse(words[1], CultureInfo.InvariantCulture),
                                        float.Parse(words[2], CultureInfo.InvariantCulture),
                                        float.Parse(words[3], CultureInfo.InvariantCulture)
                                    );
                                    break;
                                case "Q":
                                    // Formato ESP32: Q,w1,x1,y1,z1,w2,x2,y2,z2 (brazo + antebrazo). Ignorar líneas corruptas.
                                    if (words.Length == 9 &&
                                        float.TryParse(words[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float rw1) &&
                                        float.TryParse(words[2], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float rx1) &&
                                        float.TryParse(words[3], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float ry1) &&
                                        float.TryParse(words[4], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float rz1) &&
                                        float.TryParse(words[5], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float rw2) &&
                                        float.TryParse(words[6], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float rx2) &&
                                        float.TryParse(words[7], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float ry2) &&
                                        float.TryParse(words[8], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float rz2))
                                    {
                                        dualQuaternionFunction?.Invoke(rw1, rx1, ry1, rz1, rw2, rx2, ry2, rz2);
                                    }
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
    }
}
