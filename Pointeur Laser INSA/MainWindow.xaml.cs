using System;
using System.Collections.Generic;
using System.ComponentModel;
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


        public MainWindow()
        {
            InitializeComponent(); 
            ListData.Add(new ActionData { Id = "none", Display = "None" });
            ListData.Add(new ActionData { Id = "key", Display = "Simulate key press" });
            ListData.Add(new ActionData { Id = "black_screen", Display = "Display a black screen" });
            ListData.Add(new ActionData { Id = "file", Display = "Execute a custom script" });

        }

        private void portComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            foreach(string port in BluetoothManager.GetComsPorts())
            {
                portComboBox.Items.Add(port);
                if(port == Settings1.Default.Port)
                {
                    portComboBox.SelectedValue = port;
                }
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
            {
                bluetoothManager.close();
            }

            string port = "";
            if (portComboBox.SelectedValue != null) port = portComboBox.SelectedValue.ToString();
            bluetoothManager = new BluetoothManager(port, Dispatcher, onData, onError);
        }

        public void onData(string message)
        {
            test.Content = message;
            
            if(message == "ILP+B1=0\r") 
            {
                InputSimulator inputSimulator = new InputSimulator();
                inputSimulator.Keyboard.KeyPress((VirtualKeyCode)Settings1.Default.B1Key);
            }
            else if (message == "ILP+B2=0\r")
            {
                MessageBox.Show("b2 pressed", "BTN pressed", MessageBoxButton.OK, MessageBoxImage.Information);

            } else if (message == "ILP+B3=0\r")
            {
                MessageBox.Show("b3 pressed", "BTN pressed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void onError(string message)
        {
            MessageBox.Show(message, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
            {
                bluetoothManager.close();
            }
            Settings1.Default.Save();
        }
        private void portComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Settings1.Default.Port = portComboBox.SelectedValue.ToString();
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

        private void ActionBox_Loaded_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
    }
}
