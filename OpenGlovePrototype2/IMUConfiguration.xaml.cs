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

        public IMUConfiguration(Glove selectedGlove)
        {
            this.selectedGlove = selectedGlove;
            InitializeComponent();
            GridTest.Visibility = Visibility.Hidden;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            gloves.startIMU(selectedGlove);
            labelIMUStatus.Content = "On";
        }

        private bool testing;
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
            else if (this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings.Count > 0)
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

    }
}
