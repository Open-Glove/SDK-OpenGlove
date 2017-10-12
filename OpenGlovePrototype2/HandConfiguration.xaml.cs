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


using Microsoft.Win32;
using OpenGlovePrototype2;
using System.Diagnostics;
using OpenGlove_API_C_Sharp_HL;
using OpenGlove_API_C_Sharp_HL.ServiceReference1;


namespace OpenGlovePrototype2
{
    /// <summary>
    /// Lógica de interacción para HandConfiguration.xaml
    /// </summary>
    /// 

    public partial class HandConfiguration : Window
    {

        public class Mapping
        {
            public String Actuator { get; set; }
            public String Region { get; set; }
        }

        public class FlexorsMapping
        {
            public String FlexSensor { get; set; }
            public String Region { get; set; }
        }

        private ConfigManager configManager;

        private Glove selectedGlove;

        private OpenGloveAPI gloves = OpenGloveAPI.GetInstance();

        public HandConfiguration(Glove selectedGlove)
        {
          
            InitializeComponent();
            configManager = new ConfigManager();
            this.selectedGlove = selectedGlove;

            if (this.selectedGlove.GloveConfiguration.GloveProfile == null)
            {
                this.selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();
                this.selectedGlove.GloveConfiguration.GloveProfile.Mappings = new Dictionary<string, string>();
                this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings = new Dictionary<int, int>();
                this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappingsValues = new Dictionary<int, int>();
            }

        }

        private void buttonVibeBoards_Click(object sender, RoutedEventArgs e)
        {
            VibeBoardsConfiguration vibeConfig = new VibeBoardsConfiguration(this.selectedGlove);
            vibeConfig.ShowDialog();
            if (this.selectedGlove.GloveConfiguration.GloveProfile.Mappings != null)
            {
                this.numbersActuators.Content = this.selectedGlove.GloveConfiguration.GloveProfile.Mappings.Count;
            }
            
            //refreshControls();
        }

        private void buttonFlexors_Click(object sender, RoutedEventArgs e)
        {
            FlexorsConfiguration FC = new FlexorsConfiguration(this.selectedGlove);
            FC.ShowDialog();
            if (this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings != null)
            {
                this.numberFlexors.Content = this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings.Count;
            }
            
        }

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will close the current flex profile. Are you sure?", "New flexors configuration confirmation", MessageBoxButton.YesNo);

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                OpenFileDialog openConfigurationDialog = new OpenFileDialog();
                openConfigurationDialog.Filter = "XML-File | *.xml";
                openConfigurationDialog.Title = "Open a glove vibe boards profile file";
                openConfigurationDialog.ShowDialog();

                if (openConfigurationDialog.FileName != null)
                {
                    if (openConfigurationDialog.FileName != "")
                    {
                        configManager.OpenProfileConfiguration(openConfigurationDialog.FileName, selectedGlove);

                        if (this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings != null)
                        {
                            this.numberFlexors.Content = this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings.Count;
                        }

                        if (this.selectedGlove.GloveConfiguration.GloveProfile.Mappings != null)
                        {
                            this.numbersActuators.Content = this.selectedGlove.GloveConfiguration.GloveProfile.Mappings.Count;
                        }
                    }
                }
            }

        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveConfigurationDialog = new SaveFileDialog();
            saveConfigurationDialog.Filter = "XML-File | *.xml";
            saveConfigurationDialog.Title = "Save your configuration file";
            saveConfigurationDialog.ShowDialog();

            if (saveConfigurationDialog.FileName != "")
            {
                Console.WriteLine(saveConfigurationDialog.FileName);
                configManager.saveGloveProfile(saveConfigurationDialog.FileName, selectedGlove);
                //this.statusBarItemProfile.Content = saveConfigurationDialog.FileName;

                string message = "File saved.";
                string caption = "Save";
                MessageBoxButton button = MessageBoxButton.OK;

                MessageBox.Show(message, caption, button, MessageBoxImage.Information);

            }

        }
    }
}
