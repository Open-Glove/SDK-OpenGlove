using OpenGlove;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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

        int index;

        public void SaveGlove(Glove glove)
        {
            for(int i = 0; i < Glove.Gloves.Count; i++)
            {
                if (Glove.Gloves[i].BluetoothAddress.Equals(glove.BluetoothAddress))
                {
                    //if (glove.Connected == true && glove.LegacyGlove == null)
                  //  {
                        if(Glove.Gloves[i].LegacyGlove != null)
                        {
                            glove.LegacyGlove = Glove.Gloves[i].LegacyGlove;
                        }
                        else
                        {
                            glove.LegacyGlove = new LegacyOpenGlove();
                        }
                        
                  //  }
                    Glove.Gloves[i] = glove;
                    break;
                }
            }  
        }

        /*
        public void UpdateGlove(Glove glove)
        {
            index = 0;
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(glove.BluetoothAddress))
                {
                    Glove.Gloves[index].GloveConfiguration = glove.GloveConfiguration;
                    //Glove.Gloves[index].
                    // Glove.Gloves.Remove(g);
                    // Glove.Gloves.Add(glove);
                    break;
                }
                index++;
            }

        }
        */
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
                        if (g.GloveConfiguration.PositivePins.Count > 0 && g.GloveConfiguration.NegativePins.Count > 0)
                        {
                            g.LegacyGlove.InitializeMotor(g.GloveConfiguration.PositivePins);
                            g.LegacyGlove.InitializeMotor(g.GloveConfiguration.NegativePins);
                            g.LegacyGlove.ActivateMotor(g.GloveConfiguration.NegativePins, g.GloveConfiguration.NegativeInit);
                        }
                        
                        if(g.GloveConfiguration.GloveProfile == null || (g.GloveConfiguration.GloveProfile!=null && g.GloveConfiguration.GloveProfile.FlexorsMappings.Count == 0) )
                        {
                            g.LegacyGlove.resetFlexors();
                        }
                        g.LegacyGlove.setIMUStatus(0);
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
        
        public int addFlexor(string gloveAddress, int pin, int mapping)
        {
            index = 0;
            foreach (Glove g in Glove.Gloves)
            {
                
                if (g.BluetoothAddress.Equals(gloveAddress))
                {

                    g.LegacyGlove.addFlexor(pin, mapping);
                    return 0;
                }
                index++;
            }
            return 1;

        }

        public int removeFlexor(string gloveAddress, int mapping)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.removeFlexor(mapping);
                    return 0;
                }
            }
            return 1;
            
        }

        public void calibrateFlexors(string gloveAddress)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.calibrateFlexors();
                }
            }
            
        }

        public void confirmCalibration(string gloveAddress)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.confirmCalibration();
                }
            }
            
        }

        public void setThreshold(string gloveAddress, int value)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.setThreshold(value);
                }
            }
            
        }

        public void resetFlexors(string gloveAddress)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.resetFlexors();
                    if(g.GloveConfiguration.GloveProfile != null && g.GloveConfiguration.GloveProfile.FlexorsMappings != null)
                    {
                        g.GloveConfiguration.GloveProfile.FlexorsMappings.Clear();
                    }
                }
            }
        }

        public void setIMUStatus(string gloveAddress, int value)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.setIMUStatus(value);
                }
            }

        }

        public void startIMU(string gloveAddress)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.startIMU();
                }
            }

        }

        public void setRawData(string gloveAddress, int value)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.setRawData(value);
                }
            }

        }

        public void setLoopDelay(string gloveAddress, int value)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.setLoopDelay(value);
                }
            }
        }

        public void setChoosingData(string gloveAddress, int value)
        {
            foreach (Glove g in Glove.Gloves)
            {
                if (g.BluetoothAddress.Equals(gloveAddress))
                {
                    g.LegacyGlove.setChoosingData(value);
                }
            }
        }
    }
}
