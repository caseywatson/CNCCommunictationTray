using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CncCommunicationTool
{
    public class MachineProfile
    {
        public MachineProfile() { }

        public MachineProfile(XElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            Load(element);
        }

        public string Name { get; set; }
        public string Port { get; set; }
        public int BaudRate { get; set; }
        public string Parity { get; set; }
        public int DataBits { get; set; }
        public string StopBits { get; set; }
        public string Handshake { get; set; }

        private void Load(XElement element)
        {
            var nameAttribute = element.Attribute("name");
            var portAttribute = element.Attribute("port");
            var baudRateAttribute = element.Attribute("baudRate");
            var parityAttribute = element.Attribute("parity");
            var dataBitsAttribute = element.Attribute("dataBits");
            var stopBitsAttribute = element.Attribute("stopBits");
            var handshakeAttribute = element.Attribute("handshake");

            if ((nameAttribute == null) || (String.IsNullOrEmpty(nameAttribute.Value)))
                throw new ConfigurationErrorsException("Machine name [name] is required.");

            if ((portAttribute == null) || (String.IsNullOrEmpty(portAttribute.Value)))
                throw new ConfigurationErrorsException("Machine port [port] is required.");

            if ((baudRateAttribute == null) || (String.IsNullOrEmpty(baudRateAttribute.Value)))
                throw new ConfigurationErrorsException("Machine baud rate [baudRate] is required.");

            if ((parityAttribute == null) || (String.IsNullOrEmpty(parityAttribute.Value)))
                throw new ConfigurationErrorsException("Machine parity [parity] is required.");

            if ((dataBitsAttribute == null) || (String.IsNullOrEmpty(dataBitsAttribute.Value)))
                throw new ConfigurationErrorsException("Machine data bits [dataBits] is required.");

            if ((stopBitsAttribute == null) || (String.IsNullOrEmpty(stopBitsAttribute.Value)))
                throw new ConfigurationErrorsException("Machine stop bits [stopBits] is required.");

            if ((handshakeAttribute == null) || (String.IsNullOrEmpty(handshakeAttribute.Value)))
                throw new ConfigurationErrorsException("Machine handshake [handshake] is required.");

            Name = nameAttribute.Value;
            Port = portAttribute.Value;
            BaudRate = baudRateAttribute.Value.TryParseInt();
            Parity = parityAttribute.Value;
            DataBits = dataBitsAttribute.Value.TryParseInt();
            StopBits = stopBitsAttribute.Value;
            Handshake = handshakeAttribute.Value;
        }
    }
}
