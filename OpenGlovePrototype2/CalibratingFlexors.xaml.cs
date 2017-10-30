using OpenGlove_API_C_Sharp_HL;
using OpenGlove_API_C_Sharp_HL.ServiceReference1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfAnimatedGif;

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Lógica de interacción para CalibratingFlexors.xaml
    /// </summary>
    public partial class CalibratingFlexors : Window
    {
        private OpenGloveAPI gloves = OpenGloveAPI.GetInstance();

        private Glove selectedGlove;

        public CalibratingFlexors(Glove selectedGlove)
        {
            InitializeComponent();
            this.selectedGlove = selectedGlove;
            startWorker();
            
        }
        BackgroundWorker backgroundWorker1;

        public void startWorker()
        {
            backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 50)
            {
                changeOpenAndClose();
            }
            if (e.ProgressPercentage == 99)
            {
                gloves.confirmCalibration(selectedGlove);
                finishCalibration(); 
                
            }
            if (e.ProgressPercentage == 100)
            {
                this.Close();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            gloves.calibrateFlexors(selectedGlove);
            Thread.Sleep(3500);
            backgroundWorker1.ReportProgress(50);
            Thread.Sleep(7000);
            backgroundWorker1.ReportProgress(99);
            Thread.Sleep(2000);
            backgroundWorker1.ReportProgress(100);
        }


        private void changeOpenAndClose()
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(@"/Images/hand_openandclose.gif", UriKind.Relative);
            image.EndInit();
            ImageBehavior.SetAnimatedSource(imageHand, image);
            ImageBehavior.SetRepeatBehavior(imageHand, new RepeatBehavior(0));
            ImageBehavior.SetRepeatBehavior(imageHand, RepeatBehavior.Forever);
            this.labelInstruction.Content = "Open and Close your hand repeatedly";
        }

        private void finishCalibration()
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(@"/Images/hand_quiet.gif", UriKind.Relative);
            image.EndInit();
            ImageBehavior.SetAnimatedSource(imageHand, image);
            ImageBehavior.SetRepeatBehavior(imageHand, new RepeatBehavior(0));
            ImageBehavior.SetRepeatBehavior(imageHand, RepeatBehavior.Forever);
            this.labelInstruction.Content = "Successfully calibrated flexors";
            this.labelMessage.Content = "Done";
        }
    }
}
