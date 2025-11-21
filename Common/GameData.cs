using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class GameData
    {
        public ViewModelGames PlayerData { get; set; }
        public List<ViewModelGames> OtherPlayersData { get; set; }
    }
}