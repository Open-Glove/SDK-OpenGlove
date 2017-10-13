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
using OpenGloveSDKConfigurationPrototype2;
using Microsoft.Win32;
using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;
using OpenGlove_API_C_Sharp_HL;
using OpenGlove_API_C_Sharp_HL.ServiceReference1;
using System.ServiceProcess;

namespace OpenGlovePrototype2
{

    /// <summary>
    /// Interaction logic for Greeter.xaml
    /// </summary>
    public partial class Greeter : Window
    {
        private TaskbarIcon tbi;

        private OpenGloveAPI gloves;

        private Glove selectedGlove;

        private BackgroundWorker bgw;

        private ConfigManager configManager;

        private bool serviceAvailable = true;

        void getGlovesAsync(object sender, DoWorkEventArgs e)
        {
            try
            {
                e.Result = gloves.Devices;
            }
            catch (Exception)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Service Unavailable", "Error", MessageBoxButton.OK);  
            }
        }

        void getGlovesAsyncUpdate(object sender, DoWorkEventArgs e) {
            try
            {
                e.Result = gloves.UpdateDevices();
            }
            catch (Exception)
            {
                
                MessageBoxResult messageBoxResult = MessageBox.Show("Service Unavailable", "Error", MessageBoxButton.OK);
            }
        }

        void GLovesUpdateProcess(object sender, ProgressChangedEventArgs e)
        {
            //Progress bar.
        }

