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
using System.Threading;

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Lógica de interacción para FlexorsConfiguration.xaml
    /// </summary>
    public partial class FlexorsConfiguration : Window
    {

        public class Mapping
        {
            public String Flexor { get; set; }
            public String Region { get; set; }
        }

        private List<ComboBox> selectors;

        private List<ProgressBar> progressBars;

        private IEnumerable<int> flexors;

        private OpenGloveAPI gloves = OpenGloveAPI.GetInstance();

        private Glove selectedGlove;

        private ConfigManager configManager;

        public FlexorsConfiguration(Glove selectedGlove)
        {
            InitializeComponent();

            configManager = new ConfigManager();

            this.selectedGlove = selectedGlove;
            this.initializeSelectors();
            if (this.selectedGlove.GloveConfiguration.GloveProfile == null)
            {
                this.selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();
                this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings = new Dictionary<int, int>();
                foreach (ComboBox selector in this.selectors)
                {
                    selector.Items.Add("");

                    foreach (var item in selectedGlove.GloveConfiguration.FlexPins)
                    {
                        selector.Items.Add(item);
                    }
                }

            }
            else
            {

                this.updateView();
                foreach (ComboBox selector in this.selectors)
                {
                    selector.SelectionChanged -= new SelectionChangedEventHandler(selectorsSelectionChanged);
                }
            }

            // Flip controls based on hand side.
            if (this.selectedGlove.Side == Side.Left)
            {
                flipControls();
            }
        }

        /// <summary>
        /// Flips the actuator selectors to match the hand side (left). Dont use if the hand is right
        /// </summary>
        private void flipControls()
        {
            ScaleTransform st = new ScaleTransform();
            st.ScaleX = -1;
            st.ScaleY = 1;

            ScaleTransform stD = new ScaleTransform();
            stD.ScaleX = 1;
            stD.ScaleY = 1;

            imageDorsal.RenderTransform = st;
            selectorsGridDorso.RenderTransform = stD;

            List<Grid> grids = selectorsGridDorso.Children.OfType<Grid>().ToList();

            foreach (var grid in grids)
            {
                foreach (var label in grid.Children.OfType<Label>())
                {
                    label.RenderTransform = stD;
                }
            }
        }

        /// <summary>
        /// Instantiates and populates all view's comboboxes with the available amount of actuators.
        /// </summary>
        private void initializeSelectors()
        {
            selectors = new List<ComboBox>();

            foreach (var region in selectorsGridDorso.Children.OfType<Grid>())
            {
                selectors.Add(region.Children.OfType<ComboBox>().First());
            }

            flexors = new List<int>();
        }

        /// <summary>
        /// Erases an actuator from the selectors (ComboBox) that doesn't own it.
        /// </summary>
        /// <param name="flexor"></param>
        /// <param name="owner"></param>
        private void removeFlexor(int flexor, object owner) 
        {
            foreach (ComboBox selector in this.selectors)
            {
                if (((ComboBox)owner) != selector)
                {
                    selector.Items.Remove(flexor);
                }
            }
        }

        /// <summary>
        /// Repopulates an flexor on all selectors (ComboBox) except for it's owner.
        /// </summary>
        /// <param name="liberatedFlexor"></param>
        /// <param name="preowner"></param>
        private void liberateFlexor(int liberatedFlexor, Object preowner)
        {
            foreach (ComboBox selector in this.selectors)
            {
                if (!selector.Equals(preowner))
                {
                    selector.Items.Add(liberatedFlexor);
                }
            }
        }

        /// <summary>
        /// Resets all selectors to it's initial selection (index 0) and repopulates with available actuators.
        /// </summary>
        private void resetSelectors()
        {

            foreach (ComboBox selector in this.selectors)
            {
                selector.SelectionChanged -= new SelectionChangedEventHandler(selectorsSelectionChanged);
                selector.SelectedIndex = 0;
            }

            foreach (ComboBox selector in selectors)
            {
                selector.Items.Clear();
                selector.Items.Add("");
            }

            flexors = selectedGlove.GloveConfiguration.FlexPins;

            foreach (int flexor in flexors)
            {
                foreach (ComboBox selector in selectors)
                {
                    selector.Items.Add(flexor);
                }
            }

            foreach (ComboBox selector in selectors)
            {
                selector.SelectionChanged += new SelectionChangedEventHandler(selectorsSelectionChanged);
            }
        }

        /// <summary>
        /// Takes a dictionary and refreshes a list based on the changes made to the dictionary using
        /// therminology of hand regions.
        /// DEV: Should go to backend.
        /// </summary>
        /// <param name="mappings"></param>
        private void refreshMappingsList(Dictionary<int, int> mappings)
        {
            this.mappingsList.Items.Clear();
            foreach (KeyValuePair<int, int> mapping in mappings.ToList())
            {
                //Console.WriteLine("MAPPING: "+ mapping.Key + ", " + mapping.Value);
                this.mappingsList.Items.Add(new Mapping() { Flexor = mapping.Value.ToString(), Region = mapping.Key.ToString() });
            }
        }

        /// <summary>
        /// Handles the action of saving a profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveConfigurationDialog = new SaveFileDialog();
            saveConfigurationDialog.Filter = "XML-File | *.xml";
            saveConfigurationDialog.Title = "Save your configuration file";
            saveConfigurationDialog.ShowDialog();

            if (saveConfigurationDialog.FileName != "")
            {
                Console.WriteLine(saveConfigurationDialog.FileName);
                configManager.saveGloveFlexProfile(saveConfigurationDialog.FileName, selectedGlove);
                this.statusBarItemProfile.Content = saveConfigurationDialog.FileName;

                string message = "File saved.";
                string caption = "Save";
                MessageBoxButton button = MessageBoxButton.OK;

                MessageBox.Show(message, caption, button, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Sets the selectors and MappingsList to an updated state, meaningful for the user.
        /// </summary>
        private void updateView()
        {

            foreach (ComboBox selector in selectors)
            {
                selector.SelectionChanged -= new SelectionChangedEventHandler(selectorsSelectionChanged);
            }

            if (this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings != null)
            {
                //Actualizar vista
                this.refreshMappingsList(this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings);
                this.resetSelectors();

                foreach (KeyValuePair<int, int> mapping in this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings.ToList())
                {
                    this.selectors[mapping.Key].SelectedItem = mapping.Value;
                    this.removeFlexor(mapping.Value, this.selectors[mapping.Key]);
                }
                this.statusBarItemProfile.Content = this.selectedGlove.GloveConfiguration.GloveProfile.ProfileName;
            }
            else
            {

                string message = "File not found.";
                string caption = "File not found";
                MessageBoxButton button = MessageBoxButton.OK;

                MessageBox.Show(message, caption, button, MessageBoxImage.Error);

            }

            foreach (ComboBox selector in selectors)
            {
                selector.SelectionChanged += new SelectionChangedEventHandler(selectorsSelectionChanged);
            }

        }

        /// <summary>
        /// Handles the event of selectors (combobox in this case) changing their selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selectorsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (((ComboBox)sender).SelectedItem != null)
            {
                String selection = ((ComboBox)sender).SelectedItem.ToString();
                //String region = (String)((ComboBox)sender).AccessibleName;

                //Si el selector usado poseia otro actuador con anterioridad, este debe ser liberado y añadido a los otros selectores, en general, a todos.

                // Si se selecciona un actuador, la idea es que no se pueda volver a seleccionar en otro punto de la mano.

                int owner = ((ComboBox)sender).TabIndex;
                if (selection != null)
                { 
                    if (!selection.Equals(""))
                    {
                        int selectionFlexor = Int32.Parse(selection);
                        removeFlexor(selectionFlexor, sender);
                        try
                        {
                            this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings.Add(owner, selectionFlexor);
                        }
                        catch (Exception)
                        {
                            int liberatedFlexor = this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings[owner];
                            liberateFlexor(liberatedFlexor, sender);
                            this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings[owner] = selectionFlexor;
                        }
                    }
                    else
                    {
                        int liberatedFlexorC;
                        this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings.TryGetValue(owner, out liberatedFlexorC);
                        int? liberatedFlexor = liberatedFlexorC;
                        if (liberatedFlexor != null)
                        {
                            liberatedFlexorC = liberatedFlexor == null ? default(int) : liberatedFlexor.GetValueOrDefault();
                            liberateFlexor(liberatedFlexorC, sender);
                            this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings.Remove(owner);
                        }
                    }
                }
                refreshMappingsList(this.selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings);
                ((ComboBox)sender).Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Handles the event of mouse entering a region. Highlights the sender region.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Rectangle_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Rectangle)sender).Stroke = System.Windows.SystemColors.MenuHighlightBrush;
        }

        /// <summary>
        /// Handles the event of mouse leaving a region. Dimms the sender region.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Rectangle_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Rectangle)sender).Stroke = SystemColors.ControlBrush;
        }

        private void region1_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //Frame selectorFrame = new Frame();
            Grid region = (Grid)((Rectangle)sender).Parent;
            ComboBox selector = region.Children.OfType<ComboBox>().First();

            selector.Visibility = Visibility.Visible;
            selector.IsDropDownOpen = true;
        }

        private void selectorClosed(object sender, EventArgs e)
        {
            ((ComboBox)sender).Visibility = Visibility.Hidden;
        }

        private void mappingsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private bool testing;

        Stopwatch sw = new Stopwatch();

        private void buttonTestFlexors_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedGlove.Connected)
            {
                string message = "Cannot test flexors. Please establish connection with the device.";
                string caption = "Glove disconnected";
                MessageBoxButton button = MessageBoxButton.OK;

                MessageBox.Show(message, caption, button, MessageBoxImage.Error);
                return;
            }
            sw.Restart();

            /*  Mapping mapping = (Mapping)this.mappingsList.SelectedItem;
              if (testing)
              {
                  this.gloves.Activate(selectedGlove, Int32.Parse(mapping.Region), 0);
                  testing = false;
                  buttonTestIntensity.Content = "Test";
                  this.mappingsList.IsEnabled = true;

              }
              else if (this.mappingsList.SelectedItem != null)
              {
                  this.gloves.Activate(selectedGlove, Int32.Parse(mapping.Region), ((int)this.intensityUpDown.Value));

                  sw.Stop();

                  Console.WriteLine("Elapsed={0}", sw.Elapsed);

                  testing = true;
                  buttonTestIntensity.Content = "Stop";
                  this.mappingsList.IsEnabled = false;

              }
              */
            if (testing)
            {
                testing = false;
                buttonTestFlexors.Content = "Test";
                tabControl.SelectedIndex = 0;

            }
            else if (this.mappingsList.Items.Count > 0)
            {
                testing = true;
                tabControl.SelectedIndex = 1;
                buttonTestFlexors.Content = "Stop";
            }
        }

        private void changeBarValue(int index, int value)
        {
            switch (index)
            {
                case 0:
                    this.progressBar0.Value = value;
                    break;
                case 1:
                    this.progressBar1.Value = value;
                    break;
                case 2:
                    progressBar2.Value = value;
                    break;
                case 3:
                    progressBar3.Value = value;
                    break;
                case 4:
                    progressBar4.Value = value;
                    break;
                case 5:
                    progressBar5.Value = value;
                    break;
                case 6:
                    progressBar6.Value = value;
                    break;
                case 7:
                    progressBar7.Value = value;
                    break;
                case 8:
                    progressBar8.Value = value;
                    break;
                case 9:
                    progressBar9.Value = value;
                    break;
            }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void tabControl_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }
    }

}
