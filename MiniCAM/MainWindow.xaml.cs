using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace MiniCAM
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort sp = new SerialPort();

        public MainWindow()
        {
            InitializeComponent();
            cbx_Port.ItemsSource = SerialPort.GetPortNames();
//          lblMachineInfo.Content = SerialPort.GetPortNames();
        }

        private void btnMachineConnect_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
