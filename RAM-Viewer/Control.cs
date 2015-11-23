using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace RAM_Viewer
{
    public class Control
    {
        private MainWindow MainWindow;
        private DispatcherTimer CheckForCompleteTimer; // checks if 2048 bytes have been received and runs only when the serial port is open

        private SerialPort Port; // selected port

        public List<MemoryStamp> MemoryStamps = new List<MemoryStamp>(); // list of all memory stamps which have been received

        private string PortName;
        private int BaudRate = 57600;

        public int MillisecondsBetweenMemoryStamps = 500,  // minimum time between two memory stamps
            MicrosecondsBetweenBytes = 10000; // minimum time between two bytes 

        public bool ConnectionOpened = false;

        public List<WatchListVariable> WatchList = new List<WatchListVariable>(); // watch list


        public Control(MainWindow MainWindow)
        {
            this.MainWindow = MainWindow; // bidirectional association between MainWindow and Control

            MainWindow.SetApplicationStatus(ApplicationStatus.NoPort);
            MainWindow.RefreshPortList();

            // initialize timer
            CheckForCompleteTimer = new DispatcherTimer(DispatcherPriority.SystemIdle);
            CheckForCompleteTimer.Tick += new EventHandler(CheckForComplete);
            CheckForCompleteTimer.Interval = TimeSpan.FromMilliseconds(5);
        }

        public void SetPortName(string NewPortName)
        {
            PortName = NewPortName;
            ReadyToListenForData();
        }

        public void SetBaudRate(int NewBaudRate)
        {
            BaudRate = NewBaudRate;
            ReadyToListenForData();
        }

        private bool ReadyToListenForData() // checks if all data is given to open the port (port name, baud rate and no open port are required)
        {
            // check PortName
            if (PortName == null || PortName.Length == 0 || !PortNameList().Contains(PortName))
            {
                MainWindow.RefreshPortList();
                MainWindow.SetApplicationStatus(ApplicationStatus.NoPort);
                return false;
            }

            // check BaudRate
            if (BaudRate == 0)
            {
                MainWindow.SetApplicationStatus(ApplicationStatus.InvalidBaudRate);
                return false;
            }
            else if (BaudRate == -1)
            {
                MainWindow.SetApplicationStatus(ApplicationStatus.NoBaudRate);
                return false;
            }

            // already connected
            if (Port != null)
            {
                return false;
            }

            MainWindow.SetApplicationStatus(ApplicationStatus.Ready);
            return true;
        }

        public void ListenForData() // enables listening for incoming data
        {
            if (!ReadyToListenForData())
                return;

            MainWindow.DisableConnectionSettings();
            MainWindow.SetApplicationStatus(ApplicationStatus.Listening);

            // create port instance
            Port = new SerialPort(PortName, BaudRate);
            Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler); // add event handler
            try
            {
                Port.Open(); // open port
                ConnectionOpened = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                MainWindow.EnableConnectionSettings();
            }
        }

        public void CloseConnection() // closes the port
        {
            if (Port == null)
                return;
            try
            {
                Port.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            Port = null;
            ConnectionOpened = false;

            ReadyToListenForData(); // refresh application status label on main window

            MainWindow.EnableConnectionSettings();
        }

        private void DataReceivedHandler(object Sender, SerialDataReceivedEventArgs e) // fired when data is incoming
        {
            // enable timer if it isn't yet
            if (!CheckForCompleteTimer.IsEnabled)
                CheckForCompleteTimer.Start();


            if (MemoryStamps.Count == 0 || 
                MemoryStamps[MemoryStamps.Count - 1].Complete() &&

                // Timing stuff is necessary to make sure that some time passed by until a new memory stamp is created. 
                // Otherwise a few wrong bytes (which might be sent e.g. when the microcontroller restarts) may cause trouble. With this check will be shifted out and will be lost. 
                MemoryStamps[MemoryStamps.Count - 1].LastTimeDataWasAdded <= DateTime.Now.Ticks - MillisecondsBetweenMemoryStamps * 1000 * 10
                )
            { // new memory stamp incoming
                MemoryStamps.Add(new MemoryStamp("MemoryStamp" + MemoryStamps.Count.ToString())); // allocate memory for incoming data
                CheckForCompleteTimer.Start();
            }

            SerialPort Port = (SerialPort)Sender;
            // read all bytes from the serial port
            for (int i = 0, BytesAvailable = Port.BytesToRead; i < BytesAvailable; i++)
            {
                try
                {
                    MemoryStamps[MemoryStamps.Count - 1].PushByte((byte)Port.ReadByte());

                }
                catch (Exception)
                {
                    return;
                }
            }
        }


        public void CheckForComplete(object Sender, EventArgs E) // timer event checks if one memory stamp is complete
        {
            try
            {
                if (!MemoryStamps[MemoryStamps.Count - 1].Complete() && MemoryStamps[MemoryStamps.Count - 1].BytesReceived > 0 && MemoryStamps[MemoryStamps.Count - 1].LastTimeDataWasAdded <= DateTime.Now.Ticks - MicrosecondsBetweenBytes * 10)
                {
                    if (MessageBox.Show("The latest memory stamp was not fully received. Do you want to delete it?", "Memory stamp damaged", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        MemoryStamps.RemoveAt(MemoryStamps.Count - 1);
                    }
                    else
                    {
                        MemoryStamps[MemoryStamps.Count - 1].BytesReceived = MemoryStamp.TotalBytes; ;
                    }
                }

                // gui stuff
                MainWindow.SetProgress(MemoryStamps[MemoryStamps.Count - 1].GetPercentage()); // refresh progress bar
                MainWindow.Label_BytesReceived.Content = "received " + MemoryStamps[MemoryStamps.Count - 1].BytesReceived.ToString() + "/" + MemoryStamp.TotalBytes.ToString(); // refresh label

                // go through all memory stamps... 
                foreach (MemoryStamp SingleMemoryStamp in MemoryStamps)
                {
                    // ... and check if they are filled with 2048 bytes (equates to the .Complete() method) and not already added to the main window
                    if (SingleMemoryStamp.Complete() && !SingleMemoryStamp.AddedToMainWindow)
                    {
                        // check if to many bytes have been received
                        /*if (SingleMemoryStamp.BytesShiftedOut > 0)
                            MessageBox.Show(SingleMemoryStamp.BytesShiftedOut.ToString() + " bytes were lost");*/

                        MainWindow.AddMemoryStamp(SingleMemoryStamp);
                        MainWindow.SetApplicationStatus(ApplicationStatus.Listening);
                        SingleMemoryStamp.AddedToMainWindow = true;
                    }
                }

                // stop timer if the last memory stamp is complete
                if (MemoryStamps[MemoryStamps.Count - 1].Complete())
                {
                    CheckForCompleteTimer.Stop();
                    RefreshWatchListValues();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        public void AddAllMemoryStamps() // adds all memory stamps to the main window
        {
            foreach (MemoryStamp MemoryStamp in MemoryStamps)
            {
                MainWindow.AddMemoryStamp(MemoryStamp);
            }
        }

        private void RefreshWatchListValues()
        {
            if (MemoryStamps.Count == 0) // reset all watch list values if no memory stamp is available
            {
                foreach (WatchListVariable Variable in WatchList)
                {
                    Variable.ValueString = null;
                }


                return;
            }

            MemoryStamp LatestMemoryStamp = MemoryStamps[MemoryStamps.Count - 1]; // reference to the latest memory stamp
            foreach (WatchListVariable Variable in WatchList)
            { 
                // allocate memory for the few bytes which store the variable
                byte[] NewValue;
                switch (Variable.DataType)
                {
                case Control.DataType.Boolean:
                    NewValue = new byte[Variable.Elements * 1];
                    break;
                case Control.DataType.Byte:
                    NewValue = new byte[Variable.Elements * 1];
                    break;
                case Control.DataType.Char:
                    NewValue = new byte[Variable.Elements * 1];
                    break;
                case Control.DataType.UnsignedInt:
                    NewValue = new byte[Variable.Elements * 2];
                    break;
                case Control.DataType.Int:
                    NewValue = new byte[Variable.Elements * 2];
                    break;
                case Control.DataType.UnsignedLong:
                    NewValue = new byte[Variable.Elements * 4];
                    break;
                case Control.DataType.Long:
                    NewValue = new byte[Variable.Elements * 4];
                    break;
                case Control.DataType.Float:
                    NewValue = new byte[Variable.Elements * 4];
                    break;
                default:
                    NewValue = new byte[Variable.Elements * 1];
                    break;
                }

                // get data from memory stamp
                for (int i = 0; i < NewValue.Length; i++)
                {
                    NewValue[i] = LatestMemoryStamp.RAM[Variable.Address + i];
                }

                Variable.SetValue(NewValue);
            }

            MainWindow.ListView_WatchList.Items.Refresh();
        }

        public void AddWatchListVariable(WatchListVariable Variable)
        {
            WatchList.Add(Variable);
            RefreshWatchListValues();
            MainWindow.ListView_WatchList.Items.Refresh();
        }

        public void SaveRAM()
        {
            Storage.SaveMemoryStamps(MemoryStamps);
            MainWindow.RefreshLoadableMemoryStamps();
            MessageBox.Show("RAM has been saved.");
        }

        public void LoadRAM(string Name)
        {
            MainWindow.ClearRAM();
            MemoryStamps = Storage.LoadMemoryStamps(Name);
            AddAllMemoryStamps();
            RefreshWatchListValues();
        }

        public void SaveWatchList()
        {
            Storage.SaveWatchList(WatchList);
            MainWindow.RefreshLoadableWatchLists();
            MessageBox.Show("Watch list has been saved.");
        }

        public void LoadWatchList(string Name)
        {
            MainWindow.ClearWatchList();
            WatchList = Storage.LoadWatchList(Name);
            RefreshWatchListValues();
            MainWindow.ListView_WatchList.ItemsSource = WatchList;
            MainWindow.ListView_WatchList.Items.Refresh();
        }


        public static string[] PortNameList() // returns an array of all port names
        {
            return SerialPort.GetPortNames();
        }
        public static string GetHexAddress(int Address) // converts an integer to Arduino specific memory address 
        {
            return "0x" + Convert.ToString(Address + 256, 16).PadLeft(4, '0').ToUpper();
        }

        public enum ApplicationStatus : byte
        {
            NoPort,
            NoBaudRate,
            InvalidBaudRate,
            Ready,
            Listening,
            Receiving
        }

        public enum DataType : byte
        {
            Boolean = 0,
            Byte = 1,
            Char = 2,
            UnsignedInt = 3,
            Int = 4,
            UnsignedLong = 5,
            Long = 6,
            Float = 7
        }
    }
}
