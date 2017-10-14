using OpenGlove;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
//using System.Runtime.Remoting.Channels;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace OpenGloveWCF
{
    public class OGService : IOGService
    {
        private const bool DEBUGGING = false;

        private const int AREACOUNT = 58;

        private webSocketsService openGloveWS = new webSocketsService();

        private BackgroundWorker bgw;

        public int startWSService()
        {
            try
            {
                openGloveWS.addEndPoint();
                openGloveWS.startWS();
                return 0;
            }
            catch
            {
                return 1; // error
            }
            
        }

        public int startBroadcasting(Glove glove) {
            if(glove.Connected == true)
            {
                if (glove.Side == Sides.Right)
                {
                    try
                    {
                        setGloveWS(glove.Side);
                        openGloveWS.startBroadcast(glove.Side);
                        return 0;
                    }
                    catch
                    {
                        return 1;
                    }
                }
                else
                {
                    try
                    {
                        setGloveWS(glove.Side);
                        openGloveWS.startBroadcast(glove.Side);
                        return 0;
                    }
                    catch
                    {
                        return 1;
                    }
                }
            }else
            {
                return 1;
            }
        }

        public int stopBroadcasting(Glove glove)
        {
            openGloveWS.stopBroadcast(glove.Side);
            return 0;
        }

        public void setGloveWS(Sides side)
        {
            if(side == Sides.Right)
            {
                openGloveWS.setRightGloveWS(Glove.getRightlove());
            }else
            {
                openGloveWS.setLeftGloveWS(Glove.getLeftGlove());
            }
        }

        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            Glove g = (Glove)(((List<object>)e.Argument)[0]);
            IEnumerable<int> actuator = (IEnumerable<int>)(((List<object>)e.Argument)[1]);
            IEnumerable<string> intensity = (IEnumerable<string>)(((List<object>)e.Argument)[2]);
            //Your time taking work. Here it's your data query method.
            
            g.LegacyGlove.ActivateMotor(actuator, intensity);
        }

        void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //After completing the job.
        }


        public int Activate(string gloveAddress, int actuator, int intensity)
        {
            if (intensity < 0)
            {
                intensity = 0;
            }
            else if (intensity > 255)
            {
                intensity = 255;
            }

            if (actuator < 0)
            {
                return 1;
            }
            else if (actuator >= AREACOUNT)
            {
                return 1;
            }

            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    if (g.Connected)
                    {
                        try
                        {
                            bgw = new BackgroundWorker();
                            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgw_RunWorkerCompleted);
                            bgw.DoWork += new DoWorkEventHandler(bgw_DoWork);
                            bgw.RunWorkerAsync(new List<object>() { g, new List<int> { actuator }, new List<string> { intensity.ToString() } });
                            return 0;
                        }
                        catch (Exception)
                        {
                            g.Connected = false;
                            g.LegacyGlove = new LegacyOpenGlove();
                            return 1;// CANT ACTIVATE
                        }
                    }
                }
            }
            return 0; //OK
        }
        

        public int ActivateMany(string gloveAddress, List<int> actuators, List<int> intensityList)
        {
            if (actuators == null || intensityList == null) {
                return 0;
            }
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    if (g.Connected)
                    {
                        try
                        {
                            bgw = new BackgroundWorker();
                            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgw_RunWorkerCompleted);
                            bgw.DoWork += new DoWorkEventHandler(bgw_DoWork);
                            bgw.RunWorkerAsync(new List<object>() { g, actuators, intensityList.ConvertAll<string>(delegate (int i) { return i.ToString(); }) });
                            return 0;
                        }
                        catch (Exception)
                        {
                            g.Connected = false;
                            g.LegacyGlove = new LegacyOpenGlove();
                            return 1;// CANT ACTIVATE
                        }
                    }
                }
            }
            return 0; //OK
        }
        
        public List<Glove> GetGloves()
        {
            return Glove.Gloves;
        }

        public List<Glove> RefreshGloves()
        {
            return Glove.RefreshGloves();
        }

        public void SaveGlove(Glove glove)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(glove.BluetoothAddress))
                {
                    Glove.Gloves.Remove(g);
                    Glove.Gloves.Add(glove);
                    break;
                }
            }
        }

        public int Connect(string gloveAddres)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddres))
                {
                    if (g.GloveConfiguration != null)
                    {
                        Glove actualGlove;
                        if (g.Side==Sides.Right)
                        {
                            actualGlove = Glove.getRightlove();
                            if (actualGlove != null && actualGlove.Connected == true)
                            {
                                return 2;// GLOVE ON USE
                            }else
                            {
                                g.LegacyGlove = new LegacyOpenGlove();
                                g.LegacyGlove.OpenPort(g.Port, g.GloveConfiguration.BaudRate);
                                g.LegacyGlove.InitializeMotor(g.GloveConfiguration.PositivePins);
                                g.LegacyGlove.InitializeMotor(g.GloveConfiguration.NegativePins);
                                g.LegacyGlove.PinMode(g.GloveConfiguration.FlexPins, g.GloveConfiguration.FlexInit);
                                g.LegacyGlove.ActivateMotor(g.GloveConfiguration.NegativePins, g.GloveConfiguration.NegativeInit);
                                g.Connected = true;
                                Glove.setRightGlove(g.BluetoothAddress);

                            }
                        }else
                        {
                            actualGlove = Glove.getLeftGlove();
                            if (actualGlove!= null && actualGlove.Connected == true)
                            {
                                return 2;// GLOVE ON USE
                            }
                            else
                            {
                                g.LegacyGlove = new LegacyOpenGlove();
                                g.LegacyGlove.OpenPort(g.Port, g.GloveConfiguration.BaudRate);
                                g.LegacyGlove.InitializeMotor(g.GloveConfiguration.PositivePins);
                                g.LegacyGlove.InitializeMotor(g.GloveConfiguration.NegativePins);
                                g.LegacyGlove.PinMode(g.GloveConfiguration.FlexPins, g.GloveConfiguration.FlexInit);
                                g.LegacyGlove.ActivateMotor(g.GloveConfiguration.NegativePins, g.GloveConfiguration.NegativeInit);
                                g.Connected = true;
                                Glove.setLeftGlove(g.BluetoothAddress);
                            }
                        }
                    }
                    else
                    {
                        return 1; // NO CONFIG
                    }
                    return 0;
                }
            }
            return 0; //OK
        }

        public int Disconnect(string gloveAddres)
        {
            if (Glove.getRightlove().BluetoothAddress.Equals(gloveAddres))
            {
                try
                {
                    Glove.getRightlove().LegacyGlove.ClosePort();
                }
                catch (Exception)
                {

                }
                Glove.getRightlove().Connected = false;
                return 0;
            }
            if (Glove.getLeftGlove().BluetoothAddress.Equals(gloveAddres))
            {
                try
                {
                    Glove.getLeftGlove().LegacyGlove.ClosePort();
                }
                catch (Exception)
                {

                }
                Glove.getLeftGlove().Connected = false;
                return 0;
            }
            return 0;
           
        }
    }
}
