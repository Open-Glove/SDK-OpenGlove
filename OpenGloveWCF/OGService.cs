using OpenGlove;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace OpenGloveWCF
{
    public class OGService : IOGService
    {
        private const bool DEBUGGING = false;

        private const int AREACOUNT = 58;

        private BackgroundWorker bgw;

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
                        g.LegacyGlove = new LegacyOpenGlove();
                        g.LegacyGlove.OpenPort(g.Port, g.GloveConfiguration.BaudRate);
                        g.LegacyGlove.InitializeMotor(g.GloveConfiguration.PositivePins);
                        g.LegacyGlove.InitializeMotor(g.GloveConfiguration.NegativePins);
                        g.LegacyGlove.ActivateMotor(g.GloveConfiguration.NegativePins, g.GloveConfiguration.NegativeInit);
                        g.Connected = true;
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
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddres))
                {
                    try
                    {
                        g.LegacyGlove.ClosePort();
                    }
                    catch (Exception)
                    {

                    }
                    g.Connected = false;
                    return 0;
                }
            }
            return 0;
        }
    }
}
