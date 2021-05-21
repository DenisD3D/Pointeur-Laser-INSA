using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
        bool connection_validated = false, need_cursor_update = false;
        Thread connectThread, cursorThread;
        InputSimulator inputSimulator = new InputSimulator();
        int next_X, next_Y;

        public MainWindow()
        {
            InitializeComponent(); 
            ListData.Add(new ActionData { Id = "none", Display = "Aucun" });
            ListData.Add(new ActionData { Id = "key", Display = "Assigner une touche du clavier" });
            ListData.Add(new ActionData { Id = "black_screen", Display = "Afficher un écran blanc" });
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
            }

            string port = "";
            if (portComboBox.SelectedValue != null) port = portComboBox.SelectedValue.ToString();
            consoleTextBox.Text = "Attempting connection to " + port + "\n";
            bluetoothManager = new BluetoothManager(port, Dispatcher, onData, onError);

            connectThread = new Thread(Connect);
            connectThread.Start();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
            {
                bluetoothManager.close();
            }
            Settings1.Default.Save();
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

        private void ActionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = e.Source as ComboBox;
            Settings1.Default["B" + comboBox.Tag + "Action"] = comboBox.SelectedValue.ToString();

            UpdateKeySelectButtonVisibility(comboBox.Tag.ToString(), comboBox.SelectedValue != null && comboBox.SelectedValue.ToString() == "key");
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
                inputSimulator.Keyboard.KeyPress((VirtualKeyCode)Settings1.Default.B1Key);
            }
            else if (message == "ILP+B2=0\r")
            {
                //LinearSmoothMove(new System.Drawing.Point(100, 100), 10);
            }
            else if (message == "ILP+B3=0\r")
            {
                MessageBox.Show("b3 pressed", "BTN pressed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (message == "ILP+B4=0\r")
            {
                MessageBox.Show("b4 pressed", "BTN pressed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (message == "ILP+B5=0\r")
            {
                inputSimulator.Mouse.LeftButtonClick();
            }

            consoleTextBox.Text += message + "\n";
            consoleTextBox.PageDown();
        }

        public void CursorMove()
        {
            while (need_cursor_update)
            {
                need_cursor_update = false;
                LinearSmoothMove(new System.Drawing.Point(next_X, next_Y));
            }
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
                    return;
            }
            consoleTextBox.Text += "No answer\n";
        }
    }
}
