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
        }
    }
}
