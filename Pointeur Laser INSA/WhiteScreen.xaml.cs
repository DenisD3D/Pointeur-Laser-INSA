using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Pointeur_Laser_INSA
{
    /// <summary>
    /// Logique d'interaction pour WhiteScreen.xaml
    /// </summary>
    public partial class WhiteScreen : Window
    {
        public WhiteScreen()
        {
            InitializeComponent();
            KeyDown += new KeyEventHandler(WhiteBoard_KeyDown);
            MouseDown += new MouseButtonEventHandler(WhiteBoard_MouseDown);
        }

        private void WhiteBoard_KeyDown(object sender, KeyEventArgs e)
        {
            Close();
        }

        private void WhiteBoard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
