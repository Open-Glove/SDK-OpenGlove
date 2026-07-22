using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Globalization;
using System.Text;

namespace OpenGlove
{
    /// <summary>
    /// Represents  a communication instance between the API and the glove. 
    /// Provide methods for send and receive data through serial port
    /// </summary>
    class Communication
    {
        public enum ImuParseMode
        {
            Bno055,
            Lsm9ds1
        }

        /// <summary>
        /// Serial port communication field. 
        /// </summary>
        private SerialPort port = new SerialPort();

        private int websocketBase = 9870;
        private int portNumber;
        private string WSAddress;
        private WebSocketServer wssv;

        private readonly object _sbLock = new object();
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly CultureInfo _inv = CultureInfo.InvariantCulture;
        private ImuParseMode _imuMode = ImuParseMode.Bno055;

        public event Action<float, float, float, float, float, float, float, float, float> imu_ValuesFunction;

        public class WSbase
        {
            public class GloveEndPoint : WebSocketBehavior
            {
                protected override void OnMessage(MessageEventArgs e)
                {
                    var msg = e.Data == "ayuda"
                              ? "Primero configura el guante y luego activa la obtención de datos..."
                              : "OpenGlove WebSockets aun no implementa funciones para datos entrantes...";
                    Send(msg);
                }
            }
        }

        public Communication()
        {
        }

