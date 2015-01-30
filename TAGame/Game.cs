using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAGame
{
    /// <summary>
    /// Enum representing current state of the game
    /// </summary>
    enum GameState
    {
        WAITING, POSITIONSELECTION, PLACEMENT, FINISHED
    }

    /// <summary>
    /// Exception representing that an incorrect (according to the rules) move was attempted
    /// </summary>
    public class IncorrectMoveException : Exception
    {
 
    }

    /// <summary>
    /// Class representing one game round of Trans America game
    /// </summary>
    /// <remarks>After it is finished, it cannot be restarted, new game has to be created</remarks>
    public class Game
    {
        private GamePlan plan;
        private List<City> cities;
        private List<Player> players;

        private List<Player> winners;

        private GameState currentState;
        private Player activePlayer;

        private bool oneSelected;

        private int activePlayerNo;

        public int ActivePlayerNo { get { return activePlayerNo; } }

        /// <summary>
        /// Default constructor for the game class
        /// </summary>
        public Game()
        {
            winners = new List<Player>();
            oneSelected = false;
            currentState = GameState.WAITING;
            plan = new GamePlan();
            players = new List<Player>();
            InitCities();
        }

        public GamePlan Plan
        {
            get { return plan; }
        }

        /// <summary>
        /// Initializes all the cities in the game
        /// </summary>
        /// <remarks>Is called from the Game class constructor, should't be called from anywhere else</remarks>
        private void InitCities()
        {
            cities = new List<City> { 
                    new City("Seattle", plan.GetVertex(4,0), CityColor.GREEN),
                    new City("Portland", plan.GetVertex(3,1), CityColor.GREEN),
                    new City("Medford", plan.GetVertex(2,3), CityColor.GREEN),
                    new City("Sacramento", plan.GetVertex(1,5), CityColor.GREEN),
                    new City("San Francisco", plan.GetVertex(0,6), CityColor.GREEN),
                    new City("Los Angeles", plan.GetVertex(0,9), CityColor.GREEN),
                    new City("San Diego", plan.GetVertex(0,10), CityColor.GREEN),
                
                    new City("Salt Lake City", plan.GetVertex(4,4), CityColor.YELLOW),
                    new City("Denver", plan.GetVertex(6,5), CityColor.YELLOW),
                    new City("Omaha", plan.GetVertex(9,4), CityColor.YELLOW),
                    new City("Kansas City", plan.GetVertex(9,6), CityColor.YELLOW),
                    new City("St. Louis", plan.GetVertex(11,6), CityColor.YELLOW),
                    new City("Oklahoma City", plan.GetVertex(7,8), CityColor.YELLOW),
                    new City("Santa Fe", plan.GetVertex(4,8), CityColor.YELLOW),

                    new City("Helena", plan.GetVertex(6,1), CityColor.BLUE),
                    new City("Bismarck", plan.GetVertex(10,1), CityColor.BLUE),
                    new City("Duluth", plan.GetVertex(13,1), CityColor.BLUE),
                    new City("Minneapolis", plan.GetVertex(12,2), CityColor.BLUE),
                    new City("Chicago", plan.GetVertex(14,3), CityColor.BLUE),
                    new City("Buffalo", plan.GetVertex(17,2), CityColor.BLUE),
                    new City("Cincinnati", plan.GetVertex(14,5), CityColor.BLUE),

                    new City("Boston", plan.GetVertex(19,2), CityColor.ORANGE),
                    new City("New York", plan.GetVertex(17,4), CityColor.ORANGE),
                    new City("Washington", plan.GetVertex(16,5), CityColor.ORANGE),
                    new City("Richmond", plan.GetVertex(15,7), CityColor.ORANGE),
                    new City("Winston", plan.GetVertex(13,8), CityColor.ORANGE),
                    new City("Charleston", plan.GetVertex(13,10), CityColor.ORANGE),
                    new City("Jacksonville", plan.GetVertex(11,12), CityColor.ORANGE),

                    new City("Phoenix", plan.GetVertex(2,9), CityColor.RED),
                    new City("El Paso", plan.GetVertex(3,11), CityColor.RED),
                    new City("Dallas", plan.GetVertex(7,10), CityColor.RED),
                    new City("Houston", plan.GetVertex(6,12), CityColor.RED),
                    new City("Memphis", plan.GetVertex(10,9), CityColor.RED),
                    new City("Atlanta", plan.GetVertex(11,10), CityColor.RED),
                    new City("New Orleans", plan.GetVertex(8,12), CityColor.RED),

                };

            // Set cities for all vertices
            foreach (City c in cities)
            {
                c.Position.CityOn = c;
            }

        }

        /// <summary>
        /// Adds new player to the game
        /// </summary>
        /// <param name="nickname">Nickname of the player</param>
        /// <param name="id">ID of the player (unique identifier)</param>
        public void AddPlayer(String nickname, long id)
        {
            players.Add(new Player(nickname, id));
        }

        /// <summary>
        /// Gets player with specified nickname
        /// </summary>
        /// <param name="nickname">Nickname of the player</param>
        /// <returns>Player with specified nickname</returns>
        private Player GetPlayer(String nickname)
        {
            Player res = null;
            foreach (Player p in players)
            {
                if (p.Nickname == nickname) res = p;
            }
            if (res == null) throw new IncorrectMoveException();
            return res;
        }

        /// <summary>
        /// Gets player with specified id
        /// </summary>
        /// <param name="pid">ID of the player</param>
        /// <returns>Player with specified ID</returns>
        private Player GetPlayer(long pid)
        {
            Player res = null;
            foreach (Player p in players)
            {
                if (p.Id == pid) res = p;
            }
            if (res == null) throw new IncorrectMoveException();
            return res;
        }

        /// <summary>
        /// Gets city with specified name
        /// </summary>
        /// <param name="s">Name of the city</param>
        /// <returns>City with specified name</returns>
        private City GetCity(string s)
        {
            foreach (City c in cities)
            {
                if (c.Name == s) return c;
                
            }
            return null;
        }

        /// <summary>
        /// Returns list of player cities
        /// </summary>
        /// <param name="pid">ID of the player</param>
        /// <returns>List of cities of specified plaeer</returns>
        public List<City> GetPlayerCities(long pid)
        {
            Player p = GetPlayer(pid);
            return p.GetCities();
        }

        /// <summary>
        /// Automatically (random) assigns cities to all players
        /// </summary>
        /// <remarks>
        /// Should be called only once and only if no city was previously assigned!
        /// Doesn't have to be called, cities can be assigned manually
        /// </remarks>
        public void AssignCities()
        {
            List<City> green = new List<City>();
            List<City> blue = new List<City>();
            List<City> yellow = new List<City>();
            List<City> red = new List<City>();
            List<City> orange = new List<City>();

            // Separating cities by color
            foreach (City c in cities)
            {
                switch (c.Color)
                {
                    case CityColor.BLUE: blue.Add(c); break;
                    case CityColor.RED: red.Add(c); break;
                    case CityColor.GREEN: green.Add(c); break;
                    case CityColor.YELLOW: yellow.Add(c); break;
                    case CityColor.ORANGE: orange.Add(c); break;
                }
            }

            Random r = new Random();

            // Assigning one city of each color to every player
            foreach (Player p in players)
            {
                int k;
                k = r.Next(green.Count);
                p.AssignCity(green[k]);
                green.RemoveAt(k);

                k = r.Next(blue.Count);
                p.AssignCity(blue[k]);
                blue.RemoveAt(k);

                k = r.Next(yellow.Count);
                p.AssignCity(yellow[k]);
                yellow.RemoveAt(k);

                k = r.Next(red.Count);
                p.AssignCity(red[k]);
                red.RemoveAt(k);

                k = r.Next(orange.Count);
                p.AssignCity(orange[k]);
                orange.RemoveAt(k);

            }
        }

        /// <summary>
        /// Manually assigns specific set of cities to a player
        /// </summary>
        /// <param name="pid">ID of a player</param>
        /// <param name="cities">List of city names separated by ';'</param>
        public void AssignSpecificCities(long pid, string cities)
        {
            string[] parsed = cities.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            Player p = GetPlayer(pid);

            foreach(string s in parsed)
            {
                City c = GetCity(s);
                if (c == null) throw new IncorrectMoveException();
                p.AssignCity(c);
            }
        }

        /// <summary>
        /// Returns all cities for all players in format suitable for sending via TCP/IP
        /// </summary>
        /// <returns>List of strings, one per each player, containing player id and list of their cities (delimited by ";")</returns>
        public List<string> GetPlayerCities()
        {
            List<string> result = new List<string>();
            foreach (Player p in players)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(p.Id);
                sb.Append(" ");
                foreach (City c in p.GetCities())
                {
                    sb.Append(c.Name);
                    sb.Append(";");
                }
                result.Add(sb.ToString());
            }
            return result;
        }

        /// <summary>
        /// Checks whether there are enough players in the game and whether all players have all their cities selected
        /// </summary>
        /// <returns>True if the game is ready to start</returns>
        public bool IsReadyToStart()
        {
            if (players.Count < 2) return false;
            bool ready = true;
            foreach (Player p in players)
            {
                if (!p.HasCities())
                {
                    ready = false;
                    break;
                }
            }
            return ready;
        }


        /// <summary>
        /// Starts the game
        /// </summary>
        public void Start()
        {
            if (currentState != GameState.WAITING) throw new IncorrectMoveException();
            if (!IsReadyToStart()) throw new IncorrectMoveException();

            currentState = GameState.POSITIONSELECTION;

            // Sort players according to their ID
            // Required to keep the player order synchronized between all instances of the same game (server and client instances)
            players.Sort((x, y) => x.Id.CompareTo(y.Id));

            activePlayerNo = 0;
            activePlayer = players[activePlayerNo];
        }

        /// <summary>
        /// Moves activePlayer pointer to next player
        /// </summary>
        /// <returns>Returns true if goes back to first player</returns>
        private bool NextPlayer()
        {
            activePlayerNo = (activePlayerNo + 1) % players.Count;
            activePlayer = players[activePlayerNo];
            return (activePlayerNo == 0);
        }

        /// <summary>
        /// Checks whether specified player is currently on the move
        /// </summary>
        /// <param name="pid">ID of the player to be checked</param>
        /// <returns>True if the player is on the move</returns>
        public bool IsActive(long pid)
        {
            Player p = GetPlayer(pid);
            if (p == activePlayer) return true;
            else return false;
        }

        /// <summary>
        /// Selects starting position for specified player
        /// </summary>
        /// <param name="pid">ID of the player</param>
        /// <param name="x">X coordinate of start vertex</param>
        /// <param name="y">Y coordinate of start vertex</param>
        public void SelectPosition(long pid, int x, int y)
        {
            Player p = GetPlayer(pid);

            if (currentState != GameState.POSITIONSELECTION) throw new IncorrectMoveException();
            if (p != activePlayer) throw new IncorrectMoveException();

            Vertex v = plan.GetVertex(x, y);
            p.AssignStart(v);

            v.BaseOf = p;
            v.ReachableBy.Add(p);
            plan.RefreshReachability(v);
            // If all the players have selected their positions, moves on
            if (NextPlayer())
            {
                currentState = GameState.PLACEMENT;
            }
        }

        /// <summary>
        /// Checks whether specified player is allowed to place rail to a specified position
        /// </summary>
        /// <param name="pid">ID of the player</param>
        /// <param name="e">Edge on which the player wants to place rail</param>
        /// <returns>True if the player can place rail</returns>
        public bool CanPlaceRail(long pid, Edge e)
        {
            if (e.HasRail) return false;

            if ((e.IsDouble) && (oneSelected)) return false;

            Player p = GetPlayer(pid);

            // At least one end of the edge has to be reachable by specified player
            Vertex v1 = e.From;
            Vertex v2 = e.To;

            return (v1.ReachableBy.Contains(p) || v2.ReachableBy.Contains(p));
        }

        /// <summary>
        /// Overload method where edge is specified by its coordinates and direction
        /// </summary>
        /// <param name="pid">ID of the player</param>
        /// <param name="x">X coordinate of one end</param>
        /// <param name="y">Y coordinate of one end</param>
        /// <param name="d">Direction of the edge</param>
        /// <returns>True if the player can place rail</returns>
        public bool CanPlaceRail(long pid, int x, int y, Direction d)
        {
            Edge e = plan.GetEdge(x, y, d);

            return CanPlaceRail(pid, e);
        }

        /// <summary>
        /// Places rail for a specified player to a position
        /// </summary>
        /// <param name="pid">ID of the player</param>
        /// <param name="e">Edge on which the player wants to place rail</param>
        public void PlaceRail(long pid, Edge e)
        {
            Player p = GetPlayer(pid);

            // Game must be in placement state
            if (currentState != GameState.PLACEMENT) throw new IncorrectMoveException();
            // Placing player must be active player
            if (p != activePlayer) throw new IncorrectMoveException();
            // Player must be allowed to place rail to a specified position
            if (!CanPlaceRail(pid, e)) throw new IncorrectMoveException();

            // Player places his second rail, this can't be double rail!
            if (e.IsDouble && oneSelected) throw new IncorrectMoveException();

            e.PlaceRail();
            plan.RefreshReachability(e.From);
            plan.RefreshReachability(e.To);


            // If the player placed double rail, his round is over
            if (e.IsDouble)
            {
                // If there is a winner, we change the game state and don't accept any further commands
                if (CheckForVictory())
                {
                    currentState = GameState.FINISHED;
                }
                else
                {
                    oneSelected = false;
                    NextPlayer();
                }
            }
            else
            {
                // If the player placed single rail 
                if (oneSelected)
                {
                    // If there is a winner, we change the game state and don't accept any further commands
                    if (CheckForVictory())
                    {
                        currentState = GameState.FINISHED;
                    }
                    else
                    {
                        oneSelected = false;
                        NextPlayer();
                    }
                }
                else
                {
                    // If there is a winner other than the active player and this is his first move, he can still make his second
                    if (CheckForVictory() && winners.Contains(p))
                    {
                        currentState = GameState.FINISHED;
                    }
                    else
                    {
                        winners.Clear();
                        oneSelected = true;
                    }
                }
            }


        }
        
        /// <summary>
        /// Places rail for a specified player to a position
        /// </summary>
        /// <param name="pid">ID of the player</param>
        /// <param name="x">X coordinate of one end</param>
        /// <param name="y">Y coordinate of one end</param>
        /// <param name="d">Direction of the rail</param>
        public void PlaceRail(long pid, int x, int y, Direction d)
        {
            Vertex v = plan.GetVertex(x, y);
            Edge e = plan.GetEdge(x, y, d);

            PlaceRail(pid, e);
        }

        /// <summary>
        /// Checks whether there is a winner
        /// </summary>
        /// <returns>True if there is a winner in the game</returns>
        private bool CheckForVictory()
        {
            foreach (Player p in players)
            {
                // For specific player we check whether he reached all of his cities
                bool isVictorious = true;
                foreach (City c in p.GetCities())
                {
                    if (!c.Position.ReachableBy.Contains(p))
                    {
                        isVictorious = false;
                        break;
                    }
                }
                if (isVictorious) winners.Add(p);
            }
            // If there is at least one player with all cities reached, there is a winner
            if (winners.Count == 0) return false;
            else return true;
        }

        /// <summary>
        /// Gets ID of the player who is currently on the move
        /// </summary>
        /// <returns>ID of active player</returns>
        public long GetActivePlayerId()
        {
            return activePlayer.Id;
        }

        /// <summary>
        /// Checks whether the game is finished
        /// </summary>
        /// <returns>True if the game is finished</returns>
        public bool IsFinished()
        {
            return (currentState == GameState.FINISHED);
        }

        /// <summary>
        /// Checks whether the game is in starting position selection phase
        /// </summary>
        /// <returns>True if the game is in starting position selection phase</returns>
        public bool IsPositionSelection()
        {
            return (currentState == GameState.POSITIONSELECTION);
        }

        /// <summary>
        /// Checks whether the game is in rail placement phase
        /// </summary>
        /// <returns>True if the game is in rail placement phase</returns>
        public bool IsPlacement()
        {
            return (currentState == GameState.PLACEMENT);
        }

        /// <summary>
        /// Gets all the players who won the gmae
        /// </summary>
        /// <returns>IEnumerable containing all the players who won the game</returns>
        private IEnumerable<Player> GetWinners()
        {
            foreach (Player p in winners)
            {
                yield return p;
            }
        }

        /// <summary>
        /// Gets IDs of all the players who won the game
        /// </summary>
        /// <returns>IEnumerable containing all the IDs of players who won the game</returns>
        public IEnumerable<long> GetWinnerIDs()
        {
            foreach (Player p in winners)
            {
                yield return p.Id;
            }
        }
    }
}
