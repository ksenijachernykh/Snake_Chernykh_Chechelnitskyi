using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
using Newtonsoft.Json;

namespace SnakeWPF.Pages
{
    /// <summary>
    /// Логика взаимодействия для Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        public Home()
        {
            InitializeComponent();
        }
        private void StartGame(object sender, RoutedEventArgs e)
        {
            if (MainWindow.mainWindow.receivingUdpClient != null)
            {
                MainWindow.mainWindow.receivingUdpClient.Close();
            }
            if (MainWindow.mainWindow.tRec != null)
            {
                MainWindow.mainWindow.tRec.Abort();
            }
            IPAddress UserIpAdress;
            if (!IPAddress.TryParse(ip.Text, out UserIpAdress))
            {
                MessageBox.Show("Please use the IP adress in the format x.x.x.x.");
                return;
            }
            int UserPort;
            if (!int.TryParse(port.Text, out UserPort))
            {
                MessageBox.Show("Please use the port is a number");
                return;
            }
            MainWindow.mainWindow.StartReceiver();
            MainWindow.mainWindow.ViewModelUserSettings.IPAddress = ip.Text;
            MainWindow.mainWindow.ViewModelUserSettings.Port = port.Text;
            MainWindow.mainWindow.ViewModelUserSettings.Name = name.Text;
            MainWindow.Send("/start|" + JsonConvert.SerializeObject(MainWindow.mainWindow.ViewModelUserSettings));
        }
    }
}