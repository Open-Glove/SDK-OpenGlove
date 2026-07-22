using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Xml.Linq;
using OpenGlove_API_C_Sharp_HL.ServiceReference1;

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Interaction logic for PinsConfiguration.xaml
    /// </summary>
    public partial class PinsConfiguration : Window
    {
        public class PinRow
        {
            public int Pin { get; set; }
            public String Signal { get; set; }
            public String Type { get; set; }
            public String Polarity { get; set; }

            public PinRow(int pin, String signal)
            {
                this.Pin = pin;
                this.Signal = signal;
            }
        }

        public class Board
        {
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

        private static readonly int[] BaudRates = {
            300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 57600, 115200
        };

        private List<Board> openBoards()
        {
            List<Board> result = new List<Board>();

            XDocument xml = XDocument.Load("Boards.xml");
            List<XElement> xBoards = xml.Root.Elements("board").ToList();

            foreach (XElement xBoard in xBoards)
            {
                string name = xBoard.Element("name").Value;
                int digitalPins = Int32.Parse(xBoard.Element("digitalPins").Value);
                int firstAnalogPin = Int32.Parse(xBoard.Element("firstAnalogPin").Value);
                int analogPins = Int32.Parse(xBoard.Element("analogPins").Value);
                Board b = new Board();
                b.name = name;
                b.digitalPins = digitalPins;
                b.analogPins = analogPins;
                b.firstAnalogPin = firstAnalogPin;
                List<int> possiblePins = new List<int>();
                for (int i = 1; i <= digitalPins; i++)
                {
                    possiblePins.Add(i);
                }
                for (int i = 0; i < analogPins; i++)
                {
                    possiblePins.Add(firstAnalogPin + i);
                }
                b.pinNumbers = possiblePins;
                result.Add(b);
            }

            return result;
        }

        private void saveBoard(Board board)
        {
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

        private void initializeBoards()
        {
            this.boards = openBoards();
            this.comboBoxBoard.ItemsSource = this.boards;
            this.comboBoxBaudRate.ItemsSource = BaudRates;
            Polarities = new List<string>() { "Positive", "Negative" };
            Types = new List<string>() { "Vibe Board", "Flex Sensor" };
            Polarity.ItemsSource = Polarities;
            Type.ItemsSource = Types;

            this.comboBoxBoard.SelectionChanged += this.comboBoxBoard_SelectionChanged;

            // Preferir ESP32 si existe; si no, la primera board
            int espIndex = this.boards.FindIndex(b => b.name == "ESP32 DevKit V1");
            this.comboBoxBoard.SelectedIndex = espIndex >= 0 ? espIndex : 0;
        }

        private Glove selectedGlove;

        public PinsConfiguration(Glove selectedGlove)
        {
            InitializeComponent();

            this.selectedGlove = selectedGlove;

            initializeBoards();

            configManager = new ConfigManager();
        }

        private bool IsEsp32Board(Board b)
        {
            return b != null && b.name == "ESP32 DevKit V1";
        }

        private void initializePinsList(int boardIndex)
        {
            if (boardIndex < 0 || boardIndex >= this.boards.Count)
                return;

            this.pins = new List<PinRow>();
            var b = this.boards[boardIndex];

            if (IsEsp32Board(b))
            {
                int[] espPins = {
                    0, 2, 4, 5, 12, 13, 14, 15, 16, 17,
                    18, 19, 21, 22, 23,
                    25, 26, 27,
                    32, 33, 34, 35, 36, 39
                };

                foreach (var n in espPins)
                {
                    bool isAnalog =
                        n == 25 || n == 26 || n == 27 ||
                        n == 32 || n == 33 || n == 34 || n == 35 || n == 36 || n == 39;

                    this.pins.Add(new PinRow(n, isAnalog ? "Analog" : "Digital"));
                }

                this.comboBoxBaudRate.SelectedItem = 115200;
            }
            else
            {
                // LilyPad / boards genéricas (legacy)
                foreach (int pin in b.pinNumbers)
                {
                    if (pin <= b.digitalPins)
                        this.pins.Add(new PinRow(pin, "Digital"));
                    else
                        this.pins.Add(new PinRow(pin, "Analog"));
                }

                // Mismo default histórico del XAML (SelectedIndex 10 → 57600)
                this.comboBoxBaudRate.SelectedItem = 57600;
            }

            this.dataGridPins.ItemsSource = this.pins;
            UpdateBoardPinoutImage(b);
        }

        /// <summary>
        /// Cambia el diagrama de pines según la board. Solo afecta la UI de ayuda;
        /// al guardar se persisten pines + baudRate en la config del guante (no el nombre de board).
        /// </summary>
        private void UpdateBoardPinoutImage(Board board)
        {
            string resource = IsEsp32Board(board)
                ? "ESP32-Pinout.jpg"
                : "A000011_featured.jpg"; // LilyPad / legacy

            try
            {
                image.Source = new BitmapImage(new Uri("pack://application:,,,/" + resource));
            }
            catch
            {
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveConfigurationDialog = new SaveFileDialog();
            saveConfigurationDialog.Filter = "XML-File | *.xml";
            saveConfigurationDialog.Title = "Save your configuration file";
            saveConfigurationDialog.ShowDialog();

            if (saveConfigurationDialog.FileName != "")
            {
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
                            if (pin.Type.Equals("Vibe Board"))
                            {
                                if (pin.Polarity.Equals("Positive"))
                                {
                                    positivePins.Add(pin.Pin);
                                }
                                else if (pin.Polarity.Equals("Negative"))
                                {
                                    negativePins.Add(pin.Pin);
                                }
                            }
                            else if (pin.Type.Equals("Flex Sensor") && pin.Polarity.Equals("Positive"))
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

            if (addBoard.BoardName != null)
            {
                if (!addBoard.BoardName.Equals(""))
                {
                    Board b = new Board();
                    b.name = addBoard.BoardName;
                    b.digitalPins = addBoard.digitalPins;
                    b.analogPins = addBoard.analogPins;
                    b.firstAnalogPin = addBoard.firstAnalogPin;

                    for (int i = 0; i <= b.digitalPins; i++)
                    {
                        pins.Add(i);
                    }

                    for (int i = 0; i < b.analogPins; i++)
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
            if (!refresh && this.comboBoxBoard.SelectedIndex >= 0)
            {
                this.initializePinsList(this.comboBoxBoard.SelectedIndex);
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
            if (this.comboBoxBoard.SelectedIndex >= 0)
                initializePinsList(this.comboBoxBoard.SelectedIndex);
        }
    }
}
