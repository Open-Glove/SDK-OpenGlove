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
    /// Interaction logic for AddBoard.xaml
    /// </summary>
    public partial class AddBoard : Window
    {
        public string BoardName { get; set; } = null;

        public int Pins { get; set; } = 0;

        public int digitalPins { get; set; } = 0;

        public int analogPins { get; set; } = 0;

        public int firstAnalogPin { get; set; } = 0;

        public AddBoard()
        {

            InitializeComponent();
            
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if( this.digitalSelector.Value != null && this.analogSelector.Value != null && this.firstSelector.Value != null && ((int) this.digitalSelector.Value < (int) this.firstSelector.Value ) )
            {
                BoardName = this.textBoxBoardName.Text;
                digitalPins = (int)this.digitalSelector.Value;
                analogPins = (int)this.analogSelector.Value;
                firstAnalogPin = (int)this.firstSelector.Value;
                MessageBoxResult messageBoxResult = MessageBox.Show("Board has been saved", "Success", MessageBoxButton.OK);
                this.Close();
            }
            else
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Please verify your values, first Analog pin can't be less than Digital pins ", "Verification", MessageBoxButton.OK);
            }
        }
    }
}
