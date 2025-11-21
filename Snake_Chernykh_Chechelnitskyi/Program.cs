using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;

namespace Snake_Chernykh_Chechelnitskyi
{
    class Program
    {
        public static List<Leaders> Leaders = new List<Leaders>();
        public static List<ViewModelUserSettings> remoteIPAddress = new List<ViewModelUserSettings>();
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
        private static int localPort = 5001;
        public static int MaxSpeed = 15;
        private static void Send()
        {
            List<ViewModelUserSettings> usersToSend;
            lock (remoteIPAddress)
            {
                usersToSend = new List<ViewModelUserSettings>(remoteIPAddress);
            }

            foreach (ViewModelUserSettings user in usersToSend)
            {
                try
                {
                    using (UdpClient sender = new UdpClient())
                    {
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(user.IPAddress), int.Parse(user.Port));

                        var playerData = viewModelGames.Find(x => x.IdSnake == user.IdSnake);
                        var otherPlayersData = viewModelGames.FindAll(x => x.IdSnake != user.IdSnake);

                        if (playerData != null)
                            playerData.PlayerName = user.Name;

                        foreach (var otherPlayer in otherPlayersData)
                        {
                            var userInfo = remoteIPAddress.Find(x => x.IdSnake == otherPlayer.IdSnake);
                            if (userInfo != null)
                                otherPlayer.PlayerName = userInfo.Name;
                        }

                        var gameData = new GameData
                        {
                            PlayerData = playerData,
                            OtherPlayersData = otherPlayersData
                        };

                        string jsonData = JsonConvert.SerializeObject(gameData);
                        byte[] gameDataBytes = Encoding.UTF8.GetBytes(jsonData);
                        sender.Send(gameDataBytes, gameDataBytes.Length, endPoint);

                        Console.WriteLine($"Отправлены данные игроку {user.Name} ({user.IPAddress}:{user.Port})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки данных игроку {user.Name}: {ex.Message}");

                    if (ex is SocketException)
                    {
                        var playerToRemove = viewModelGames.Find(x => x.IdSnake == user.IdSnake);
                        if (playerToRemove != null)
                        {
                            playerToRemove.SnakesPlayers.GameOver = true;
                        }
                    }
                }
            }
        }
        public static void LoadLeaders()
        {
            if (File.Exists("./leaders.txt"))
            {
                StreamReader SR = new StreamReader("./leaders.txt");
                string json = SR.ReadLine();
                SR.Close();
                if (!string.IsNullOrEmpty(json))
                {
                    Leaders = JsonConvert.DeserializeObject<List<Leaders>>(json);
                }
                else
                {
                    Leaders = new List<Leaders>();
                }
            }
            else
            {
                Leaders = new List<Leaders> { };
            }
        }
        public static void SaveLeaders()
        {
            string json = JsonConvert.SerializeObject(Leaders);
            StreamWriter SW = new StreamWriter("./leaders.txt");
            SW.WriteLine(json);
            SW.Close();
        }
        public static int AddSnake()
        {
            ViewModelGames viewModelGamesPlayer = new ViewModelGames();
            viewModelGamesPlayer.SnakesPlayers = new Snakes()
            {
                Points = new List<Snakes.Point>()
        {
            new Snakes.Point() {X = 30, Y = 10 },
            new Snakes.Point() {X = 20, Y = 10 },
            new Snakes.Point() {X = 10, Y = 10 },
        },
                direction = Snakes.Direction.Right 
            };
            viewModelGamesPlayer.Points = new Snakes.Point(new Random().Next(10, 783), new Random().Next(10, 410));
            viewModelGames.Add(viewModelGamesPlayer);
            return viewModelGames.FindIndex(x => x == viewModelGamesPlayer);
        }
        public static void Timer()
        {
            LoadLeaders();

            while (true)
            {
                try
                {
                    Thread.Sleep(100);

                    List<ViewModelGames> gamesCopy;
                    List<ViewModelUserSettings> usersCopy;

                    lock (viewModelGames)
                        lock (remoteIPAddress)
                        {
                            gamesCopy = new List<ViewModelGames>(viewModelGames);
                            usersCopy = new List<ViewModelUserSettings>(remoteIPAddress);
                        }

                    var deadPlayers = gamesCopy.Where(x => x.SnakesPlayers.GameOver).ToList();
                    foreach (var deadPlayer in deadPlayers)
                    {
                        var user = usersCopy.Find(x => x.IdSnake == deadPlayer.IdSnake);
                        if (user != null)
                        {
                            Console.WriteLine($"Игрок отключен: {user.Name}");

                            lock (remoteIPAddress)
                            {
                                remoteIPAddress.RemoveAll(x => x.IdSnake == deadPlayer.IdSnake);
                            }

                            Leaders.Add(new Leaders()
                            {
                                Name = user.Name,
                                Points = deadPlayer.SnakesPlayers.Points.Count - 3
                            });
                        }

                        lock (viewModelGames)
                        {
                            viewModelGames.RemoveAll(x => x.IdSnake == deadPlayer.IdSnake);
                        }
                    }

                    if (deadPlayers.Count > 0)
                    {
                        SaveLeaders();
                    }

                    foreach (var user in usersCopy)
                    {
                        var player = gamesCopy.Find(x => x.IdSnake == user.IdSnake);
                        if (player == null) continue;

                        var snake = player.SnakesPlayers;
                        if (snake.GameOver) continue;

                        for (int i = snake.Points.Count - 1; i >= 0; i--)
                        {
                            if (i != 0)
                            {
                                snake.Points[i] = new Snakes.Point(
                                    snake.Points[i - 1].X,
                                    snake.Points[i - 1].Y
                                );
                            }
                            else
                            {
                                int speed = Math.Min(10 + (int)Math.Round(snake.Points.Count / 20f), MaxSpeed);
                                var head = snake.Points[0];

                                switch (snake.direction)
                                {
                                    case Snakes.Direction.Right:
                                        snake.Points[i] = new Snakes.Point(head.X + speed, head.Y);
                                        break;
                                    case Snakes.Direction.Left:
                                        snake.Points[i] = new Snakes.Point(head.X - speed, head.Y);
                                        break;
                                    case Snakes.Direction.Down:
                                        snake.Points[i] = new Snakes.Point(head.X, head.Y + speed);
                                        break;
                                    case Snakes.Direction.Up:
                                        snake.Points[i] = new Snakes.Point(head.X, head.Y - speed);
                                        break;
                                }
                            }
                        }

                        var headPoint = snake.Points[0];
                        if (headPoint.X <= 0 || headPoint.X >= 793 || headPoint.Y <= 0 || headPoint.Y >= 723)
                        {
                            snake.GameOver = true;
                            continue;
                        }

                        if (snake.direction != Snakes.Direction.Start)
                        {
                            for (int i = 1; i < snake.Points.Count; i++)
                            {
                                if (Math.Abs(headPoint.X - snake.Points[i].X) <= 1 &&
                                    Math.Abs(headPoint.Y - snake.Points[i].Y) <= 1)
                                {
                                    snake.GameOver = true;
                                    break;
                                }
                            }
                        }

                        if (snake.GameOver) continue;

                        if (Math.Abs(headPoint.X - player.Points.X) <= 15 &&
                            Math.Abs(headPoint.Y - player.Points.Y) <= 15)
                        {
                            player.Points = new Snakes.Point(
                                new Random().Next(10, 783),
                                new Random().Next(10, 410)
                            );

                            var lastPoint = snake.Points[snake.Points.Count - 1];
                            snake.Points.Add(new Snakes.Point(lastPoint.X, lastPoint.Y));

                            LoadLeaders();
                            Leaders.Add(new Leaders()
                            {
                                Name = user.Name,
                                Points = snake.Points.Count - 3
                            });

                            Leaders = Leaders.OrderByDescending(x => x.Points)
                                           .ThenBy(x => x.Name)
                                           .ToList();

                            player.Top = Leaders.FindIndex(x => x.Points == snake.Points.Count - 3 && x.Name == user.Name) + 1;
                            SaveLeaders();
                        }
                    }

                    Send();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в игровом цикле: {ex.Message}");
                }
            }
        }
        public static void Receiver()
        {
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;
            try
            {
                Console.WriteLine("Сервер запущен:");
                while (true)
                {
                    byte[] receiveBytes = receivingUdpClient.Receive(
                        ref RemoteIpEndPoint);
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    Console.WriteLine("Получил команду: " + returnData.ToString());
                    if (returnData.ToString().Contains("/start"))
                    {
                        string[] dataMessage = returnData.ToString().Split('|');
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        Console.WriteLine($"Подключился пользователь: {viewModelUserSettings.IPAddress}:{viewModelUserSettings.Port}");
                        remoteIPAddress.Add(viewModelUserSettings);
                        viewModelUserSettings.IdSnake = AddSnake();
                        viewModelGames[viewModelUserSettings.IdSnake].IdSnake = viewModelUserSettings.IdSnake;
                    }
                    else
                    {
                        string[] dataMessage = returnData.ToString().Split('|');
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        int IdPlayer = -1;
                        IdPlayer = remoteIPAddress.FindIndex(x => x.IPAddress == viewModelUserSettings.IPAddress
                            && x.Port == viewModelUserSettings.Port);
                        if (IdPlayer != -1)
                        {
                            if (dataMessage[0] == "Up" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Down)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Up;
                            else if (dataMessage[0] == "Down" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Up)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Down;
                            else if (dataMessage[0] == "Left" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Right)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Left;
                            else if (dataMessage[0] == "Right" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Left)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Right;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n" + ex.Message);
            }
        }
        static void Main(string[] args)
        {
            try
            {
                Thread tRec = new Thread(new ThreadStart(Receiver));
                tRec.Start();
                Thread tTime = new Thread(Timer);
                tTime.Start();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n" + ex.Message);
            }
        }
    }
}