using Microsoft.Win32;
using OpenGlovePrototype2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;
using OpenGlove_API_C_Sharp_HL;
using OpenGlove_API_C_Sharp_HL.ServiceReference1;
using System.ServiceModel;

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Lógica de interacción para IMUConfiguration.xaml
    /// </summary>
    public partial class IMUConfiguration : Window
    {
        private OGServiceClient serviceClient;
        

        private OpenGloveAPI gloves = OpenGloveAPI.GetInstance();

        private Glove selectedGlove;

        private bool testing;
        public IMUConfiguration(Glove selectedGlove)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            EndpointAddress address = new EndpointAddress("http://localhost:8733/Design_Time_Addresses/OpenGloveWCF/OGService/");
            serviceClient = new OGServiceClient(binding, address);

            this.selectedGlove = selectedGlove;
            InitializeComponent();
            if (this.selectedGlove.GloveConfiguration.GloveProfile == null)
            {
                this.selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();

                this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus = false;
                this.selectedGlove.GloveConfiguration.GloveProfile.rawData = false;
                this.selectedGlove.GloveConfiguration.GloveProfile.imuCalibrationStatus = false;
                this.selectedGlove.GloveConfiguration.GloveProfile.imuModel = "Default";
                labelIMUStatus.Content = "Off";
            }else
            {
                if(this.selectedGlove.GloveConfiguration.GloveProfile.imuModel == null || this.selectedGlove.GloveConfiguration.GloveProfile.imuModel.Equals(""))
                {
                    this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus = false;
                    this.selectedGlove.GloveConfiguration.GloveProfile.rawData = false;
                    this.selectedGlove.GloveConfiguration.GloveProfile.imuCalibrationStatus = false;
                    this.selectedGlove.GloveConfiguration.GloveProfile.imuModel = "Default";
                    labelIMUStatus.Content = "Off";
                }
            }
            updateView();
            testing = false;
            GridTest.Visibility = Visibility.Hidden;
        }

        private void updateView()
        {

            if(this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus == true)
            {
                button.Content = "Deactivate data";
                labelIMUStatus.Content = "On";
            }else
            {
                button.Content = "Activate data";
                labelIMUStatus.Content = "Off";
            }

            if (this.selectedGlove.GloveConfiguration.GloveProfile.rawData == true)
            {
                buttonSetRawData.Content = "Processed data";
            }
            else
            {
                buttonSetRawData.Content = "Raw Data";
            }
            serviceClient.SaveGlove(this.selectedGlove);

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus == false)
            {
                gloves.startIMU(selectedGlove);
                this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus = true;
            }else
            {
                gloves.setIMUStatus(selectedGlove, false);
                this.selectedGlove.GloveConfiguration.GloveProfile.imuStatus = false;
            }
            
            updateView();
        }

        

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            if (testing)
            {
                testing = false;
                buttonTest.Content = "Test";
                gloves.getDataReceiver(selectedGlove).imu_ValuesFunction -= allIMUValues;
                GridTest.Visibility = Visibility.Hidden;

            }
            else
            {
                testing = true;
                GridTest.Visibility = Visibility.Visible;
                gloves.getDataReceiver(selectedGlove).imu_ValuesFunction += allIMUValues;
                buttonTest.Content = "Stop";
            }
        }

        public void allIMUValues(float ax, float ay, float az, float gx, float gy, float gz, float mx, float my, float mz)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.labelAx.Content = ax.ToString();
                this.labelAy.Content = ay.ToString();
                this.labelAz.Content = az.ToString();
                this.labelGx.Content = gx.ToString();
                this.labelGy.Content = gy.ToString();
                this.labelGz.Content = gz.ToString();
                this.labelMx.Content = mx.ToString();
                this.labelMy.Content = my.ToString();
                this.labelMz.Content = mz.ToString();
            }));
        }
   
        private void buttonSetRawData_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGlove.GloveConfiguration.GloveProfile.rawData == true)
            {
                gloves.setRawData(selectedGlove, false);
                selectedGlove.GloveConfiguration.GloveProfile.rawData = false;
            }
            else
            {
                gloves.setRawData(selectedGlove, true);
                selectedGlove.GloveConfiguration.GloveProfile.rawData = true;
            }
            updateView();            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /*
            if(testing == true)
            {
                buttonTest.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            */
        }
    }
}
