using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CncCommunicationTool
{
    public partial class Form1 : Form
    {
        private List<MachineProfile> machineProfiles;

        private StringBuilder receivingBuilder;
        private string receivingFileName;
        private string receivingMachineName;
        private Timer receiveTimer;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            notifyIcon1.Icon = new Icon(Application.StartupPath + "\\connect.ico");

            receiveTimer = new Timer();
            receiveTimer.Interval = 2000;
            receiveTimer.Tick += (x, y) => DoneReceiving();

            LoadMachineProfiles();
            SetupContextMenu();
        }

        private void LoadMachineProfiles()
        {
            if (File.Exists("Machines.xml") == false)
                throw new ConfigurationErrorsException("Machines.xml not found.");

            var machineCfgDoc = XDocument.Parse(File.ReadAllText("Machines.xml"));

            machineProfiles = machineCfgDoc.Descendants("machine").Select(m => new MachineProfile(m)).OrderBy(m => m.Name).ToList();
        }

        private void ResetSerialPort()
        {
            if (serialPort.IsOpen)
                serialPort.Close();
        }

        private void SetupSerialPort(MachineProfile machineProfile)
        {
            serialPort.BaudRate = machineProfile.BaudRate;
            serialPort.DataBits = machineProfile.DataBits;
            serialPort.Encoding = Encoding.ASCII;

            Handshake handshake;

            if (Enum.TryParse<Handshake>(machineProfile.Handshake, out handshake))
                serialPort.Handshake = handshake;
            else
                throw new ConfigurationErrorsException(String.Format("Machine [{0}] handshake mode [{1}] is invalid.", machineProfile.Name, machineProfile.Handshake));

            Parity parity;

            if (Enum.TryParse<Parity>(machineProfile.Parity, out parity))
                serialPort.Parity = parity;
            else
                throw new ConfigurationErrorsException(String.Format("Machine [{0}] parity mode [{1}] is invalid.", machineProfile.Name, machineProfile.Parity));

            serialPort.PortName = machineProfile.Port;

            StopBits stopBits;

            if (Enum.TryParse<StopBits>(machineProfile.StopBits, out stopBits))
                serialPort.StopBits = stopBits;
            else
                throw new ConfigurationErrorsException(String.Format("Machine [{0}] stop bits [{1}] is invalid.", machineProfile.Name, machineProfile.StopBits));
        }

        private void SetupContextMenu()
        {
            foreach (var machineProfile in machineProfiles)
            {
                var profile = machineProfile;
                var machineMenuItem = new ToolStripMenuItem(profile.Name);

                var sendMenuItem = new ToolStripMenuItem("Send File...");
                var receiveMenuItem = new ToolStripMenuItem("Receive File...");

                machineMenuItem.DropDownItems.Add(sendMenuItem);
                machineMenuItem.DropDownItems.Add(receiveMenuItem);

                sendMenuItem.Click += (sender, e) =>
                    Send(profile);

                receiveMenuItem.Click += (sender, e) =>
                    Receive(profile);

                trayMenu.Items.Add(machineMenuItem);
            }
        }

        private void Send(MachineProfile machine)
        {
            ResetSerialPort();
            SetupSerialPort(machine);

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if ((String.IsNullOrEmpty(openFileDialog1.FileName)) || (File.Exists(openFileDialog1.FileName) == false))
                {
                    MessageBox.Show("The requested file was not found.", "File not found.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                if (MessageBox.Show(String.Format("Is [{0}] ready to receive the file?", machine.Name), "Ready?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                    return;

                try
                {
                    serialPort.WriteBufferSize = 512;
                    serialPort.Open();

                    notifyIcon1.ShowBalloonTip(1000, "Sending...", String.Format("Sending [{0}] to [{1}]...", openFileDialog1.FileName, machine.Name), ToolTipIcon.Info);

                    serialPort.Write(File.ReadAllText(openFileDialog1.FileName));

                    notifyIcon1.ShowBalloonTip(1000, "Sent.", String.Format("[{0}] sent to [{1}].", openFileDialog1.FileName, machine.Name), ToolTipIcon.Info);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("An error occurred while attempting to send the selected file [{0}] to [{1}]. More details are provided below:\n\n{2}",
                        openFileDialog1.FileName, machine.Name, ex.Message), "Send Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();
                }
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (receiveTimer.Enabled == false)
            {
                BeginInvoke(new Action(() => notifyIcon1.ShowBalloonTip(1000, "Receiving...", String.Format("Receiving [{0}] from [{1}]...", receivingFileName, receivingMachineName), ToolTipIcon.Info)));
                receivingBuilder = new StringBuilder();
            }

            BeginInvoke(new Action(() =>
                {
                    receiveTimer.Stop();
                    receiveTimer.Start();
                }));

            receivingBuilder.Append(serialPort.ReadExisting());
        }

        private void DoneReceiving()
        {
            receiveTimer.Stop();

            BeginInvoke(new Action(() => notifyIcon1.ShowBalloonTip(1000, "Received file.", String.Format("Received [{0}] from [{1}].", receivingFileName, receivingMachineName), ToolTipIcon.Info)));

            File.WriteAllText(receivingFileName, receivingBuilder.ToString());
        }

        private void Receive(MachineProfile machine)
        {
            ResetSerialPort();
            SetupSerialPort(machine);

            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (String.IsNullOrEmpty(openFileDialog1.FileName))
                {
                    MessageBox.Show("Please provide a file name before continuing.", "File name is required.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                try
                {
                    if (receiveTimer.Enabled)
                        receiveTimer.Stop();

                    receivingBuilder = new StringBuilder();
                    receivingFileName = saveFileDialog1.FileName;
                    receivingMachineName = machine.Name;

                    serialPort.ReadBufferSize = 512;
                    serialPort.Open();

                    MessageBox.Show(String.Format("Please send the file from [{0}] now.", machine.Name));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("An error occurred while attempting to send the selected file [{0}] to [{1}]. More details are provided below:\n\n{2}",
                       openFileDialog1.FileName, machine.Name, ex.Message), "Send Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void notifyIcon1_MouseDown(object sender, MouseEventArgs e)
        {
            trayMenu.Show(Control.MousePosition);
        }

        private void serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            BeginInvoke(new Action(() =>
                {
                    receiveTimer.Stop();
                    ResetSerialPort();

                    MessageBox.Show(String.Format("An error occurred while attempting to send the receive file [{0}] to [{1}]. More details are provided below:\n\n{2}",
                      receivingFileName, receivingMachineName, e.EventType), "Send Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
        }
    }
}
