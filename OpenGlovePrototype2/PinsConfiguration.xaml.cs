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
using System.Windows.Forms;
using System.Xml.Linq;
using OpenGlove_API_C_Sharp_HL;
using OpenGlove_API_C_Sharp_HL.ServiceReference1;

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Interaction logic for PinsConfiguration.xaml
    /// </summary>
    public partial class PinsConfiguration : Window
    {
        public class PinRow{
            public int Pin { get; set; }
            public String Signal {get; set;}
            public String Type { get; set; }
            public String Polarity { get; set; }

            public PinRow(int pin, String signal)
            {
                this.Pin = pin;
                this.Signal = signal;
            }
        }

        public class Board {
            public string name { get; set; }
            public int digitalPins { get; set; }
            public int analogPins { get; set; }
            public int firstAnalogPin { get; set; }
            public List<int> pinNumbers { get; set; }

            public override string ToString()
            {
                return this.name;
            }
        }

        public List<PinRow> pins;

        List<String> Polarities;
        List<String> Types;

        private List<Board> boards;

        private ConfigManager configManager;

        private List<Board> openBoards() {
            List<Board> result = new List<Board>();
            
            XDocument xml = XDocument.Load("Boards.xml");
            List<XElement> xBoards = xml.Root.Elements("board").ToList();

            foreach (XElement xBoard in xBoards)
            {
                string name = xBoard.Element("name").Value;
               // int pins = Int32.Parse(xBoard.Element("pins").Value);
                int digitalPins = Int32.Parse(xBoard.Element("digitalPins").Value);
                int firstAnalogPin = Int32.Parse(xBoard.Element("firstAnalogPin").Value);
                int analogPins = Int32.Parse(xBoard.Element("analogPins").Value);
                Board b = new Board();
                b.name = name;
                b.digitalPins = digitalPins;
                b.analogPins = analogPins;
                b.firstAnalogPin = firstAnalogPin;
                List<int> possiblePins = new List<int>();
                for (int i = 1; i <= digitalPins;i++) //add digital pins to the list
                {
                    possiblePins.Add(i);

                }
                for (int i = 0; i < analogPins; i++) // add analog pins to the list
                {
                    possiblePins.Add(firstAnalogPin+i);
                }
                b.pinNumbers = possiblePins;
                result.Add(b);
            }

            return result;
        }

        private void saveBoard(Board board) {
            XDocument xml = XDocument.Load("Boards.xml");

            XElement xboard = new XElement("board");
            XElement xname = new XElement("name");
            XElement xdigitalPins = new XElement("digitalPins");
            XElement xfirstAnalogPin = new XElement("firstAnalogPin");
            XElement xanalogPins = new XElement("analogPins");

            xname.Value = board.name;
            xdigitalPins.Value = board.digitalPins.ToString();
            xanalogPins.Value = board.analogPins.ToString();
            xfirstAnalogPin.Value = board.firstAnalogPin.ToString();

            xboard.Add(xname);
            xboard.Add(xdigitalPins);
            xboard.Add(xfirstAnalogPin);
            xboard.Add(xanalogPins);
  
            xml.Root.Add(xboard);

            xml.Save("Boards.xml");
            this.boards.Add(board);
            refreshBoards();
        }

        private void initializeBoards() {
            this.boards = openBoards();
            this.comboBoxBoard.SelectedIndex = 0;
            this.initializePinsList(0);
            this.comboBoxBoard.SelectionChanged += this.comboBoxBoard_SelectionChanged;
        }

        private Glove selectedGlove;

        public PinsConfiguration(Glove selectedGlove)
        {
            InitializeComponent();

            this.selectedGlove = selectedGlove;

            initializeBoards();

            configManager = new ConfigManager();

        }

        private void initializePinsList(int boardIndex) {
            this.pins = new List<PinRow>();
            foreach (int pin in this.boards[boardIndex].pinNumbers)
            {
                if (pin <= this.boards[boardIndex].digitalPins )
                {
                    this.pins.Add(new PinRow(pin, "Digital"));
                }else
                {
                    this.pins.Add(new PinRow(pin, "Analog"));
                }      
            }
            this.dataGridPins.ItemsSource = this.pins;

            Polarities = new List<string>() {"Positive", "Negative" };
            Types = new List<string>() { "Vibe Board", "Flex Sensor" };
            Polarity.ItemsSource = Polarities;
            Type.ItemsSource = Types;
            this.comboBoxBaudRate.ItemsSource = new List<int> { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200 };
            this.comboBoxBoard.ItemsSource = this.boards;
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveConfigurationDialog = new SaveFileDialog();
            saveConfigurationDialog.Filter = "XML-File | *.xml";
            saveConfigurationDialog.Title = "Save your configuration file";
            saveConfigurationDialog.ShowDialog();

            if (saveConfigurationDialog.FileName != "") {
                if (this.comboBoxBaudRate.SelectedItem != null)
                {
                    selectedGlove.GloveConfiguration.BaudRate = Int32.Parse(this.comboBoxBaudRate.SelectedItem.ToString());

                    List<int> positivePins = new List<int>();
                    List<int> negativePins = new List<int>();
                    List<int> flexPins = new List<int>();

                    foreach (PinRow pin in pins)
                    {
                        if (pin.Polarity != null && pin.Type != null)
                        {
                            if(pin.Type.Equals("Vibe Board"))
                            {
                                if (pin.Polarity.Equals("Positive"))
                                {
                                    positivePins.Add(pin.Pin);
                                }
                                else if (pin.Polarity.Equals("Negative"))
                                {
                                    negativePins.Add(pin.Pin);
                                }
                            }else if(pin.Type.Equals("Flex Sensor") && pin.Polarity.Equals("Positive"))
                            {
                                flexPins.Add(pin.Pin);
                            }
                            
                        }
                    }

                    selectedGlove.GloveConfiguration.PositivePins = positivePins.ToArray();
                    selectedGlove.GloveConfiguration.NegativePins = negativePins.ToArray();
                    selectedGlove.GloveConfiguration.FlexPins = flexPins.ToArray();
                    

                    List<string> positiveInit = new List<string>();
                    List<string> negativeInit = new List<string>();
                    List<string> flexInit = new List<string>();

                    for (int i = 0; i < positivePins.Count; i++)
                    {
                        positiveInit.Add("HIGH");
                        negativeInit.Add("LOW");
                    }

                    for (int i = 0; i < flexPins.Count; i++)
                    {
                        flexInit.Add("INPUT");
                    }

                    selectedGlove.GloveConfiguration.PositiveInit = positiveInit.ToArray();
                    selectedGlove.GloveConfiguration.NegativeInit = negativeInit.ToArray();
                    selectedGlove.GloveConfiguration.FlexInit = flexInit.ToArray();

                    configManager.saveGloveConfiguration(saveConfigurationDialog.FileName, selectedGlove);
                }
                else
                {
                    string message = "Must select BaudRate";
                    string caption = "BaudRate";
                    MessageBoxButton button = MessageBoxButton.OK;
                    System.Windows.MessageBox.Show(message, caption, button, MessageBoxImage.Error);
                }
            }
 
        }

        private void buttonAddBoard_Click(object sender, RoutedEventArgs e)
        {
            AddBoard addBoard = new AddBoard();
            addBoard.ShowDialog();

            List<int> pins = new List<int>();

            if (addBoard.BoardName != null) {
                if (! addBoard.BoardName.Equals("")) {
                    Board b = new Board();
                    b.name = addBoard.BoardName;
                    b.digitalPins = addBoard.digitalPins;
                    b.analogPins = addBoard.analogPins;
                    b.firstAnalogPin = addBoard.firstAnalogPin;

                    for(int i = 0; i <= b.digitalPins; i++)
                    {
                        pins.Add(i);
                    }

                    for (int i = 0; i < b.analogPins; i++) // add analog pins to the list
                    {
                        pins.Add(b.firstAnalogPin + i);
                    }

                    b.pinNumbers = pins;

                    this.saveBoard(b);
                }
            }
        }

        Boolean refresh = false;

        private void comboBoxBoard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!refresh)
            {
                this.initializePinsList(((System.Windows.Controls.ComboBox)sender).SelectedIndex);
            }
           
        }

        private void dataGridPins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void refreshBoards()
        {
            refresh = true;
            int actualIndex = this.comboBoxBoard.SelectedIndex;
            this.boards = openBoards();
            this.comboBoxBoard.ItemsSource = this.boards;
            this.comboBoxBoard.SelectedIndex = actualIndex;
            refresh = false;
        } 
    }
}
