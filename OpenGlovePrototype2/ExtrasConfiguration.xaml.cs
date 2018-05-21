using OpenGlove_API_C_Sharp_HL.ServiceReference1;
using OpenGlove_API_C_Sharp_HL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Lógica de interacción para ExtrasConfiguration.xaml
    /// </summary>
    public partial class ExtrasConfiguration : Window
    {
        private OpenGloveAPI gloves = OpenGloveAPI.GetInstance();

        private Glove selectedGlove;

        private int maxDelayValue = 200;

        public ExtrasConfiguration(Glove selectedGlove)
        {
            this.selectedGlove = selectedGlove;
            InitializeComponent();
        }

        private void buttonDelay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                gloves.setLoopDelay(this.selectedGlove, (int)delayValue.Value);
            }
            catch
            {

            }
        }

        private void delayValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this.delayValue.Value < 0)
            {
                this.delayValue.Value = 0;
            }
            if(this.delayValue.Value > maxDelayValue)
            {
                this.delayValue.Value = maxDelayValue;
            }
        }
    }
}
