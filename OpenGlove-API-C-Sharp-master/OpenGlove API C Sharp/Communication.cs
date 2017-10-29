using System.IO.Ports;
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
        private static string WSAddress = "ws://localhost:9876/";
        private static WebSocketServer wssv = new WebSocketServer(WSAddress);

        public class WSbase
        {

            public class FlexorsEndPoint : WebSocketBehavior
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
        /// Serial port communication field. 
        /// </summary>
        private SerialPort port = new SerialPort();

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
            this.port.PortName = portName;
            this.port.BaudRate = baudRate;
            this.port.Open();
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
            this.port.PortName = portName;
            this.port.BaudRate = baudRate;
            this.port.Open();
            wssv.AddWebSocketService<WSbase.FlexorsEndPoint>(this.port.PortName+"/flexors");
            wssv.Start();
        }
        /// <summary>
        /// Send the string to the serial port
        /// </summary>
        /// <param name="data">String data to send</param>
        public void Write(string data)
        {
            this.port.Write(data);
        }
        /// <summary>
        /// Read the input buffet until a next line character
        /// </summary>
        /// <returns>A string without the next line character</returns>
        public string ReadLine()
        {
            return this.port.ReadLine();
        }
        /// <summary>
        /// Close the serial communication
        /// </summary>
        public void ClosePort()
        {
            this.port.Close();
            wssv.Stop();

        }

       


    }
}
