using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace OpenGlove
{
    /// <summary>
    /// Represents  a communication instance between the API and the glove. 
    /// Provide methods for send and receive data through serial port
    /// </summary>
    class Communication
    {
        /// <summary>
        /// Serial port communication field. 
        /// </summary>
        private static SerialPort port = new SerialPort();

        private static int websocketBase = 9870;
        private static int portNumber;
        private static string WSAddress;
        private static WebSocketServer wssv;
        //static Thread readThread;
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


        /// <summary>
        /// Initialize an instance of Communication class without open the communication with the device.
        /// </summary>
        public Communication()
        {
        }
        /// <summary>
        /// Initialize an instance of Communication class, opening the communication using the specified port and baud rate.
        /// </summary>
        /// <param name="portName">Name of the serial port to open a communication </param>
        /// <param name="baudRate">Data rate in bits per second. Use one of these values: 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, or 115200</param>
        public Communication(string portName, int baudRate)  
        {
            port.PortName = portName;
            port.BaudRate = baudRate;
            port.Open();
        }
        /// <summary>
        /// Returns an array with all active serial ports names
        /// </summary>
        /// <returns>An array with the names of all active serial ports</returns>

        public string[] GetPortNames() {

            return SerialPort.GetPortNames();
        }
        /// <summary>
        /// Open a new connection with the specified port and baud rate
        /// </summary>
        ///<param name = "portName" >Name of the serial port to open a communication</param>
        /// <param name="baudRate">Data rate in bits per second. Use one of these values: 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, or 115200</param>
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
            port.Open();
           // readThread = new Thread(Read);
          //  readThread.Start();
        }
        /// <summary>
        /// Send the string to the serial port
        /// </summary>
        /// <param name="data">String data to send</param>
        public void Write(string data)
        {
            port.Write(data);
        }
        /// <summary>
        /// Read the input buffet until a next line character
        /// </summary>
        /// <returns>A string without the next line character</returns>
        public string ReadLine()
        {
            return port.ReadLine();
        }
        /// <summary>
        /// Close the serial communication
        /// </summary>
        public void ClosePort()
        {
            port.Close();
           // readThread.Abort();
            wssv.Stop();

        }

        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Obtenemos el puerto serie que lanza el evento
            SerialPort currentSerialPort = (SerialPort)sender;

            // Leemos el dato recibido del puerto serie
            try
            {
                string inData = currentSerialPort.ReadLine();
            
                wssv.WebSocketServices["/" + port.PortName].Sessions.Broadcast(inData);
            }
            catch
            {

            }
            
        }
        /*
        public static void Read()
        {
            while (true)
            {
                try
                {
                    string message = port.ReadLine();
                    wssv.WebSocketServices["/"+port.PortName].Sessions.Broadcast(message);
                }
                catch (Exception)
                {
                }
            }
        }
        */

    }
}
