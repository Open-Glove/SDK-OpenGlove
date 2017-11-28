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
    /// Lógica de interacción para VibeBoardsConfiguration.xaml
    /// </summary>
    public partial class VibeBoardsConfiguration : Window
    {
        public class Mapping
        {
            public String Actuator { get; set; }
            public String Region { get; set; }
        }

        private List<ComboBox> selectors;

        private IEnumerable<int> actuators;

        private OpenGloveAPI gloves = OpenGloveAPI.GetInstance();

        private Glove selectedGlove;

        private ConfigManager configManager;

        public VibeBoardsConfiguration(Glove selectedGlove)
        {
            InitializeComponent();

            configManager = new ConfigManager();

            this.selectedGlove = selectedGlove;

            this.initializeSelectors();
            if (this.selectedGlove.GloveConfiguration.GloveProfile == null)
            {
                this.selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();
                this.selectedGlove.GloveConfiguration.GloveProfile.Mappings = new Dictionary<string, string>();
                foreach (ComboBox selector in this.selectors)
                {
                    selector.Items.Add("");

                    foreach (var item in selectedGlove.GloveConfiguration.PositivePins)
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
            imagePalmar.RenderTransform = st;
            selectorsGrid.RenderTransform = st;

            foreach (var item in selectors)
            {
                item.RenderTransform = st;
            }

            List<Grid> grids = selectorsGrid.Children.OfType<Grid>().ToList();

            foreach (var grid in grids)
            {
                foreach (var label in grid.Children.OfType<Label>())
                {
                    label.RenderTransform = st;
                }
            }

            ScaleTransform stD = new ScaleTransform();
            stD.ScaleX = 1;
            stD.ScaleY = 1;

            imageDorsal.RenderTransform = st;
            selectorsGridDorso.RenderTransform = stD;

            grids = selectorsGridDorso.Children.OfType<Grid>().ToList();

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

            foreach (var region in selectorsGrid.Children.OfType<Grid>())
            {
                selectors.Add(region.Children.OfType<ComboBox>().First());
            }

            foreach (var region in selectorsGridDorso.Children.OfType<Grid>())
            {
                selectors.Add(region.Children.OfType<ComboBox>().First());
            }

            actuators = new List<int>();
        }

        /// <summary>
        /// Erases an actuator from the selectors (ComboBox) that doesn't own it.
        /// </summary>
        /// <param name="actuator"></param>
        /// <param name="owner"></param>
        private void removeActuator(String actuator, object owner)
        {
            foreach (ComboBox selector in this.selectors)
            {
                if (((ComboBox)owner) != selector)
                {
                    selector.Items.Remove(Int32.Parse(actuator));
                }
            }
        }

        /// <summary>
        /// Repopulates an actuator on all selectors (ComboBox) except for it's owner.
        /// </summary>
        /// <param name="liberatedActuator"></param>
        /// <param name="preowner"></param>
        private void liberateActuator(String liberatedActuator, Object preowner)
        {
            foreach (ComboBox selector in this.selectors)
            {
                if (!selector.Equals(preowner))
                {
                    selector.Items.Add(Int32.Parse(liberatedActuator));
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

            actuators = selectedGlove.GloveConfiguration.PositivePins;

            foreach (int actuator in actuators)
            {
                foreach (ComboBox selector in selectors)
                {
                    selector.Items.Add(actuator);
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
        private void refreshMappingsList(Dictionary<string, string> mappings)
        {
            this.mappingsList.Items.Clear();
            foreach (KeyValuePair<string, string> mapping in mappings.ToList())
            {
                //Console.WriteLine("MAPPING: "+ mapping.Key + ", " + mapping.Value);
                this.mappingsList.Items.Add(new Mapping() { Actuator = mapping.Value, Region = mapping.Key });
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
                configManager.saveGloveProfile(saveConfigurationDialog.FileName, selectedGlove);
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

            if (this.selectedGlove.GloveConfiguration.GloveProfile.Mappings != null)
            {
                //Actualizar vista
                this.refreshMappingsList(this.selectedGlove.GloveConfiguration.GloveProfile.Mappings);
                this.resetSelectors();

                foreach (KeyValuePair<string, string> mapping in this.selectedGlove.GloveConfiguration.GloveProfile.Mappings.ToList())
                {
                    this.selectors[Int32.Parse(mapping.Key)].SelectedItem = Int32.Parse(mapping.Value);
                    this.removeActuator(mapping.Value, this.selectors[Int32.Parse(mapping.Key)]);
                }
                if(this.selectedGlove.GloveConfiguration.GloveProfile.ProfileName == null)
                {
                    this.selectedGlove.GloveConfiguration.GloveProfile.ProfileName = "Unnamed Configuration";
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

                String owner = ((ComboBox)sender).TabIndex.ToString();
                if (selection != null)
                {
                    if (!selection.Equals(""))
                    {
                        removeActuator(selection, sender);
                        try
                        {
                            this.selectedGlove.GloveConfiguration.GloveProfile.Mappings.Add(owner, selection);
                        }
                        catch (Exception)
                        {
                            String liberatedActuator = this.selectedGlove.GloveConfiguration.GloveProfile.Mappings[owner];
                            liberateActuator(liberatedActuator, sender);
                            this.selectedGlove.GloveConfiguration.GloveProfile.Mappings[owner] = selection;
                        }
                    }
                    else
                    {
                        String liberatedActuator;
                        this.selectedGlove.GloveConfiguration.GloveProfile.Mappings.TryGetValue(owner, out liberatedActuator);
                        if (liberatedActuator != null)
                        {
                            liberateActuator(liberatedActuator, sender);
                            this.selectedGlove.GloveConfiguration.GloveProfile.Mappings.Remove(owner);
                        }

                    }
                }
                refreshMappingsList(this.selectedGlove.GloveConfiguration.GloveProfile.Mappings);
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
            this.buttonTestIntensity.IsEnabled = true;
            this.intensityUpDown.IsEnabled = true;
        }

        private bool testing;

        Stopwatch sw = new Stopwatch();

        private void buttonTestIntensity_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedGlove.Connected)
            {
                string message = "Cannot activate. Please establish connection with the device.";
                string caption = "Glove disconnected";
                MessageBoxButton button = MessageBoxButton.OK;

                MessageBox.Show(message, caption, button, MessageBoxImage.Error);
                return;
            }
            sw.Restart();
            Mapping mapping = (Mapping)this.mappingsList.SelectedItem;
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
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


    }

}
