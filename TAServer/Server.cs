using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using TAGame;

namespace TAServer
{
    // State of the server
    enum ServerState { READY, STARTING, INGAME }

    // Class representing running server
    class Server
    {
        private MessageQueue queue;
        public ServerState State { get; set; }
        private string name;
        private int port;
        private TcpListener listener;
        private long counter;  
        private List<ClientHandler> clients;  // List of all connected clients
        private object clientLock;  // Special object for locking while manipulating with clients list
        private const int maxPlayers = 6;
        private const int minPlayers = 2;
        private const int pointLimit = 5;

        public static readonly int PortNo = 4686;

        private Game currentGame;

        private volatile bool closing;

        public Server(string name, int port, MessageQueue queue)
        {
            this.queue = queue;
            State = ServerState.READY;
            this.name = name;
            this.port = port;
            listener = new TcpListener(IPAddress.Any, port);
            clients = new List<ClientHandler>();
            counter = 1;
            clientLock = new object();
            closing = false;
        }

        // Constructor assigning default values
        public Server(MessageQueue queue) : this("TAServer", PortNo, queue) { }

        // Starts the listener
        public void Start()
        {
            listener.Start();
        }

        // Starts the main loop of the server
        // (which is asynchronious in order to accept commands from std. input)
        public async void Run()
        {
            Console.WriteLine("Server {0} is running!", name);
            Console.WriteLine("Target port: {0}", port);

            // Infinite cycle -> quitting server will break it
            while (!closing)
            {
                Socket s = await listener.AcceptSocketAsync();
                AcceptClient(s);
            }
        }

        // Synchroniously communicates with client which is attempting to connect
        // first checks the handshake -> if its incorrect, sends out ERROR message and quits the client connection
        // second checks if there is room on the server -> if not, sends out ERROR message and disconnects the client
        private void AcceptClient(Socket s)
        {
            ClientHandler handler;
            lock (clientLock)
            {
                long currentNo = counter++;
                handler = new ClientHandler(s, currentNo, this, queue);

                // Try the handshake
                bool success = handler.Handshake();
                if (!success)
                {
                    handler.Close();
                    return;
                }

                // Check whether we accept players
                if (State != ServerState.READY)
                {
                    handler.Send("ERROR The game is already running!");
                    handler.Close();
                    return;
                }
                
                // Check the player count
                if (clients.Count >= maxPlayers)
                {
                    handler.Send("ERROR Server is full");
                    handler.Close();
                    return;
                }

                // Check the player name
                if (NameExists(handler.Name))
                {
                    handler.Send("ERROR Name is already in use on the server");
                    handler.Close();
                    return;
                }
                handler.Send("ACK " + currentNo);
                SendAll("CONNECTED " + handler.Id + " " + handler.Name);
                clients.Add(handler);
                handler.Send("CLIST " + GetClients(false));
                handler.Send("RLIST " + GetClients(true));

            }
            // Starts receiving information from the client
            handler.Init();
        }

        // Gets all connected client as a string (space is delimiter)
        private string GetClients(bool readyOnly)
        {
            StringBuilder tmp = new StringBuilder();
            lock (clientLock)
            {
                foreach (ClientHandler ch in clients)
                {
                    if (readyOnly)
                    {
                        if (ch.IsReady) tmp.Append(" " + ch.Id + " " + ch.Name);
                    }
                    else tmp.Append(" " + ch.Id + " " + ch.Name);
                }
            }
            return (tmp.Length > 0) ? tmp.Remove(0, 1).ToString() : "";
        }

        // Checks whether there is client connected with specified name
        private bool NameExists(string name)
        {

            bool exists = false;
            lock (clientLock)
            {
                foreach (ClientHandler ch in clients)
                {
                    if (ch.Name == name)
                    {
                        exists = true;
                        break;
                    }
                }
            }
            return exists;
        }

        public void Unregister(ClientHandler ch)
        {
            lock (clientLock)
            {
                clients.Remove(ch);
            }
        }

        // Sends the same message to all clients
        public void SendAll(string message)
        {
            lock (clientLock)
            {
                foreach (ClientHandler ch in clients)
                {
                    ch.Send(message);
                }
            }
        }

        // Refreshes the client states and attempts to start the game
        public void RefreshStates()
        {
            // Lock to prevent other players from joining while checking states
            lock (clientLock)
            {
                if (clients.Count < minPlayers) return;
                bool allReady = true;
                foreach (ClientHandler ch in clients)
                {
                    if (!ch.IsReady)
                    {
                        allReady = false;
                        break;
                    }
                }
                if (allReady) StartGame();
            }
        }

        
        // Starts new game (initializes it) and informs all clients
        private void StartGame()
        {
            Console.WriteLine("Game starting!");
            SendAll("STARTING");
            State = ServerState.STARTING;
            SendAll("PLAYERS " + GetClients(false));

            ResetPoints();
            NewRound();

          

        }

