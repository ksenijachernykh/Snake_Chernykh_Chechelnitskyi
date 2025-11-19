using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snake_Chernykh_Chechelnitskyi
{
    class Program
    {
        public static List<Leadres> Leadres = new List<Leadres>();
        public static List<ViewModelUserSettings> remoteIPAddress = new List<ViewModelUserSettings>();
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
        private static int localPort = 5001;
        public static int MaxSpeed = 15;
        static void Main(string[] args)
        {
        }
    }
}
