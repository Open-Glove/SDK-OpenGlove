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

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Lógica de interacción para IMUConfiguration.xaml
    /// </summary>
    public partial class IMUConfiguration : Window
    {
        private OpenGloveAPI gloves = OpenGloveAPI.GetInstance();

        private Glove selectedGlove;

        private bool testing;
        public IMUConfiguration(Glove selectedGlove)
        {
            this.selectedGlove = selectedGlove;
            InitializeComponent();
            if (this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings == null)
            {
                this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings = new Glove.Configuration.Profile.IMU_Settings();
                this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.imuStatus = false;
                this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.rawData = false;
                this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.calibrationStatus = false;
                this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.nameModel = "Default";
                labelIMUStatus.Content = "Off";

            }
            updateView();
            testing = false;
            GridTest.Visibility = Visibility.Hidden;
        }

        private void updateView()
        {

            if(this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.imuStatus == true)
            {
                button.Content = "Deactivate data";
                labelIMUStatus.Content = "On";
            }else
            {
                button.Content = "Activate data";
                labelIMUStatus.Content = "Off";
            }

            if (this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.rawData == true)
            {
                buttonSetRawData.Content = "Processed data";
            }
            else
            {
                buttonSetRawData.Content = "Raw Data";
            }

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.imuStatus == false)
            {
                gloves.startIMU(selectedGlove);
                this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.imuStatus = true;
            }else
            {
                gloves.setIMUStatus(selectedGlove, false);
                this.selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.imuStatus = false;
            }
            
            updateView();
        }

        

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            if (testing)
            {
                testing = false;
                buttonTest.Content = "Test";
                gloves.imu_ValuesFunction -= allIMUValues;
                gloves.stopCaptureData();
                GridTest.Visibility = Visibility.Hidden;

            }
            else
            {
                testing = true;
                GridTest.Visibility = Visibility.Visible;
                gloves.startCaptureData(selectedGlove);
                gloves.imu_ValuesFunction += allIMUValues;
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
            if (selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.rawData == true)
            {
                gloves.setRawData(selectedGlove, false);
                selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.rawData = false;
            }
            else
            {
                gloves.setRawData(selectedGlove, true);
                selectedGlove.GloveConfiguration.GloveProfile.IMUSettings.rawData = true;
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
