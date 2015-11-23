using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RAM_Viewer
{
    public partial class MainWindow : Window
    {
        // other windows
        HelpWindow HelpWindow;

        private Control Control;

        List<List<string>> TableAllData = new List<List<string>>(); // stores all data of RAM
        List<List<string>> TableReduced; // stores only the data of RAM which has changed

        private DisplayType CurrentDisplayType = DisplayType.Decimal; // defines the display type of the main table

        private bool RowsWithoutChangeHidden = false; // stores wether the all rows are shown or only those which include changes of data
        private bool InitializingComponent = true; // is only true until the InitializeComponent() function has finished

        private int Columns = 0; // stores how many columns of data have been added yet


        public MainWindow()
        {
            InitializeComponent();
            ComboBox_DisplayType.SelectedIndex = 1; // select decimal by default
            InitializingComponent = false;

            // initialize TablleAllData list and address selection combo box
            for (int i = 0; i < 2048; i++)
            {
                TableAllData.Add(new List<string>());
                TableAllData[i].Add(Control.GetHexAddress(i));
                ComboBox_WatchListAddress.Items.Add(Control.GetHexAddress(i));
            }

            // add address column to listview
            GridViewColumn AdressGridViewColumn = new GridViewColumn();
            Binding NewBinding = new Binding();
            NewBinding.Mode = BindingMode.OneWay;
            NewBinding.Path = new PropertyPath("[0]");
            AdressGridViewColumn.DisplayMemberBinding = NewBinding;
            AdressGridViewColumn.Header = "Address";
            GridView_RAM.Columns.Add(AdressGridViewColumn);
            

            Control = new Control(this); // bidirectional association between MainWindow and Control

            // os version label
            Label_FooterInfo.Content = "© Timo Denk (www.timodenk.com) 2015  ·  Arduino SRAM-Viewer (1.0.0)  ·  " + Environment.OSVersion.ToString();


            // bind data to list views
            ListView_RAM.ItemsSource = TableAllData;
            ListView_RAM.Items.Refresh();
            ListView_WatchList.ItemsSource = Control.WatchList;
            ListView_WatchList.Items.Refresh();

            RefreshLoadableMemoryStamps();
            RefreshLoadableWatchLists();
            RefreshButtonAddWatchListVariableIsEnabled();
        }

        public void RefreshPortList()
        {
            ComboBox_PortNames.SelectedIndex = -1;
            ComboBox_PortNames.Items.Clear();
            foreach (string PortName in Control.PortNameList())
            {
                ComboBox_PortNames.Items.Add(PortName);
            }
        }


        public void SetApplicationStatus(Control.ApplicationStatus NewStatus)
        { 
            string NewStatusContent;
            switch (NewStatus)
            {
                case Control.ApplicationStatus.NoPort:
                    NewStatusContent = "No port has been selected.";
                    break;
                case Control.ApplicationStatus.NoBaudRate:
                    NewStatusContent = "No baud rate has been given.";
                    break;
                case Control.ApplicationStatus.InvalidBaudRate:
                    NewStatusContent = "The given baud rate is invalid.";
                    break;
                case Control.ApplicationStatus.Ready:
                    NewStatusContent = "Ready to listen for incoming data.";
                    ProgressBar_Main.IsIndeterminate = false;
                    Label_BytesReceived.Content = null;
                    break;
                case Control.ApplicationStatus.Listening:
                    NewStatusContent = "Listening for data.";
                    ProgressBar_Main.IsIndeterminate = true;
                    ProgressBar_Main.Value = 0;
                    break;
                case Control.ApplicationStatus.Receiving:
                    NewStatusContent = "Receiving data.";
                    break;
                default:
                    NewStatusContent = "A unknown error occured.";
                    break;
            }

            // update toggle button enabled or not
            if (NewStatus != Control.ApplicationStatus.Ready && NewStatus != Control.ApplicationStatus.Listening && NewStatus != Control.ApplicationStatus.Receiving)
                Button_ToggleListen.IsEnabled = false;
            else
                Button_ToggleListen.IsEnabled = true;


            Label_ApplicationStatus.Content = NewStatusContent;
        } // application status management

        private void Button_RefreshPortNames_Click(object sender, RoutedEventArgs e) // refresh port comboxbox button event
        {
            RefreshPortList();
        }
        private void ComboBox_PortNames_SelectionChanged(object sender, SelectionChangedEventArgs e) // port comboxbox event
        { 
            Control.SetPortName((string)ComboBox_PortNames.SelectedValue); 
        }
        private void TextBox_BaudRate_TextChanged(object sender, TextChangedEventArgs e) // update baud rate textbox changed event
        {
            if (InitializingComponent) return; // default value may trigger this method before textbox actually exists (return in this case)

            string NewBaudRateText = TextBox_BaudRate.Text;

            if (NewBaudRateText.Length == 0)
            {
                Control.SetBaudRate(-1);
            }
            try
            {
                int NewBaudRateInt = Convert.ToInt32(NewBaudRateText);
                Control.SetBaudRate(NewBaudRateInt);
            }
            catch (FormatException)
            {
                Control.SetBaudRate(0);
            }
        }
        
        

        // disables or enables elements (buttons, etc.) 
        public void DisableConnectionSettings()
        {
            ComboBox_PortNames.IsEnabled = false;
            Button_RefreshPortNames.IsEnabled = false;
            TextBox_BaudRate.IsEnabled = false;
            ProgressBar_Main.IsEnabled = true;

            TextBlock_ToggleListen_Content.Text = "Close serial port";
        }
        public void EnableConnectionSettings()
        {
            ComboBox_PortNames.IsEnabled = true;
            Button_RefreshPortNames.IsEnabled = true;
            TextBox_BaudRate.IsEnabled = true;
            ProgressBar_Main.IsEnabled = false;

            TextBlock_ToggleListen_Content.Text = "Open serial port";
        }

        private void Button_ToggleListen_Click(object sender, RoutedEventArgs e) // toggle between an opened and a closed port
        {
            if (Control.ConnectionOpened)
                Control.CloseConnection();
            else
                Control.ListenForData();
        }

        public void SetProgress(double Progress)
        {
            if (ProgressBar_Main.IsIndeterminate)
                ProgressBar_Main.IsIndeterminate = false;
            ProgressBar_Main.Value = Progress;
        } // sets the progress of the main progress bar (value is element of [0;1])

        public void AddMemoryStamp(MemoryStamp MemoryStamp)
        {
            GridViewColumn GridViewColumn = new GridViewColumn();
            Binding NewBinding = new Binding();
            NewBinding.Mode = BindingMode.OneWay;
            NewBinding.Path = new PropertyPath("[" + (Columns + 1).ToString() + "]");
            GridViewColumn.DisplayMemberBinding = NewBinding;
            GridViewColumn.Header = Columns.ToString();
            for (int i = 0; i < MemoryStamp.TotalBytes; i++)
            {
                string Cell = "";
                switch (CurrentDisplayType)
                {
                    case DisplayType.Binary:
                        Cell = Convert.ToString(MemoryStamp.RAM[i], 2).PadLeft(8, '0');
                        break;
                    case DisplayType.Decimal:
                        Cell = MemoryStamp.RAM[i].ToString();
                        break;
                    case DisplayType.Hex:
                        Cell = Convert.ToString(MemoryStamp.RAM[i], 16).PadLeft(2, '0').ToUpper();
                        break;
                    case DisplayType.ASCII:
                        Cell = Regex.Escape(((char)MemoryStamp.RAM[i]).ToString());
                        break;
                    default:
                        break;
                }

                TableAllData[i].Add(Cell);
            }
            GridView_RAM.Columns.Add(GridViewColumn);

            ListView_RAM.Items.Refresh();
            Columns++;

            if (RowsWithoutChangeHidden)
                HideRowsWithoutChange();
        }

        private void HideRowsWithoutChange()
        {
            RowsWithoutChangeHidden = true;

            TableReduced = new List<List<string>>();

            int RowsAdded = 0;
            for (int i = 0; i < 2048; i++)
            {
                bool DifferenceInCurrentRow = false;
                for (int j = 2; j < TableAllData[i].Count; j++)
                {
                    if (TableAllData[i][j] != TableAllData[i][j - 1])
                    {
                        DifferenceInCurrentRow = true;
                    }
                }

                if (DifferenceInCurrentRow)
                {
                    TableReduced.Add(new List<string>());
                    for (int j = 0; j < TableAllData[i].Count; j++)
                    {
                        TableReduced[RowsAdded].Add(TableAllData[i][j]);
                    }
                    RowsAdded++;
                }
            }

            ListView_RAM.ItemsSource = TableReduced;
            ListView_RAM.Items.Refresh();
        } // hides all rows which don't have differences
        private void ShowAllRows()
        {
            RowsWithoutChangeHidden = false;

            ListView_RAM.ItemsSource = TableAllData;
            ListView_RAM.Items.Refresh();
        } // shows all rows (not only those with differences)

        private void CheckBox_HideRowsWithoutChange_Checked(object sender, RoutedEventArgs e) { HideRowsWithoutChange(); } // hide rows without change event
        private void CheckBox_HideRowsWithoutChange_Unchecked(object sender, RoutedEventArgs e) { ShowAllRows(); } // show rows without change event

        private void SetRAMDisplayType(DisplayType DisplayType)
        {
            CurrentDisplayType = DisplayType;

            // remove table data
            for (int i = 0; i < MemoryStamp.TotalBytes; i++)
            {
                if (TableAllData[i].Count > 1)
                    TableAllData[i].RemoveRange(1, TableAllData[i].Count - 1);
            }

            // remove user interface table columns (except from the first one which is the address column)
            while (GridView_RAM.Columns.Count > 1)
                GridView_RAM.Columns.RemoveAt(1);

            Columns = 0;

            Control.AddAllMemoryStamps();
        } // change display type (e.g. 0101001 or D3, etc.)
        private void ComboBox_DisplayType_SelectionChanged(object sender, SelectionChangedEventArgs e) // change display type event
        { 
            if (InitializingComponent) return; // default value may trigger this method before textbox actually exists (return in this case)
            SetRAMDisplayType((DisplayType)ComboBox_DisplayType.SelectedIndex);
        }

        private enum DisplayType : byte
        {
            Binary,
            Decimal,
            Hex,
            ASCII
        }

        private void TextBox_MinimumTimeBetweenMemoryStamps_TextChanged(object sender, TextChangedEventArgs e) // minimum time between memory stamps
        {
            if (InitializingComponent) return; // default value may trigger this method before textbox actually exists (return in this case)

            string NewTime = TextBox_MinimumTimeBetweenMemoryStamps.Text;

            if (NewTime.Length == 0)
            {
                Control.MillisecondsBetweenMemoryStamps = 0;
            }
            try
            {
                int NewTimeInt = Convert.ToInt32(NewTime);
                if (NewTimeInt >= 0)
                {
                    Control.MillisecondsBetweenMemoryStamps = NewTimeInt;
                }
            }
            catch (FormatException)
            {
                Control.MillisecondsBetweenMemoryStamps = 0;
            }
        }




        // watchlist stuff

        private void Button_WatchListAddVariable_Click(object sender, RoutedEventArgs e) // add watch list variable
        {
            try
            {
                string Name = TextBox_WatchListName.Text;
                Control.DataType DataType = (Control.DataType)ComboBox_WatchListDataType.SelectedIndex;
                int Address = ComboBox_WatchListAddress.SelectedIndex;
                int Elements = Convert.ToInt32(Textbox_WatchListElements.Text);

                if (Name.Length > 0 && Elements > 0)
                {
                    Control.AddWatchListVariable(new WatchListVariable(Name, Address, Elements, DataType));

                    TextBox_WatchListName.Text = null;
                    ComboBox_WatchListDataType.SelectedIndex = -1;
                    ComboBox_WatchListAddress.SelectedIndex = -1;
                    Textbox_WatchListElements.Text = "1";

                    RefreshButtonAddWatchListVariableIsEnabled();
                }
            }
            catch (Exception)
            {
                RefreshButtonAddWatchListVariableIsEnabled();
                return;
            }
        }

        private void ListView_RAM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListView_RAM.SelectedIndex == -1) return;
            ComboBox_WatchListAddress.SelectedValue = ((List<string>)ListView_RAM.SelectedItem)[0];
            Textbox_WatchListElements.Text = ListView_RAM.SelectedItems.Count.ToString();
        }

        public void ClearRAM()
        {
            // reset
            Control.MemoryStamps = new List<MemoryStamp>();

            while (GridView_RAM.Columns.Count > 1)
            {
                GridView_RAM.Columns.RemoveAt(1);
            }

            Columns = 0;
        }

        private void Buttom_ClearRAM_Click(object sender, RoutedEventArgs e)
        {
            ClearRAM();
        }

        public void RefreshLoadableMemoryStamps()
        {
            foreach (string Name in Storage.AvailableMemoryStamps())
            {
                ComboBox_LoadRAM.Items.Add(Name.Substring(Storage.MemoryStampsPath.Length).Replace(Storage.Extension, ""));
            }
        }

        public void RefreshLoadableWatchLists()
        {
            foreach (string Name in Storage.AvailableWatchLists())
            {
                ComboBox_LoadWatchList.Items.Add(Name.Substring(Storage.WatchListsPath.Length).Replace(Storage.Extension, ""));
            }
        }

        private void Button_SaveRAM_Click(object sender, RoutedEventArgs e)
        {
            Control.SaveRAM();
        }

        private void ComboBox_LoadRAM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_LoadRAM.SelectedIndex == -1) return;

            Control.LoadRAM((string)ComboBox_LoadRAM.SelectedItem);
            ComboBox_LoadRAM.SelectedIndex = -1;
        }

        private void ComboBox_LoadWatchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_LoadWatchList.SelectedIndex == -1) return;

            Control.LoadWatchList((string)ComboBox_LoadWatchList.SelectedItem);
            ComboBox_LoadWatchList.SelectedIndex = -1;
        }

        private void Button_SaveWatchList_Click(object sender, RoutedEventArgs e)
        {
            Control.SaveWatchList();
        }

        public void ClearWatchList()
        {
            Control.WatchList = new List<WatchListVariable>();
            ListView_WatchList.ItemsSource = Control.WatchList; // re-bind
            ListView_WatchList.Items.Refresh();
        }

        private void Button_ClearWatchList_Click(object sender, RoutedEventArgs e)
        {
            ClearWatchList();
        }

        private void ListView_WatchList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // not enabled yet
            return;

            ListView ListView = sender as ListView;
            GridView GridView = ListView.View as GridView;

            double Name = 0.40, Value = 0.60, Address = 50;

            double WorkingWidth = ListView.ActualWidth - 35 - Address; // take into account vertical scrollbar

            GridView.Columns[0].Width = WorkingWidth * Name;
            GridView.Columns[1].Width = WorkingWidth * Value;
            GridView.Columns[2].Width = Address;
        }

        private void ListView_WatchList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                if (ListView_WatchList.SelectedIndex == -1) return;
                DeleteWatchListVariable(ListView_WatchList.SelectedIndex);
            }
        }

        public void DeleteWatchListVariable(int Index)
        {
            Control.WatchList.RemoveAt(Index);
            ListView_WatchList.Items.Refresh();
        }

        private void Textbox_MinimumTimeBytes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InitializingComponent) return; // default value may trigger this method before textbox actually exists (return in this case)

            string NewTime = Textbox_MinimumTimeBytes.Text;

            if (NewTime.Length == 0)
            {
                Control.MicrosecondsBetweenBytes = 0;
            }
            try
            {
                int NewTimeInt = Convert.ToInt32(NewTime);
                if (NewTimeInt >= 0)
                {
                    Control.MicrosecondsBetweenBytes = NewTimeInt;
                }
            }
            catch (FormatException)
            {
                Control.MicrosecondsBetweenBytes = 0;
            }
        }

        public void RefreshButtonAddWatchListVariableIsEnabled()
        {
            try
            {
                if (TextBox_WatchListName.Text != null && ComboBox_WatchListDataType.SelectedIndex != -1 && ComboBox_WatchListAddress.SelectedIndex != -1 && Textbox_WatchListElements.Text != null)
                    Button_WatchListAddVariable.IsEnabled = true;
                else
                    Button_WatchListAddVariable.IsEnabled = false;
            }
            catch (Exception)
            {
                return;
            }
        }

        private void TextBox_WatchListName_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshButtonAddWatchListVariableIsEnabled();
        }

        private void ComboBox_WatchListDataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshButtonAddWatchListVariableIsEnabled();
        }

        private void ComboBox_WatchListAddress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshButtonAddWatchListVariableIsEnabled();
        }

        private void Textbox_WatchListElements_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshButtonAddWatchListVariableIsEnabled();
        }



        // help window
        private void Button_Help_Click(object sender, RoutedEventArgs e)
        {
            OpenHelpWindow();
        }

        private void OpenHelpWindow()
        {
            if (HelpWindow != null && HelpWindow.IsVisible)
            {
                HelpWindow.Focus();
                return;
            }
            HelpWindow = new HelpWindow();
            HelpWindow.Show();
        }

        private void Window_MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (HelpWindow != null && HelpWindow.IsVisible)
            {
                HelpWindow.Close();
            }
        }

        private void Window_MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F1)
            {
                OpenHelpWindow();
            }
        }
    }
}