        void getGlovesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //After completing the job.
            this.bar.Close();
            if (e.Result != null)
            {
                this.listViewGloves.ItemsSource = (List<Glove>)e.Result;
                this.serviceToggle.Header = "Service running (Click to Stop)";
                this.ServiceStatusIcon.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString("#00ff3f"); 
                serviceAvailable = true;
                
            }
            else {
                this.listViewGloves.ItemsSource = null;
                this.serviceToggle.Header = "Service unavailable (Click to Start)";
                this.ServiceStatusIcon.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString("#ff0000");
                serviceAvailable = false;
            }
        }

        public Greeter()
        {
            InitializeComponent();
            configManager = new ConfigManager();
            gloves = OpenGloveAPI.GetInstance();
            bgw = new BackgroundWorker();
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(GLovesUpdateProcess);
            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(getGlovesCompleted);
            bgw.DoWork += new DoWorkEventHandler(getGlovesAsync);

            ReloadGloves();

            tbi = new TaskbarIcon();
            tbi.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location); ;
            tbi.ToolTipText = "OpenGlove";

            MenuItem mi1 = new MenuItem();
            mi1.Header = "Exit";
            mi1.Click += sysTrayItemClicked;

            tbi.ContextMenu = new ContextMenu();

            tbi.ContextMenu.Items.Add(mi1);

            tbi.TrayLeftMouseUp += this.onTrayClick;

            comboBoxSide.Items.Add(Side.Left);
            comboBoxSide.Items.Add(Side.Right);

        }

        private List<Glove> updateGloves() {
            return gloves.Devices;
        }

        LoadingBar bar;
        private void ReloadGloves() {
            bgw.RunWorkerAsync();
            bar = new LoadingBar();
            bar.ShowDialog();
        }

        private void onTrayClick(object sender, RoutedEventArgs e)
        {
            toggleVisibility();
            //ReloadGloves();
        }

        private void toggleVisibility() {
            if (this.Visibility == Visibility.Visible)
            {
                this.Visibility = Visibility.Hidden;
            }
            else {
                this.Visibility = Visibility.Visible;
            }
        }

        private void sysTrayItemClicked(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Header.Equals("Exit"))
            {
                this.Close();
            }
        }

        private void listViewGloves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedGlove = (Glove)((ListView)sender).SelectedItem;
            refreshControls();
        }

        private void refreshControls()
        {
            if (selectedGlove != null)
            {
                this.comboBoxSide.SelectedItem = selectedGlove.Side;
                this.comboBoxSide.IsEnabled = true;

            }

            if (serviceAvailable)
            {
                this.serviceToggle.Header = "Stop Service";
            }
            else {
                this.serviceToggle.Header = "Start Service";
            }

            if (selectedGlove.GloveConfiguration == null)
            {
                this.labelProfile.Content = "None";
                this.labelGloveConfig.Content = "None";
                this.buttonCreateGloveConfig.IsEnabled = true;
                this.buttonOpenGloveConfig.IsEnabled = true;
                this.buttonCreateProfileConfig.IsEnabled = false;
                this.buttonOpenProfileConfig.IsEnabled = false;
                this.ConnectMenuItem.IsEnabled = false;
                this.CurrentProfileMenuItem.IsEnabled = false;
            }
            else
            {
                this.labelGloveConfig.Content = this.selectedGlove.GloveConfiguration.GloveName;
                this.buttonCreateGloveConfig.IsEnabled = true;
                this.buttonOpenGloveConfig.IsEnabled = true;
                this.buttonCreateProfileConfig.IsEnabled = true;
                this.buttonOpenProfileConfig.IsEnabled = true;
                this.ConnectMenuItem.IsEnabled = true;

                if (this.selectedGlove.GloveConfiguration.GloveProfile == null)
                {
                    this.labelProfile.Content = "None";
                    
                }
                else
                {
                    this.CurrentProfileMenuItem.IsEnabled = true;
                    this.labelProfile.Content = this.selectedGlove.GloveConfiguration.GloveProfile.ProfileName;
                }

                if (selectedGlove.Connected)
                {
                    this.ConnectMenuItem.Header = "Disconnect";
                }
                else {
                    this.ConnectMenuItem.Header = "Connect";
                }
            }

        }

        private void buttonCreateGloveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (this.selectedGlove.GloveConfiguration == null) {
                this.selectedGlove.GloveConfiguration = new Glove.Configuration();
            }

            PinsConfiguration pins = new PinsConfiguration(this.selectedGlove);
            pins.ShowDialog();
        }

        private void buttonRefreshGloves_Click(object sender, RoutedEventArgs e)
        {
            bgw = new BackgroundWorker();
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(GLovesUpdateProcess);
            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(getGlovesCompleted);
            bgw.DoWork += new DoWorkEventHandler(getGlovesAsyncUpdate);

            this.ReloadGloves();
        }

        private void buttonOpenGloveConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will close the current profile. Are you sure?", "New configuration confirmation", MessageBoxButton.YesNo);

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                OpenFileDialog openConfigurationDialog = new OpenFileDialog();
                openConfigurationDialog.Filter = "XML-File | *.xml";
                openConfigurationDialog.Title = "Open a glove configuration file";
                openConfigurationDialog.ShowDialog();

                if (openConfigurationDialog.FileName != null)
                {
                    if (openConfigurationDialog.FileName != "")
                    {
                        configManager.OpenGloveConfiguration(openConfigurationDialog.FileName, selectedGlove);
                        refreshControls();
                    }
                }
            }
        }

        private void buttonOpenProfileConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will close the current profile. Are you sure?", "New configuration confirmation", MessageBoxButton.YesNo);

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                OpenFileDialog openConfigurationDialog = new OpenFileDialog();
                openConfigurationDialog.Filter = "XML-File | *.xml";
                openConfigurationDialog.Title = "Open a glove profile file";
                openConfigurationDialog.ShowDialog();

                if (openConfigurationDialog.FileName != null)
                {
                    if (openConfigurationDialog.FileName != "")
                    {
                        configManager.OpenProfileConfiguration(openConfigurationDialog.FileName, selectedGlove);
                        refreshControls();
                    }
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void CurrentProfileMenuItem_Click(object sender, RoutedEventArgs e)
        {

            if (selectedGlove.GloveConfiguration.GloveProfile.Mappings.Count != 0)
            {
                ConfigurationTool config = new ConfigurationTool(selectedGlove);
                config.Show();
            }
            else
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("No profile loaded.", "No profile", System.Windows.MessageBoxButton.OK);

            }
        }

        private void ConnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGlove.Connected)
            {
                int result = gloves.Disconnect(selectedGlove);
                
                if (result == 0)
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show("Glove " + selectedGlove.Name + " successfully disconnected.", "Connection", MessageBoxButton.OK);
                    selectedGlove.Connected = false;
                }
            }
            else
            {
                int result = gloves.Connect(selectedGlove);
                if (result == 0)
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show("Glove " + selectedGlove.Name + " successfully connected.", "Connection", MessageBoxButton.OK);
                    selectedGlove.Connected = true;
                }
                else
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show("Can't connect to" + selectedGlove.Name + ". Try repairing it.", "Connection", MessageBoxButton.OK);
                }
            }
            refreshControls();
        }

        private void buttonCreateProfileConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will close the current profile. Are you sure?", "New configuration confirmation", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                HandConfiguration HC = new HandConfiguration(this.selectedGlove);
                HC.ShowDialog();
               // ConfigurationTool config = new ConfigurationTool(this.selectedGlove);
              //  config.ShowDialog();
                refreshControls();
            }
            
        }

        private void comboBoxSide_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedGlove.Connected == false)
            {
                selectedGlove.Side = (Side)((ComboBox)sender).SelectedItem;
                configManager.saveGlove(selectedGlove);
            }else
            {
                if (selectedGlove.Side == Side.Right)
                {
               //MessageBoxResult messageBoxResult = MessageBox.Show("Para cambiar el guante de lado, primero desconecta el guante", "Cambio de lado", MessageBoxButton.OK);
                    ((ComboBox)sender).SelectedIndex = 1;
                }else
                {
                    ((ComboBox)sender).SelectedIndex = 0;
                }
            }
            
        }

        private int startService() {
            ServiceController service = new ServiceController("OpenGloveService");
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(10000);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                return 1;
            }
            catch(Exception ex)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("You must run this program as administrator to control the service.", "Permission required", MessageBoxButton.OK);
                return 0;
            }
        }

        private int stopService() {
            ServiceController service = new ServiceController("OpenGloveService");
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(50000);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                return 1;
            }
            catch (Exception ex)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("You must run this program as administrator to control the service.", "Permission required", MessageBoxButton.OK);
                return 0;
            }
        }

        private void HideWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void serviceToggle_Click(object sender, RoutedEventArgs e)
        {
            int result = 0;

            if (serviceAvailable)
            {
                result = stopService();
            }
            else {
                result = startService();
            }
            if (result == 1) {

                bgw = new BackgroundWorker();
                bgw.WorkerReportsProgress = true;
                bgw.ProgressChanged += new ProgressChangedEventHandler(GLovesUpdateProcess);
                bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(getGlovesCompleted);
                bgw.DoWork += new DoWorkEventHandler(getGlovesAsyncUpdate);

                this.ReloadGloves();
            }
            
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            gloves.letsgoWS();
        }
    }
}
