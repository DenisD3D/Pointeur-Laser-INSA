using System.ComponentModel;
using System.Windows;
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

        public MainWindow()
        {
            InitializeComponent();
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

        private void button1_key_Click(object sender, RoutedEventArgs e)
        {
            button1_key.Content = "<Press key>";
        }

        private void button1_key_KeyDown(object sender, KeyEventArgs e)
        {
            int keycode = System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key);
            button1_key.Content = e.Key.ToString();
            Settings1.Default.B1Key = keycode;
        }

        private void button1_key_Loaded(object sender, RoutedEventArgs e)
        {
            button1_key.Content = System.Windows.Input.KeyInterop.KeyFromVirtualKey(Settings1.Default.B1Key).ToString();
        }
    }
}