        public Communication(string portName, int baudRate)
        {
            port.PortName = portName;
            port.BaudRate = baudRate;
            port.Open();
        }

        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// Configura el parser según el modelo del perfil: BNO055 (dual quat) o LSM9DS1 (EOF legacy).
        /// </summary>
        public void SetImuModel(string imuModel)
        {
            if (string.Equals(imuModel, "LSM9DS1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(imuModel, "Default", StringComparison.OrdinalIgnoreCase))
            {
                _imuMode = ImuParseMode.Lsm9ds1;
            }
            else
            {
                _imuMode = ImuParseMode.Bno055;
            }
        }

        public void OpenPort(string portName, int baudRate)
        {
            portNumber = int.Parse(Regex.Replace(portName, @"[^\d]", ""));
            WSAddress = "ws://localhost:" + (websocketBase + portNumber).ToString();
            wssv = new WebSocketServer(WSAddress);
            port.PortName = portName;
            port.BaudRate = baudRate;
            port.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
            wssv.AddWebSocketService<WSbase.GloveEndPoint>("/" + port.PortName);
            wssv.Start();

            port.NewLine = "\n";
            port.ReadTimeout = 2000;
            port.DtrEnable = true;
            port.RtsEnable = true;
            port.Handshake = Handshake.None;

            port.Open();
        }

        public void Write(string data)
        {
            port.Write(data);
        }

        public string ReadLine()
        {
            return port.ReadLine();
        }

        public void ClosePort()
        {
            try
            {
                port.DataReceived -= SerialPort_DataReceived;
                if (port.IsOpen) port.Close();
            }
            catch { }
            try
            {
                wssv?.Stop();
            }
            catch { }
        }

        private void Broadcast(string message)
        {
            try
            {
                wssv?.WebSocketServices["/" + port.PortName].Sessions.Broadcast(message);
            }
            catch { }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var currentSerialPort = (SerialPort)sender;

            try
            {
                string chunk = currentSerialPort.ReadExisting();
                if (string.IsNullOrEmpty(chunk)) return;

                lock (_sbLock)
                {
                    _sb.Append(chunk);

                    // Líneas terminadas en \n: BNO055 (8 floats) o mensajes no-IMU (p. ej. flexores f,...)
                    while (true)
                    {
                        string text = _sb.ToString();
                        int nl = text.IndexOf('\n');
                        if (nl < 0) break;

                        string line = text.Substring(0, nl).Trim('\r', ' ', '\t');
                        _sb.Remove(0, nl + 1);

                        if (string.IsNullOrEmpty(line))
                            continue;

                        if (_imuMode == ImuParseMode.Bno055
                            && TryParseDualQuaternionLine(line, out float w1, out float x1, out float y1, out float z1, out float w2, out float x2, out float y2, out float z2))
                        {
                            string qMsg = $"Q,{w1.ToString(_inv)},{x1.ToString(_inv)},{y1.ToString(_inv)},{z1.ToString(_inv)},{w2.ToString(_inv)},{x2.ToString(_inv)},{y2.ToString(_inv)},{z2.ToString(_inv)}";
                            Broadcast(qMsg);
                        }
                        else
                        {
                            // Flexores u otros mensajes: reenviar tal cual (no romper modo dual)
                            Broadcast(line);
                        }
                    }

                    // LSM9DS1: frame legacy terminado en "EOF", 12 valores separados por '/'
                    if (_imuMode == ImuParseMode.Lsm9ds1)
                    {
                        while (true)
                        {
                            string text = _sb.ToString();
                            int eof = text.IndexOf("EOF", StringComparison.Ordinal);
                            if (eof < 0) break;

                            string frame = text.Substring(0, eof);
                            _sb.Remove(0, eof + 3);
                            ProcessImuFrame(frame);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Parsea "w1,x1,y1,z1,w2,x2,y2,z2" (8 floats, InvariantCulture).
        /// </summary>
        private bool TryParseDualQuaternionLine(string line, out float w1, out float x1, out float y1, out float z1, out float w2, out float x2, out float y2, out float z2)
        {
            w1 = x1 = y1 = z1 = w2 = x2 = y2 = z2 = 0f;
            string[] parts = line.Split(',');
            if (parts.Length != 8) return false;

            if (!float.TryParse(parts[0], NumberStyles.Float, _inv, out w1)) return false;
            if (!float.TryParse(parts[1], NumberStyles.Float, _inv, out x1)) return false;
            if (!float.TryParse(parts[2], NumberStyles.Float, _inv, out y1)) return false;
            if (!float.TryParse(parts[3], NumberStyles.Float, _inv, out z1)) return false;
            if (!float.TryParse(parts[4], NumberStyles.Float, _inv, out w2)) return false;
            if (!float.TryParse(parts[5], NumberStyles.Float, _inv, out x2)) return false;
            if (!float.TryParse(parts[6], NumberStyles.Float, _inv, out y2)) return false;
            if (!float.TryParse(parts[7], NumberStyles.Float, _inv, out z2)) return false;
            return true;
        }

        private void ProcessImuFrame(string frame)
        {
            var parts = frame.Split('/');
            if (parts.Length != 12) return;

            if (!double.TryParse(parts[0], NumberStyles.Float, _inv, out var axD)) return;
            if (!double.TryParse(parts[1], NumberStyles.Float, _inv, out var ayD)) return;
            if (!double.TryParse(parts[2], NumberStyles.Float, _inv, out var azD)) return;
            if (!double.TryParse(parts[3], NumberStyles.Float, _inv, out var gxD)) return;
            if (!double.TryParse(parts[4], NumberStyles.Float, _inv, out var gyD)) return;
            if (!double.TryParse(parts[5], NumberStyles.Float, _inv, out var gzD)) return;
            if (!double.TryParse(parts[6], NumberStyles.Float, _inv, out var mxD)) return;
            if (!double.TryParse(parts[7], NumberStyles.Float, _inv, out var myD)) return;
            if (!double.TryParse(parts[8], NumberStyles.Float, _inv, out var mzD)) return;

            if (!double.TryParse(parts[9], NumberStyles.Float, _inv, out var qxD)) return;
            if (!double.TryParse(parts[10], NumberStyles.Float, _inv, out var qyD)) return;
            if (!double.TryParse(parts[11], NumberStyles.Float, _inv, out var qzD)) return;

            float ax = (float)axD;
            float ay = (float)ayD;
            float az = (float)azD;
            float gx = (float)gxD;
            float gy = (float)gyD;
            float gz = (float)gzD;
            float mx = (float)mxD;
            float my = (float)myD;
            float mz = (float)mzD;

            float qx = (float)qxD;
            float qy = (float)qyD;
            float qz = (float)qzD;

            imu_ValuesFunction?.Invoke(ax, ay, az, gx, gy, gz, mx, my, mz);

            string zMsg =
                $"z,{ax.ToString(_inv)},{ay.ToString(_inv)},{az.ToString(_inv)}," +
                $"{gx.ToString(_inv)},{gy.ToString(_inv)},{gz.ToString(_inv)}," +
                $"{mx.ToString(_inv)},{my.ToString(_inv)},{mz.ToString(_inv)}";

            Broadcast(zMsg);

            string qMsg =
                $"q,{qx.ToString(_inv)},{qy.ToString(_inv)},{qz.ToString(_inv)}";

            Broadcast(qMsg);
        }
    }
}
