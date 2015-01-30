using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAGame;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace TAClient
{
    /// <summary>
    /// Exception for situations in which the server commands are invalid
    /// </summary>
    public class InvalidOperationException : Exception
    {

    }

    /// <summary>
    /// Class representing one client connected to the game
    /// </summary>
    public class Client
    {
        public string NickName { get; private set; }
        private int points;
        public Border PlayerFrame { get; private set; }
        private TextBlock playerText;
        private StackPanel playerPanel;
        public bool IsReady { get; private set; }
        public bool IsActive { get; private set; }
        public long ID { get; private set; }

        public Brush Color { get; private set; }

        /// <summary>
        /// Default constructor for client, requires his ID and nickname
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="id"></param>
        public Client(string nickname, long id)
        {
            this.NickName = nickname;
            this.ID = id;
            this.points = 0;
            this.PlayerFrame = new Border() { Margin = new Thickness(2), BorderBrush = Brushes.Gray, Background = Brushes.DarkGray, BorderThickness = new Thickness(2) };
            playerText = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center, Text = nickname };
            playerPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            playerPanel.Children.Add(playerText);
            this.PlayerFrame.Child = playerPanel;
            this.IsReady = false;
            this.IsActive = false;
        }

        /// <summary>
        /// Sets client's status as ready
        /// </summary>
        public void Ready()
        {
            IsReady = true;
            PlayerFrame.Background = Brushes.LightGreen;
        }

        /// <summary>
        /// Sets client's status as not ready
        /// </summary>
        public void NotReady()
        {
            IsReady = false;
            PlayerFrame.Background = Brushes.DarkGray;
        }

        /// <summary>
        /// Specified client is the one running the game
        /// </summary>
        public void SetMe()
        {
            playerText.FontWeight = FontWeights.Bold;
        }

        /// <summary>
        /// Sets color for the client
        /// </summary>
        /// <param name="color">Color for the client</param>
        public void SetColor(Brush color)
        {
            this.Color = color;
            playerPanel.Children.Add(new Ellipse() { Fill = color, Width = 10, Height = 10, Margin = new Thickness(4) });
        }

        /// <summary>
        /// Resets color for the client
        /// </summary>
        public void ResetColor()
        {
            this.Color = null;
            foreach (UIElement e in playerPanel.Children)
            {
                if (e is Ellipse)
                {
                    playerPanel.Children.Remove(e);
                    return;
                }
            }
        }
    }


    /// <summary>
    /// Class providing the synchronization of running game on the server and in the client
    /// </summary>
    public class Synchronizer
    {
        private Connection parentConnection;
        private Game runningGame;
        private GameWindow gameWindow;
        private GameMap gameMap;

        private bool playing;
        private bool positionSelection;

        private static readonly int maxClients = 6;

        private List<Client> connectedClients;
        private List<City> assignedCities;
        private long myId;

        private List<Border> cityBoxes;
        private List<TextBlock> cityNames;
        private List<TextBlock> cityInfos;

        private int assignedCityCount;

        /// <summary>
        /// Default constructor for Synchronizer, requires connection to the server and window in which the game is running
        /// </summary>
        /// <param name="parentConnection">Connection to game server</param>
        /// <param name="gameWindow">Window in which the game is running</param>
        public Synchronizer(Connection parentConnection, GameWindow gameWindow)
        {
            this.parentConnection = parentConnection;
            parentConnection.SetErrorCallback(ConnectionErrorCallback);
            
            this.gameWindow = gameWindow;
            this.myId = parentConnection.Pid;
            this.playing = false;
            this.positionSelection = false;

            connectedClients = new List<Client>();

            cityBoxes = new List<Border> { gameWindow.CityBox1, gameWindow.CityBox2, gameWindow.CityBox3, gameWindow.CityBox4, gameWindow.CityBox5 };
            cityNames = new List<TextBlock> { gameWindow.CityName1, gameWindow.CityName2, gameWindow.CityName3, gameWindow.CityName4, gameWindow.CityName5 };
            cityInfos = new List<TextBlock> { gameWindow.CityInfo1, gameWindow.CityInfo2, gameWindow.CityInfo3, gameWindow.CityInfo4, gameWindow.CityInfo5 };

            ResetCities(false);
        }

        /// <summary>
        /// Automatically assigns colors to players according to their ID
        /// </summary>
        private void AssignColors()
        {
            List<Brush> colors = new List<Brush> { Brushes.Green, Brushes.Yellow, Brushes.Red, Brushes.Blue, Brushes.White, Brushes.Black };
            long n = Int64.MaxValue;
            long k = -1;
            Client current = null;
            for (int i = 0; i < connectedClients.Count; i++)
            {
                foreach (Client c in connectedClients)
                {
                    if ((c.ID <= n) && (c.ID > k))
                    {
                        current = c;
                        n = c.ID;
                    }
                }

                k = n;
                n = Int64.MaxValue;
                current.SetColor(colors[0]);
                colors.RemoveAt(0);

            }
        }

        /// <summary>
        /// Client received invalid operation command from the server
        /// </summary>
        public void InvalidOperation()
        {
            CloseWindow("ERROR: Invalid operation received from the server!");
        }

        /// <summary>
        /// Client was kicked from the server
        /// </summary>
        public void Kicked()
        {
            CloseWindow("You were kicked from the server!");
        }

        /// <summary>
        /// Game was ended by the server
        /// </summary>
        /// <param name="error"></param>
        public void Quit(string error)
        {
            gameWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show("Game ended by the server: " + error);
            }));
            gameWindow.Log("Game ended, reason: " + error);
            EndGame(false);
        }

        /// <summary>
        /// Ends running game
        /// </summary>
        /// <param name="leaveOld">If true, content of the map screen and city boxes is left intact</param>
        private void EndGame(bool leaveOld)
        {
            playing = false;
            positionSelection = false;

            runningGame = null;

            if (!leaveOld)
            {
                gameMap.UnloadMap();
                gameMap = null;
            }

            ResetCities(leaveOld);


            foreach (Client c in connectedClients)
            {
                c.NotReady();
                c.ResetColor();
            }

            gameWindow.ReadyCheckBox.IsEnabled = true;
            gameWindow.ReadyCheckBox.IsChecked = false;

            if (!leaveOld)
            {
                gameWindow.SetStatusText("Press Ready to start the game!", false);
            }
        }

        /// <summary>
        /// Adds new client to the game
        /// </summary>
        /// <param name="nickname">Nickname of new client</param>
        /// <param name="id">ID of new client</param>
        public void AddClient(string nickname, long id)
        {
            if (playing) throw new InvalidOperationException();
            if (connectedClients.Count < maxClients)
            {
                Client c = new Client(nickname, id);
                if (id == myId)
                {
                    c.SetMe();
                }
                connectedClients.Add(c);
                gameWindow.PlayerPanel.Children.Add(c.PlayerFrame);
                gameWindow.Log("Player " + c.NickName + " connected");
            }
            else throw new InvalidOperationException();
        }

        /// <summary>
        /// Removes specified client from the game
        /// </summary>
        /// <param name="id">ID of the client</param>
        public void RemoveClient(long id)
        {
            if (playing) throw new InvalidOperationException();

            Client c = GetClient(id);
            if (c == null) throw new InvalidOperationException();
            connectedClients.Remove(c);
            gameWindow.PlayerPanel.Children.Remove(c.PlayerFrame);
            gameWindow.Log("Player " + c.NickName + " disconnected");
        }

        /// <summary>
        /// Sets status of specified client as ready
        /// </summary>
        /// <param name="id">ID of the client</param>
        public void MakeReady(long id)
        {
            if (playing) throw new InvalidOperationException();

            Client c = GetClient(id);
            if (c == null) throw new InvalidOperationException();
            c.Ready();
            gameWindow.Log("Player " + c.NickName + " ready");
            if (id == myId)
            {
                gameWindow.SetStatusText("Waiting for other players...", false);
            }
        }

        /// <summary>
        /// Gets client with a specific ID (from connected clients)
        /// </summary>
        /// <param name="id">ID of the client</param>
        /// <returns>Client with specific ID</returns>
        public Client GetClient(long id)
        {
            foreach (Client c in connectedClients)
            {
                if (c.ID == id) return c;
            }
            return null;
        }

        /// <summary>
        /// Receives message from the server
        /// </summary>
        /// <param name="name">Name of sender</param>
        /// <param name="message">Content of message</param>
        public void MessageReceived(string name, string message)
        {
            gameWindow.PrintMessage("[" + name + "] " + message);
        }

        /// <summary>
        /// Sends chat message from the client
        /// </summary>
        /// <param name="message">Content of message</param>
        public void SendMessage(string message)
        {

            SendToServer("MSG " + message);
    
        }

        /// <summary>
        /// Sends specified string to server using the connection
        /// </summary>
        /// <param name="msg">String to be sent to server</param>
        public void SendToServer(string msg)
        {
            try
            {
                parentConnection.Send(msg);
            }
            catch (IOException)
            {
                CloseWindow("Error: Connection to server interrupted!");
            }
        }

        /// <summary>
        /// Starts the actual game (after all the clients are reported ready on the server)
        /// </summary>
        public void StartGame()
        {
            playing = true;
            positionSelection = true;

            // Create local game instance, add all the clients as players
            runningGame = new Game();
            foreach (Client c in connectedClients)
            {
                runningGame.AddPlayer(c.NickName, c.ID);
            }

            // Automatically assign colors to players
            AssignColors();

            assignedCityCount = 0;

            gameWindow.Log("All ready, game starting...");
            gameWindow.SetStatusText("Waiting for server to respond...", false);
        }

        /// <summary>
        /// Assigns specific set of cities to a player in the game
        /// </summary>
        /// <remarks>When cities are assigned to all players, AllCitiesAssigned() method is automatically called</remarks>
        /// <param name="pid">Player ID</param>
        /// <param name="cities">String containing all the city names delimited by ';'</param>
        public void AssignCities(long pid, string cities)
        {
            // Assign cities in local instance of the game
            runningGame.AssignSpecificCities(pid, cities);
            assignedCityCount++;
            // If those cities belong to the client, initializes boxes showing city infos
            if (pid == myId)
            {
                assignedCities = runningGame.GetPlayerCities(myId);
                InitCityBoxes();
            }
            gameWindow.Log("Cities assigned to " + GetClient(pid).NickName);

            if (assignedCityCount == connectedClients.Count)
            {
                AllCitiesAssigned();
            }
        }

        /// <summary>
        /// Method called when all cities were succesfully assigned, starts the actual game
        /// </summary>
        private void AllCitiesAssigned()
        {
            runningGame.Start();

            // If there is already a game map (from previous game)
            if (gameMap != null)
            {
                // We unload the map before loading the new one
                gameMap.UnloadMap();
            }

            // Prepare and load the new map
            gameMap = new GameMap(gameWindow.GameCanvas, runningGame, this);
            gameMap.LoadMap();

            // Prepare the interface for the client
            if (IsActivePlayer())
            {
                gameMap.ActivateMap();
                gameWindow.SetStatusText("Choose your starting position on the map", true);
            }
            else
            {
                gameMap.DeactivateMap();
                gameWindow.SetStatusText("Waiting for other players...",false);
            }

            gameWindow.Log("Player's turn to select start: " + GetClient(runningGame.GetActivePlayerId()).NickName);
        }

        /// <summary>
        /// Initializes all the boxes showing the cities
        /// </summary>
        private void InitCityBoxes()
        {
            for (int i = 0; i < 5; i++)
            {
                cityBoxes[i].Background = GetColorFromCity(assignedCities[i].Color);
                cityNames[i].Text = assignedCities[i].Name;
                if (assignedCities[i].Position.IsReachableBy(myId))
                {
                    cityInfos[i].Text = "Connected";
                    cityInfos[i].Foreground = Brushes.Green;
                }
                else
                {
                    cityInfos[i].Text = "Not Connected";
                    cityInfos[i].Foreground = Brushes.Red;
                }
                cityBoxes[i].Tag = assignedCities[i];
                cityBoxes[i].MouseEnter += CityEnterHandler;
                cityBoxes[i].MouseLeave += CityLeaveHandler;
            }
            
        }

        /// <summary>
        /// Refreshes all the boxes showing the cities (reachability of the cities)
        /// </summary>
        private void RefreshCities()
        {
            for (int i = 0; i < 5; i++)
            {
                if (assignedCities[i].Position.IsReachableBy(myId))
                {
                    cityInfos[i].Text = "Connected";
                    cityInfos[i].Foreground = Brushes.Green;
                }
                else
                {
                    cityInfos[i].Text = "Not Connected";
                    cityInfos[i].Foreground = Brushes.Red;
                }
            }
        }

        /// <summary>
        /// Puts the city info boxes to the original state and apperance
        /// </summary>
        /// <param name="leaveText">If true, leaves the current content in the boxes</param>
        private void ResetCities(bool leaveText)
        {
            for (int i = 0; i < 5; i++)
            {
                if (!leaveText)
                {
                    cityBoxes[i].Background = Brushes.DarkGray;
                    cityNames[i].Text = "";

                    cityInfos[i].Text = "";
                }
                cityBoxes[i].Tag = null;
                cityBoxes[i].MouseEnter -= CityEnterHandler;
                cityBoxes[i].MouseLeave -= CityLeaveHandler;
            }
        }

        /// <summary>
        /// Attempts to set a start position on the map for specified player
        /// </summary>
        /// <param name="pid">Player ID</param>
        /// <param name="x">X coordinate of the start</param>
        /// <param name="y">Y coordinate of the start</param>
        public void SetStart(long pid, int x, int y)
        {
            try
            {
                runningGame.SelectPosition(pid, x, y);
                gameMap.AddBase(runningGame.Plan.GetVertex(x, y));
                gameWindow.Log(GetClient(pid).NickName + " selected his start");
                RefreshCities();


                // Checking for end of start selection phase
                if (runningGame.IsPlacement())
                {
                    positionSelection = false;
                    gameMap.ReloadMap();

                    gameWindow.Log("Everybody selected starting position");
                    gameWindow.Log("Player's turn to place rail: " + GetClient(runningGame.GetActivePlayerId()).NickName);


                    if (IsActivePlayer())
                    {
                        gameWindow.SetStatusText("Your turn! Place a rail", true);
                        gameMap.ActivateMap();
                    }
                    else
                    {
                        gameWindow.SetStatusText("Waiting for other players...", false);
                        gameMap.DeactivateMap();
                    }
                }
                else
                {
                    gameMap.RefreshMap();
                    gameWindow.Log("Player's turn to select start: " + GetClient(runningGame.GetActivePlayerId()).NickName);

                    if (IsActivePlayer())
                    {
                        gameWindow.SetStatusText("Choose your starting position on the map", true);
                        gameMap.ActivateMap();

                    }
                    else
                    {
                        gameWindow.SetStatusText("Waiting for other players...", false);
                        gameMap.DeactivateMap();

                    }
                }

            }
            catch (IncorrectMoveException e)
            {
                Quit("Game error: Incorrect move received from the server!");
                
            }

        }

        /// <summary>
        /// Attempts to place a rail on the map by a specified player
        /// </summary>
        /// <param name="pid">Player ID</param>
        /// <param name="x">X coordinate of one end of the rail</param>
        /// <param name="y">Y coordinate of one end of the rail</param>
        /// <param name="d">Direction of the rail from specified end</param>
        public void PlaceRail(long pid, int x, int y, Direction d)
        {
            try
            {
                runningGame.PlaceRail(pid, x, y, d);
                gameMap.RefreshMap();
                gameWindow.Log(GetClient(pid).NickName + " placed a rail");

                RefreshCities();

                // Checking for end of game
                if (runningGame.IsFinished())
                {
                    playing = false;
                    positionSelection = false;

                    StringBuilder sb = new StringBuilder();

                    foreach (long l in runningGame.GetWinnerIDs())
                    {
                        sb.Append(GetClient(l).NickName);
                        sb.Append(" ");
                    }

                    gameWindow.Log("Game finished! Winners: " + sb.ToString());
                    gameWindow.SetStatusText("Game finished! Winners: " + sb.ToString(), true);
                    gameMap.DeactivateMap();
                    EndGame(true);
                }
                else
                {
                    gameWindow.Log("Player's turn to place rail: " + GetClient(runningGame.GetActivePlayerId()).NickName);

                    if (IsActivePlayer())
                    {
                        gameWindow.SetStatusText("Your turn! Place a rail", true);
                        gameMap.ActivateMap();
                    }
                    else
                    {
                        gameWindow.SetStatusText("Waiting for other players...", false);
                        gameMap.DeactivateMap();
                    }
                }

            }
            catch (IncorrectMoveException e)
            {
                Quit("Game error: Incorrect move received from the server!");

            }

        }


        /// <summary>
        /// User wants to set his start on the client
        /// </summary>
        /// <param name="x">X coordinate of desired start</param>
        /// <param name="y">Y coordinate of desired start</param>
        public void ClientSetStart(int x, int y)
        {
            SendToServer("SETSTART " + x + " " + y);
            gameMap.DeactivateMap();
        }

        /// <summary>
        /// User wants to place rail on the map
        /// </summary>
        /// <param name="x">X coordinate of one end of the rail</param>
        /// <param name="y">Y coordinate of one end of the rail</param>
        /// <param name="d">Direction of the rail</param>
        public void ClientPlaceRail(int x, int y, Direction d)
        {
            string dir = "";
            switch (d)
            {
                case Direction.EAST: dir = "E"; break;
                case Direction.NORTHEAST: dir = "NE"; break;
                case Direction.NORTHWEST: dir = "NW"; break;
                case Direction.SOUTHEAST: dir = "SE"; break;
                case Direction.SOUTHWEST: dir = "SW"; break;
                case Direction.WEST: dir = "W"; break;
            }
            SendToServer("PLACERAIL " + x + " " + y + " " + dir);
            gameMap.DeactivateMap();
        }

        /// <summary>
        /// Checks whether the player running the game is active (on the move)
        /// </summary>
        /// <returns>True if the player running the game is active</returns>
        public bool IsActivePlayer()
        {
            return (runningGame.GetActivePlayerId() == myId);
        }

        /// <summary>
        /// Handler for MouseEnter event of the city box
        /// </summary>
        public void CityEnterHandler(object sender, RoutedEventArgs e)
        {
            gameMap.HighlightCity((City)(((Border)sender).Tag));
        }

        /// <summary>
        /// Handler for MouseLeave event of the city box
        /// </summary>
        public void CityLeaveHandler(object sender, RoutedEventArgs e)
        {
            gameMap.RemoveHighlights();
        }

        /// <summary>
        /// Gets actual Brush from the CityColor parameter
        /// </summary>
        /// <param name="c">Color in the CityColor form</param>
        /// <returns>Actual WPF Brush</returns>
        private Brush GetColorFromCity(CityColor c)
        {
            switch (c)
            {
                case CityColor.BLUE: return Brushes.LightBlue;
                case CityColor.GREEN: return Brushes.LightGreen;
                case CityColor.ORANGE: return Brushes.Orange;
                case CityColor.RED: return Brushes.Salmon;
                case CityColor.YELLOW: return Brushes.LightYellow;
            }
            return null;
        }

        /// <summary>
        /// Client sets his status to ready
        /// </summary>
        public void LocalReady()
        {
            if (playing) throw new InvalidOperationException();
            SendToServer("READY");
            gameWindow.SetStatusText("Waiting for server to respond...", false);
        }

        /// <summary>
        /// Method for reporting connection error from the connection itself
        /// </summary>
        public void ConnectionErrorCallback()
        {
            CloseWindow("Error: Connection to server interrupted!");
        }

        /// <summary>
        /// Quits the game and closes the game window
        /// </summary>
        /// <param name="error">Error message to be shown</param>
        public void CloseWindow(string error)
        {
            gameWindow.Dispatcher.Invoke(new Action(() =>
            {
                gameWindow.Close();
                MessageBox.Show(error);
            }));
        }
    }
}
