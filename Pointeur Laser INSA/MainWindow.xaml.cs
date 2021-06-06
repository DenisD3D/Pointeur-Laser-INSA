using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;

namespace Pointeur_Laser_INSA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BluetoothManager bluetoothManager;
        List<ActionData> ListData = new List<ActionData>();
        bool connection_validated = false, need_cursor_update = false, deadzone_need_wait = false;
        Thread connectThread, cursorThread, deadzoneThread;
        InputSimulator inputSimulator = new InputSimulator();
        int next_X, next_Y, deadzoneValue;
        WhiteScreen whiteScreen = null;
        bool notrigger;

        public MainWindow()
        {
            InitializeComponent(); 
            ListData.Add(new ActionData { Id = "none", Display = "Aucun" });
            ListData.Add(new ActionData { Id = "laser_push", Display = "Activer le laser" });
            ListData.Add(new ActionData { Id = "laser_toggle", Display = "Interrupteur du laser" });
            ListData.Add(new ActionData { Id = "key", Display = "Assigner une touche du clavier" });
            ListData.Add(new ActionData { Id = "left_click", Display = "Faire un clic gauche" });
            ListData.Add(new ActionData { Id = "right_click", Display = "Faire un clic droit" });
            ListData.Add(new ActionData { Id = "white_screen", Display = "Afficher un écran blanc" });
            ListData.Add(new ActionData { Id = "file", Display = "Exécuter un script" });
        }
        
        //Home page
        private void portComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateComPorts();
        }

        private void refreshComPortsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateComPorts();
        }

        private void UpdateComPorts()
        {
            portComboBox.Items.Clear();
            foreach (string port in BluetoothManager.GetComsPorts())
            {
                portComboBox.Items.Add(port);
                if (port == Settings1.Default.Port)
                {
                    portComboBox.SelectedValue = port;
                }
            }
        }

        private void portComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (portComboBox.SelectedValue != null)
                Settings1.Default.Port = portComboBox.SelectedValue.ToString();
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
            {
                bluetoothManager.close();
                consoleTextBox.Text += "Déconnecté du pointeur laser\n";
                connectButton.Content = " Connecter ";
            }
            else
            {
                if (portComboBox.SelectedValue != null)
                {
                    string port = portComboBox.SelectedValue.ToString();
                    consoleTextBox.Text = "Tentative de connexion à " + port + "\n";
                    bluetoothManager = new BluetoothManager(port, Dispatcher, onData, onError);

                    connectThread = new Thread(Connect);
                    connectThread.Start();

                    connectButton.Content = " Déconnecter ";
                }
            }
            
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
            {
                bluetoothManager.close();
            }
            Settings1.Default.Save();
            Application.Current.Shutdown();
        }
        
        // Actions setup page
        private void KeySelectButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = e.Source as Button;
            b.Content = "<Press key>";
        }

        private void KeySelectButton_KeyDown(object sender, KeyEventArgs e)
        {
            Button b = e.Source as Button;
            int keycode = System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key);
            b.Content = e.Key.ToString();
            Settings1.Default["B" + b.Tag + "Key"] = keycode;

            e.Handled = true; // Do not process the default beahivor of the key
            Keyboard.ClearFocus(); // Remove focus on the button
        }

        private void KeySelectButton_Loaded(object sender, RoutedEventArgs e)
        {
            Button b = e.Source as Button;
            b.Content = System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)Settings1.Default["B" + b.Tag + "Key"]).ToString();
        }

        private void ActionBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = e.Source as ComboBox;
            comboBox.ItemsSource = ListData;
            comboBox.DisplayMemberPath = "Display";
            comboBox.SelectedValuePath = "Id";
            comboBox.SelectedValue = Settings1.Default["B" + comboBox.Tag + "Action"];

            UpdateKeySelectButtonVisibility(comboBox.Tag.ToString(), comboBox.SelectedValue != null && comboBox.SelectedValue.ToString() == "key");
        }

        private void UpdateKeySelectButtonVisibility(string tag, Boolean is_visible)
        {
            Button b = (Button)this.FindName("keySelectButton_" + tag);
            b.Visibility = is_visible ? Visibility.Visible : Visibility.Hidden;
        }
        private void UpdateFileSelectButtonVisibility(string tag, Boolean is_visible)
        {
            Button b = (Button)this.FindName("fileSelectButton_" + tag);
            b.Visibility = is_visible ? Visibility.Visible : Visibility.Hidden;
        }

        private void ActionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = e.Source as ComboBox;
            Settings1.Default["B" + comboBox.Tag + "Action"] = comboBox.SelectedValue.ToString();

            UpdateKeySelectButtonVisibility(comboBox.Tag.ToString(), comboBox.SelectedValue != null && comboBox.SelectedValue.ToString() == "key");
            UpdateFileSelectButtonVisibility(comboBox.Tag.ToString(), comboBox.SelectedValue != null && comboBox.SelectedValue.ToString() == "file");

            if (comboBox.SelectedValue != null && (comboBox.SelectedValue.ToString() == "laser_push" || comboBox.SelectedValue.ToString() == "laser_toggle"))
            {
                for (int i = 1; i <= 5; i++)
                {
                    notrigger = true;
                    ComboBox target = (ComboBox)FindName("selectActionBox_" + i);
                    if (comboBox.Tag.ToString() != target.Tag.ToString() && target.SelectedValue != null && (target.SelectedValue.ToString() == "laser_push" || target.SelectedValue.ToString() == "laser_toggle"))
                    {
                        ((ComboBox)FindName("selectActionBox_" + i)).SelectedIndex = 0;
                    }
                    notrigger = false;
                }

                if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
                {
                    bluetoothManager.Write("ILP+UPDATELASER=" + (comboBox.SelectedValue.ToString() == "laser_push" ? "1" : "2") + "," + comboBox.Tag.ToString());
                }
            }
            else if (!notrigger && (e.RemovedItems.Contains(ListData.Find(x => x.Id == "laser_push")) || e.RemovedItems.Contains(ListData.Find(x => x.Id == "laser_toggle"))))
            {
                if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
                {
                    bluetoothManager.Write("ILP+UPDATELASER=0,0");
                }
            }
        }

        public class ActionData
        {
            public string Id { get; set; }
            public string Display { get; set; }
        }

        // Bluetooth methods
        public void onData(string message)
        {
            if (message == "OK\r")
            {
                connection_validated = true;
                consoleTextBox.Text += "Connection successfull\n";

                bool anySelected = false;
                for (int i = 1; i <= 5; i++)
                {
                    if (Settings1.Default["B" + i + "Action"].ToString() == "laser_push" || Settings1.Default["B" + i + "Action"].ToString() == "laser_toggle")
                    {
                        bluetoothManager.Write("ILP+UPDATELASER=" + (Settings1.Default["B" + i + "Action"].ToString() == "laser_push" ? "1" : "2") + "," + i);
                        anySelected = true;
                    }
                }
                if (!anySelected)
                {
                    bluetoothManager.Write("ILP+UPDATELASER=0,0");
                }
                return; // Do not log "OK"
            } 
            else if(message.StartsWith("ILP+JOYSTICK"))
            {
                string[] pos = message.Substring(13).Split(",");
                next_X = int.Parse(pos[0]);
                next_Y = int.Parse(pos[1]);
                need_cursor_update = true;

                if (cursorThread == null || !cursorThread.IsAlive)
                {
                    cursorThread = new Thread(CursorMove);
                    cursorThread.Start();
                }
            }
            else if (message == "ILP+B1=0\r")
            {
                ProcessButtonPress(1);
            }
            else if (message == "ILP+B1=1\r")
            {
                ProcessButtonRelease(1);
            }
            else if (message == "ILP+B2=0\r")
            {
                ProcessButtonPress(2);
            }
            else if (message == "ILP+B2=1\r")
            {
                ProcessButtonRelease(2);
            }
            else if (message == "ILP+B3=0\r")
            {
                ProcessButtonPress(3);
            }
            else if (message == "ILP+B3=1\r")
            {
                ProcessButtonRelease(3);
            }
            else if (message == "ILP+B4=0\r")
            {
                ProcessButtonPress(4);
            }
            else if (message == "ILP+B4=1\r")
            {
                ProcessButtonRelease(4);
            }
            else if (message == "ILP+B5=0\r")
            {
                ProcessButtonPress(5);
            }
            else if (message == "ILP+B5=1\r")
            {
                ProcessButtonRelease(5);
            }

            consoleTextBox.Text += message + "\n";
            consoleTextBox.PageDown();
        }

        private void ProcessButtonPress(int buttonId)
        {
            switch (Settings1.Default["B" + buttonId + "Action"])
            {
                case "key":
                    inputSimulator.Keyboard.KeyDown((VirtualKeyCode)Settings1.Default["B" + buttonId + "Key"]);
                    break;
                case "left_click":
                    inputSimulator.Mouse.LeftButtonDown();
                    break;
                case "right_click":
                    inputSimulator.Mouse.RightButtonDown();
                    break;
                case "white_screen":
                    if(whiteScreen == null)
                    {
                        whiteScreen = new WhiteScreen();
                        whiteScreen.Closed += (sender, args) => whiteScreen = null;
                        whiteScreen.Show();
                    }
                    else
                    {
                        whiteScreen.Close();
                        whiteScreen = null;
                    }
                    break;
                case "file":
                    ProcessStartInfo processInfo;
                    Process process;
                    processInfo = new ProcessStartInfo("cmd.exe", "/c " + Settings1.Default["B" + buttonId + "File"].ToString());
                    processInfo.CreateNoWindow = true;
                    processInfo.UseShellExecute = false;
                    process = Process.Start(processInfo);
                    break;
                case "none":
                case "default":
                    break;
            }
        }
        private void ProcessButtonRelease(int buttonId)
        {
            switch (Settings1.Default["B" + buttonId + "Action"])
            {
                case "key":
                    inputSimulator.Keyboard.KeyUp((VirtualKeyCode)Settings1.Default["B" + buttonId + "Key"]);
                    break;
                case "left_click":
                    inputSimulator.Mouse.LeftButtonUp();
                    break;
                case "right_click":
                    inputSimulator.Mouse.RightButtonUp();
                    break;
                case "none":
                case "default":
                    break;
            }
        }

        public void CursorMove()
        {
            while (need_cursor_update)
            {
                need_cursor_update = false;
                LinearSmoothMove(new System.Drawing.Point(next_X, next_Y));
            }
        }

        private void deadzoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            deadzoneValue = (int)deadzoneSlider.Value;
            if (Settings1.Default.Deadzone == (int)deadzoneSlider.Value)
                return;


            Settings1.Default.Deadzone = (int)deadzoneSlider.Value;

            deadzone_need_wait = true;

            if (deadzoneThread == null || !deadzoneThread.IsAlive)
            {
                deadzoneThread = new Thread(SendDeadzoneUpdate);
                deadzoneThread.Start();
            }
        }

        private void fileSelectButton_Loaded(object sender, RoutedEventArgs e)
        {
            Button b = e.Source as Button;
            b.Content = Win32.PathShortener(Settings1.Default["B" + b.Tag + "File"].ToString(), 40);
        }

        private void fileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = e.Source as Button;
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                Settings1.Default["B" + b.Tag + "File"] = openFileDialog.FileName;
                b.Content = Win32.PathShortener(openFileDialog.FileName, 40);
            }
        }

        private void deadzoneSlider_Loaded(object sender, RoutedEventArgs e)
        {
            deadzoneSlider.Value = Settings1.Default.Deadzone;
        }

        public void LinearSmoothMove(System.Drawing.Point addPosition)
        {
            Win32.POINT start;
            if(Win32.GetCursorPos(out start))
            {
                PointF iterPoint = new PointF(start.X, start.Y);

                PointF newPosition = new PointF(start.X + addPosition.X, start.Y + addPosition.Y);

                // Find the slope of the line segment defined by start and newPosition
                PointF slope = new PointF((float)(newPosition.X - start.X), (float)(newPosition.Y - start.Y));

                int steps = 10;
                // Divide by the number of steps
                slope.X = slope.X / steps;
                slope.Y = slope.Y / steps;

                // Move the mouse to each iterative point.
                for (int i = 0; i < steps; i++)
                {
                    iterPoint = new PointF(iterPoint.X + slope.X, iterPoint.Y + slope.Y);
                    System.Drawing.Point final = System.Drawing.Point.Round(iterPoint);
                    Win32.SetCursorPos(final.X, final.Y);
                    Thread.Sleep(10);
                }

                // Move the mouse to the final destination.
                Win32.SetCursorPos((int)newPosition.X, (int)newPosition.Y);
            }
        }

        public void onError(string message)
        {
            MessageBox.Show(message, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Connect(object obj)
        {
            for (int i = 0; i < 50; i++)
            {
                bluetoothManager.Write("ILP");
                Thread.Sleep(2000);
                if (connection_validated || bluetoothManager == null || !bluetoothManager._continue || !bluetoothManager._serialPort.IsOpen)
                {
                    return;
                }
            }
            consoleTextBox.Text += "No answer\n";
        }

        private void SendDeadzoneUpdate()
        {
            while(deadzone_need_wait)
            {
                deadzone_need_wait = false;
                Thread.Sleep(1500);
            }


            if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
            {
                bluetoothManager.Write("ILP+DEADZONE=" + deadzoneValue);
            }

        }
    }
}
