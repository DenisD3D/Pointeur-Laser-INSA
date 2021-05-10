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

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            foreach(string port in BluetoothManager.GetComsPorts())
            {
                PortComboBox.Items.Add(port);
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
            {
                bluetoothManager.close();
            }

            string port = "";
            if (PortComboBox.SelectedValue != null) port = PortComboBox.SelectedValue.ToString();
            bluetoothManager = new BluetoothManager(port, Dispatcher, onData, onError);
        }

        public void onData(string message)
        {
            test.Content = message;
            
            if(message=="ILP+B1=0\r") 
            {
                MessageBox.Show("b1 pressed", "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
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
        }

        /*
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
  if (bluetoothManager != null && bluetoothManager._continue && bluetoothManager._serialPort.IsOpen)
  {
      bluetoothManager.close();
  }
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

private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{

}*/
    }
}
