using System;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;

namespace Pointeur_Laser_INSA
{
    class BluetoothManager
    {
        public string port;
        private readonly Dispatcher dispatcher;
        public SerialPort _serialPort;
        public bool _continue;
        private Thread readThread;
        private Action<string> onData;
        private Action<string> onError;

        public BluetoothManager(string port, System.Windows.Threading.Dispatcher dispatcher, Action<string> onData, Action<string> onError)
        {
            this.port = port;
            this.dispatcher = dispatcher;
            this.onData = onData;
            this.onError = onError;
            readThread = new Thread(Read);

            try
            {
                _serialPort = new SerialPort(port, 9600);
                _serialPort.ReadTimeout = 500;
                _serialPort.WriteTimeout = 500;
                _serialPort.Open();

                _continue = true;
                readThread.Start();
            }
            catch (Exception ex)
            {
                _continue = false;
                dispatcher.Invoke(onError, "Can't connect : " + ex.Message);
            }

        }       

        public void Read()
        {
            while (_continue)
            {
                try
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        string message = _serialPort.ReadLine();
                        dispatcher.Invoke(onData, message);
                    }
                }
                catch (TimeoutException) { }
                catch (OperationCanceledException)
                {
                    _continue = false;
                }
            }
        }

        public static string[] GetComsPorts()
        {
            return SerialPort.GetPortNames();
        }

        public void Write(string s)
        {
            try
            {
                _serialPort.Write(s);
            }
            catch
            {
                _continue = false;
                dispatcher.Invoke(onError, "Connection timed out");
            }
        }

        public void close()
        {
            _continue = false;
            readThread.Join();
            _serialPort.Close();
        }
    }
}
