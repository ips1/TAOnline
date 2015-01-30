using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAGame;

namespace TAClient
{
    /// <summary>
    /// Class used for parsing server commands and calling methods on Synchronizer
    /// </summary>
    public class MessageParser
    {
        private Synchronizer gameSynchronizer;

        /// <summary>
        /// Default constructor specifiing assigned synchronizer
        /// </summary>
        /// <param name="gameSynchronizer">Synchronizer to call methods on</param>
        public MessageParser(Synchronizer gameSynchronizer)
        {
            this.gameSynchronizer = gameSynchronizer;
        }

        /// <summary>
        /// Parses one message and calls corresponding method on the synchronizer (if the message is correct)
        /// </summary>
        /// <param name="msg">Message to be parsed</param>
        public void ParseMessage(string msg)
        {
            try
            {
                InternalParse(msg);
            }
            catch (InvalidOperationException)
            {
                gameSynchronizer.InvalidOperation();
            }

 
        }

        /// <summary>
        /// Internal method for parsing message, should be called only from ParseMessage method
        /// </summary>
        /// <param name="msg">Message to be parsed</param>
        private void InternalParse(string msg)
        {
            String[] parsed = msg.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (parsed.Length == 0) return;

            if (parsed.Length == 1 && parsed[0] == "KICKED")
            {
                gameSynchronizer.Kicked();

            }
            else if (parsed[0] == "STARTING")
            { }
            else if (parsed[0] == "PLAYERS")
            { }
            else if (parsed[0] == "SETSTART" && parsed.Length == 4)
            {
                long id;
                int x;
                int y;
                try
                {
                    id = Int64.Parse(parsed[1]);
                    x = Int32.Parse(parsed[2]);
                    y = Int32.Parse(parsed[3]);
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException();
                }
                gameSynchronizer.SetStart(id, x, y);
            }
            else if (parsed[0] == "PLACERAIL" && parsed.Length == 5)
            {
                long id;
                int x;
                int y;
                Direction d;
                try
                {
                    id = Int64.Parse(parsed[1]);
                    x = Int32.Parse(parsed[2]);
                    y = Int32.Parse(parsed[3]);
                    d = GetDirection(parsed[4]);
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException();
                }
                gameSynchronizer.PlaceRail(id, x, y, d);
            }
            else if (parsed[0] == "NEWROUND" && parsed.Length == 1)
            {
                gameSynchronizer.StartGame();
            }
            else if (parsed[0] == "CITIES" && parsed.Length >= 3)
            {
                long id;
                try
                {
                    id = Int64.Parse(parsed[1]);
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException();
                }
                string cities = ReconstructMessage(parsed, 2, ' ');
                gameSynchronizer.AssignCities(id, cities);
            }
            else if (parsed[0] == "QUIT" && parsed.Length >= 2)
            {
                string error = ReconstructMessage(parsed, 1, ' ');
                gameSynchronizer.Quit(error);
            }
            else if (parsed[0] == "MSG" && parsed.Length >= 3)
            {
                string name = parsed[1];

                // Reconstruct the message
                StringBuilder nmsg = new StringBuilder();
                for (int i = 2; i < parsed.Length; i++)
                {
                    nmsg.Append(parsed[i]);
                    nmsg.Append(" ");
                }

                gameSynchronizer.MessageReceived(name, nmsg.ToString());
            }
            else if (parsed[0] == "CLIST" && (parsed.Length % 2 == 1))
            {
                int i = 1;
                while (i < parsed.Length)
                {
                    long id;
                    try
                    {
                        id = Int64.Parse(parsed[i++]);
                    }
                    catch (FormatException)
                    {
                        throw new InvalidOperationException();
                    }
                    string nick = parsed[i++];
                    gameSynchronizer.AddClient(nick, id);
                }
            }
            else if (parsed[0] == "RLIST" && (parsed.Length % 2 == 1))
            {
                int i = 1;
                while (i < parsed.Length)
                {
                    long id;
                    try
                    {
                        id = Int64.Parse(parsed[i++]);
                    }
                    catch (FormatException)
                    {
                        throw new InvalidOperationException();
                    }
                    string nick = parsed[i++];
                    gameSynchronizer.MakeReady(id);
                }
            }
            else if (parsed.Length == 2 && parsed[0] == "READY")
            {
                long id;
                try
                {
                    id = Int64.Parse(parsed[1]);
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException();
                }
                gameSynchronizer.MakeReady(id);
            }
            else if (parsed.Length == 3 && parsed[0] == "CONNECTED")
            {
                long id;
                try
                {
                    id = Int64.Parse(parsed[1]);
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException();
                }
                string nick = parsed[2];
                gameSynchronizer.AddClient(nick, id);
            }
            else if (parsed.Length == 3 && parsed[0] == "DISCONNECTED")
            {
                long id;
                try
                {
                    id = Int64.Parse(parsed[1]);
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException();
                }
                string nick = parsed[2];
                gameSynchronizer.RemoveClient(id);
            }
            else
            {
                //throw new InvalidOperationException();
            }
 
        }

        /// <summary>
        /// Gets the TAGame.Direction from a string representation of direction
        /// </summary>
        /// <remarks>String formats: NW, NE, E, W, SW, SE</remarks>
        /// <param name="s">String from which the direction is parsed</param>
        /// <returns>Direction parsed from the string</returns>
        /// <exception cref="FormatException">String is not in required format</exception>
        private Direction GetDirection(string s)
        {
            Direction result;
            switch (s)
            {
                case "NW": result = Direction.NORTHWEST; break;
                case "NE": result = Direction.NORTHEAST; break;
                case "E": result = Direction.EAST; break;
                case "W": result = Direction.WEST; break;
                case "SW": result = Direction.SOUTHWEST; break;
                case "SE": result = Direction.SOUTHEAST; break;
                default: throw new FormatException(); break;
            }
            return result;
        }

        private string ReconstructMessage(string[] parts, int startIndex, char separator)
        {
            StringBuilder nmsg = new StringBuilder();
            for (int i = startIndex; i < parts.Length; i++)
            {
                nmsg.Append(parts[i]);
                if (i < parts.Length - 1) nmsg.Append(separator);
            }
            return nmsg.ToString();

        }

    }
}
