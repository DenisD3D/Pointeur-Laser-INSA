using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

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

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            label1.Content = "Available Com port : ";
            foreach (string port in BluetoothManager.GetComsPorts())
            {
                label1.Content += port + " ";
            }
            label1.Content += "\n";
        }

        public void onData(string message)
        {
            if (message.StartsWith("ILP+JOYSTICK="))
                return;
            label1.Content += message + "\n";
        }

        public void onError(string message)
        {
            label1.Content += message + "\n";
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            bluetoothManager.close();
        }

        private void textbox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (bluetoothManager == null || !bluetoothManager._continue || !bluetoothManager._serialPort.IsOpen)
                {
                    label1.Content += "Connection to " + textbox1.Text + "\n";
                    bluetoothManager = new BluetoothManager(textbox1.Text, Dispatcher, onData, onError);
                }
                else {
                    bluetoothManager.Write(textbox1.Text);
                }
                textbox1.Text = "";
            }
        }
    }
}