        // Starts new round of an existing game
        // Internally represented as a new game, but points persist
        private void NewRound()
        {
            currentGame = new Game();
            lock (clientLock)
            {
                foreach (ClientHandler ch in clients)
                {
                    currentGame.AddPlayer(ch.Name, ch.Id);
                }
            }

            SendAll("NEWROUND");
            
            // CITY ASSIGNMENT

            currentGame.AssignCities();

            // SENDING CITY NAMES TO ALL CLIENTS

            List<string> cityLists = currentGame.GetPlayerCities();
            
            foreach (string s in cityLists)
            {
                SendAll("CITIES " + s);
            }

            // GAME STARTING

            State = ServerState.INGAME;
            try
            {
                currentGame.Start();
            }
            catch (IncorrectMoveException)
            {
                ForceQuitGame("Cannot start the game!");
            }

        }

        // Quits the game because of an error or player disconnecting
        public void ForceQuitGame(String errorMessage)
        {
            Console.WriteLine("Quitting the game, error:");
            Console.WriteLine(errorMessage);
            SendAll("QUIT " + errorMessage);
            currentGame = null;
            lock (clientLock)
            {
                State = ServerState.READY;
                foreach (ClientHandler ch in clients)
                {
                    ch.IsReady = false;
                }
            }
           
        }

        /// <summary>
        /// Gets ClientHandler for the specified ID of a client
        /// </summary>
        /// <param name="id">ID of the client</param>
        /// <returns>ClientHandler of the specified client</returns>
        private ClientHandler GetClient(long id)
        {
            foreach (ClientHandler ch in clients)
            {
                if (ch.Id == id) return ch;
            }
            return null;
        }


        /// <summary>
        /// Checks whether someone has won the round, if so, starts a new round or ends the game
        /// </summary>
        public void CheckForVictory()
        {
            if (!currentGame.IsFinished()) return;

            GameFinished();
        }

        /// <summary>
        /// Ends the game
        /// </summary>
        /// <remarks>
        /// Should be called only after player reaches the point cap
        /// </remarks>
        public void GameFinished()
        {

            Console.WriteLine("Game finished. Winners:");

            StringBuilder sb = new StringBuilder("FINISHED");

            foreach (long pid in currentGame.GetWinnerIDs())
            {
                ClientHandler client = GetClient(pid);
                Console.WriteLine(client.Name);
                sb.Append(" ");
                sb.Append(client.Id);
            }

            SendAll(sb.ToString());
            currentGame = null;

            lock (clientLock)
            {
                State = ServerState.READY;
                foreach (ClientHandler ch in clients)
                {
                    ch.IsReady = false;
                }
            }
        }
        

        // Kicks the client with specified name
        public void Kick(string name)
        {
            ClientHandler target = null;
            foreach (ClientHandler ch in clients)
            {
                if (ch.Name == name)
                {
                    target = ch;
                    break;
                }
            }
            if (target != null)
            {
                Console.WriteLine("Player kicked: {0}", name);
                target.Send("KICKED");
                target.Close();
            }
        }

        /// <summary>
        /// Attempts to set the starting position of a player to specified coordinates
        /// </summary>
        /// <param name="pid">Player whose position is to be set</param>
        /// <param name="x">X coordinate of starting position</param>
        /// <param name="y">Y coordinate of starting position</param>
        public void SetStart(long pid, int x, int y)
        {
            if (State != ServerState.INGAME) ForceQuitGame("Invalid move by player " + pid);

            try
            {
                currentGame.SelectPosition(pid, x, y);
                SendAll("SETSTART " + pid + " " + x + " " + y);
            }
            catch (IncorrectMoveException e)
            {
                ForceQuitGame("Invalid move by player " + pid);
            }
        }
        
        /// <summary>
        /// Attempts to place rail for a player to specified position
        /// </summary>
        /// <param name="pid">Player to place the rail</param>
        /// <param name="x">X coordinate of one end of the rail</param>
        /// <param name="y">Y coordinate of one end of the rail</param>
        /// <param name="d">Direction in which the rail leads</param>
        public void PlaceRail(long pid, int x, int y, Direction d)
        {
            if (State != ServerState.INGAME) ForceQuitGame("Invalid move by player " + pid);

            try
            {
                currentGame.PlaceRail(pid, x, y, d);
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
                SendAll("PLACERAIL " + pid + " " + x + " " + y + " " + dir);
                CheckForVictory();
            }
            catch (IncorrectMoveException e)
            {
                ForceQuitGame("Invalid move by player " + pid);
            }
        }

        /// <summary>
        /// Prints list of all connected clients to the console
        /// </summary>
        public void List()
        {
            lock (clientLock)
            {
                Console.WriteLine("------------------");

                Console.WriteLine("Connected clients:");
                foreach (ClientHandler ch in clients)
                {
                    Console.WriteLine("  {0}: {1}", ch.Id, ch.Name);
                }
                Console.WriteLine("------------------");
            }
        }

        // Resets points for all clients from previous games
        private void ResetPoints()
        {
            foreach (ClientHandler ch in clients)
            {
                ch.ResetPoints();
            }
        }
    }
}
