using OpenGlove_API_C_Sharp_HL.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpenGlovePrototype2
{
    class ConfigManager
    {
        private OGServiceClient serviceClient;

        public ConfigManager()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            EndpointAddress address = new EndpointAddress("http://localhost:8733/Design_Time_Addresses/OpenGloveWCF/OGService/");
            serviceClient = new OGServiceClient(binding, address);
        }

        public void saveGloveConfiguration(string fileName, Glove selectedGlove)
        {
            XElement rootXML = new XElement("hand");
            XElement boardPins = new XElement("boardPins");
            rootXML.Add(boardPins);
            foreach (int pin in selectedGlove.GloveConfiguration.PositivePins)
            {
                XElement positivePinXML = new XElement("positivePin");
                positivePinXML.SetAttributeValue("pin", pin);
                boardPins.Add(positivePinXML);
            }

            foreach (int pin in selectedGlove.GloveConfiguration.NegativePins)
            {
                XElement negativePinXML = new XElement("negativePin");
                negativePinXML.SetAttributeValue("pin", pin);
                boardPins.Add(negativePinXML);
            }

            foreach (int pin in selectedGlove.GloveConfiguration.FlexPins)
            {
                XElement flexPinXML = new XElement("flexPin");
                flexPinXML.SetAttributeValue("pin", pin);
                boardPins.Add(flexPinXML);
            }

            selectedGlove.GloveConfiguration.GloveHash = selectedGlove.GloveConfiguration.PositivePins.GetHashCode().ToString();
            rootXML.SetAttributeValue("baudRate", selectedGlove.GloveConfiguration.BaudRate);
            rootXML.SetAttributeValue("gloveHash", selectedGlove.GloveConfiguration.GloveHash);
            rootXML.SetAttributeValue("gloveName", fileName);
            selectedGlove.GloveConfiguration.GloveName = fileName;

            rootXML.Save(fileName);

            serviceClient.SaveGlove(selectedGlove);
        }

        public void OpenGloveConfiguration(string fileName, Glove selectedGlove)
        {
            selectedGlove.GloveConfiguration = new Glove.Configuration();

            XDocument xml = XDocument.Load(fileName);
            List<XElement> Xpins = xml.Root.Element("boardPins").Elements("positivePin").ToList();
            List<int> positivePins = new List<int>();
            foreach (XElement xpin in Xpins)
            {
                int pinNumber = Int32.Parse(xpin.Attribute("pin").Value);
                positivePins.Add(pinNumber);
            }

            Xpins = xml.Root.Element("boardPins").Elements("negativePin").ToList();
            List<int> negativePins = new List<int>();
            foreach (XElement xpin in Xpins)
            {
                int pinNumber = Int32.Parse(xpin.Attribute("pin").Value);
                negativePins.Add(pinNumber);
            }

            Xpins = xml.Root.Element("boardPins").Elements("flexPin").ToList();
            List<int> flexPins = new List<int>();
            foreach (XElement xpin in Xpins)
            {
                int pinNumber = Int32.Parse(xpin.Attribute("pin").Value);
                flexPins.Add(pinNumber);
            }


            int baudRate = Int32.Parse(xml.Root.Attribute("baudRate").Value);
            selectedGlove.GloveConfiguration.PositivePins = positivePins.ToArray();
            selectedGlove.GloveConfiguration.NegativePins = negativePins.ToArray();
            selectedGlove.GloveConfiguration.FlexPins = flexPins.ToArray();
            selectedGlove.GloveConfiguration.BaudRate = baudRate;
            selectedGlove.GloveConfiguration.GloveHash = (string)xml.Root.Attribute("gloveHash");
            selectedGlove.GloveConfiguration.GloveName = (string)xml.Root.Attribute("gloveName");

            List<string> positiveInit = new List<string>();
            List<string> negativeInit = new List<string>();

            for (int i = 0; i < positivePins.Count; i++)
            {
                positiveInit.Add("HIGH");
                negativeInit.Add("LOW");
            }

            List<string> flexInit = new List<string>();

            for (int i = 0; i < flexPins.Count; i++)
            {
                flexInit.Add("INPUT");
            }

            selectedGlove.GloveConfiguration.PositiveInit = positiveInit.ToArray();
            selectedGlove.GloveConfiguration.NegativeInit = negativeInit.ToArray();
            selectedGlove.GloveConfiguration.FlexInit = flexInit.ToArray();

            //Tell the service to update the glove configuration
            serviceClient.SaveGlove(selectedGlove);
        }

        public void OpenProfileConfiguration(string fileName, Glove selectedGlove)
        {
            selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();

            Dictionary<String, String> openedConfiguration = new Dictionary<string, string>();

            XDocument xml = XDocument.Load(fileName);

            if (!xml.Root.Attribute("gloveHash").Equals(selectedGlove.GloveConfiguration.GloveHash))
            {
                //avisar
            }

            openedConfiguration = xml.Root.Element("mappings").Elements("mapping")
                               .ToDictionary(c => (string)c.Element("region"),
                                             c => (string)c.Element("actuator"));

            selectedGlove.GloveConfiguration.GloveProfile.Mappings = openedConfiguration;

            openedConfiguration = xml.Root.Element("FlexorsMappings").Elements("mapping")
                               .ToDictionary(c => (string)c.Element("region"),
                                             c => (string)c.Element("flexor"));
            selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings = openedConfiguration;

            //Aqui deberia comprobarse que sean todos valores validos
            selectedGlove.GloveConfiguration.GloveProfile.ProfileName = fileName;
            selectedGlove.GloveConfiguration.GloveProfile.GloveHash = selectedGlove.GloveConfiguration.GloveHash;
            serviceClient.SaveGlove(selectedGlove);
        }

        public void OpenProfileFlexConfiguration(string fileName, Glove selectedGlove)
        {
            if (selectedGlove.GloveConfiguration.GloveProfile == null)
            {
                selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();
            }

            Dictionary<String, String> openedConfiguration = new Dictionary<string, string>();

            XDocument xml = XDocument.Load(fileName);

            if (!xml.Root.Attribute("gloveHash").Equals(selectedGlove.GloveConfiguration.GloveHash))
            {
                //avisar
            }

            openedConfiguration = xml.Root.Element("FlexorsMappings").Elements("mapping")
                               .ToDictionary(c => (string)c.Element("region"),
                                             c => (string)c.Element("flexor"));
            selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings = openedConfiguration;

            //Aqui deberia comprobarse que sean todos valores validos
           // selectedGlove.GloveConfiguration.GloveProfile.ProfileName = fileName;
           // selectedGlove.GloveConfiguration.GloveProfile.GloveHash = selectedGlove.GloveConfiguration.GloveHash;
           // serviceClient.SaveGlove(selectedGlove);
        }

        public void OpenProfileVibeBoardsConfiguration(string fileName, Glove selectedGlove)
        {
            //
            if(selectedGlove.GloveConfiguration.GloveProfile == null)
            {
                selectedGlove.GloveConfiguration.GloveProfile = new Glove.Configuration.Profile();
            }

            Dictionary<String, String> openedConfiguration = new Dictionary<string, string>();

            XDocument xml = XDocument.Load(fileName);

            if (!xml.Root.Attribute("gloveHash").Equals(selectedGlove.GloveConfiguration.GloveHash))
            {
                //avisar
            }

            openedConfiguration = xml.Root.Element("mappings").Elements("mapping")
                               .ToDictionary(c => (string)c.Element("region"),
                                             c => (string)c.Element("actuator"));
            selectedGlove.GloveConfiguration.GloveProfile.Mappings = openedConfiguration;

            //Aqui deberia comprobarse que sean todos valores validos
            // selectedGlove.GloveConfiguration.GloveProfile.ProfileName = fileName;
            // selectedGlove.GloveConfiguration.GloveProfile.GloveHash = selectedGlove.GloveConfiguration.GloveHash;
            // serviceClient.SaveGlove(selectedGlove);
        }


        public void saveGloveProfile(string fileName, Glove selectedGlove)
        {
            XElement rootXML = new XElement("hand");
            rootXML.SetAttributeValue("gloveHash", selectedGlove.GloveConfiguration.GloveHash);
            XElement mappings = new XElement("mappings");
            rootXML.Add(mappings);
            foreach (KeyValuePair<string, string> mapping in selectedGlove.GloveConfiguration.GloveProfile.Mappings)
            {
                XElement mappingXML = new XElement("mapping", new XElement("region", mapping.Key), new XElement("actuator", mapping.Value));
                mappings.Add(mappingXML);
            }

            mappings = new XElement("FlexorsMappings");
            rootXML.Add(mappings);
            foreach (KeyValuePair<string, string> mapping in selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings)
            {
                XElement mappingXML = new XElement("mapping", new XElement("region", mapping.Key), new XElement("flexor", mapping.Value));
                mappings.Add(mappingXML);
            }

            rootXML.Save(fileName);
            selectedGlove.GloveConfiguration.GloveProfile.ProfileName = fileName;
            serviceClient.SaveGlove(selectedGlove);
        }

        public void saveGloveVibeBoardsProfile(string fileName, Glove selectedGlove)
        {
            XElement rootXML = new XElement("hand");
            rootXML.SetAttributeValue("gloveHash", selectedGlove.GloveConfiguration.GloveHash);
            XElement mappings = new XElement("mappings");
            rootXML.Add(mappings);
            foreach (KeyValuePair<string, string> mapping in selectedGlove.GloveConfiguration.GloveProfile.Mappings)
            {
                XElement mappingXML = new XElement("mapping", new XElement("region", mapping.Key), new XElement("actuator", mapping.Value));
                mappings.Add(mappingXML);
            }

            rootXML.Save(fileName);
          //  selectedGlove.GloveConfiguration.GloveProfile.ProfileName = fileName;
          //  serviceClient.SaveGlove(selectedGlove);
        }

        public void saveGloveFlexProfile(string fileName, Glove selectedGlove)
        {
            XElement rootXML = new XElement("hand");
            rootXML.SetAttributeValue("gloveHash", selectedGlove.GloveConfiguration.GloveHash);
            XElement mappings = new XElement("FlexorsMappings");
            rootXML.Add(mappings);
            foreach (KeyValuePair<string, string> mapping in selectedGlove.GloveConfiguration.GloveProfile.FlexorsMappings)
            {
                XElement mappingXML = new XElement("mapping", new XElement("region", mapping.Key), new XElement("flexor", mapping.Value));
                mappings.Add(mappingXML);
            }

            rootXML.Save(fileName);
            //selectedGlove.GloveConfiguration.GloveProfile.ProfileName = fileName;
           // serviceClient.SaveGlove(selectedGlove);
        }

        public void saveGlove(Glove selectedGlove) {
            serviceClient.SaveGlove(selectedGlove);
        }

    }
}
