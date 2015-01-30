using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAGame;

namespace TAServer
{

    /// <summary>
    /// Class for parsing messages from clients and console and responding to them
    /// </summary>
    /// <remarks>
    /// Main method is RunParser() which blocks the calling thread and handles all the messages in an infinite loop
    /// </remarks>
    class MessageParser
    {
        private Server parentServer;
        private MessageQueue parentQueue;

        /// <summary>
        /// Creates new MessageParser with specified server and queue to withdraw messages from
        /// </summary>
        /// <param name="parentServer">Server which should respond to messages</param>
        /// <param name="parentQueue">Queue containing all the messages</param>
        public MessageParser(Server parentServer, MessageQueue parentQueue)
        {
            this.parentServer = parentServer;
            this.parentQueue = parentQueue;
        }

        /// <summary>
        /// Starts handling the messages, blocks calling thread
        /// </summary>
        public void RunParser()
        {
            while (true)
            {
                Message msg = parentQueue.Get();
                ParseMessage(msg);
            }
        }

        /// <summary>
        /// Parses one message and responds to it
        /// </summary>
        /// <param name="msg">Message to be handled</param>
        private void ParseMessage(Message msg)
        {
            if (msg.Sender == null)
            {
                ParseLocal(msg.Content);
            }
            else
            {
                ParseClient(msg.Sender, msg.Content);
            }
        }

        /// <summary>
        /// Handles local message (console command) 
        /// </summary>
        /// <param name="msg">Message to be handled</param>
        private void ParseLocal(string msg)
        {
            String[] parsed = msg.Split(' ');
            if ((parsed.Length == 2) && (parsed[0] == "KICK"))
            {
                parentServer.Kick(parsed[1]);
            }
            else if ((parsed.Length == 1) && (parsed[0] == "LIST"))
            {
                parentServer.List();
            }
        }

        /// <summary>
        /// Handles remote message (client message)
        /// </summary>
        /// <param name="sender">Sender of the message</param>
        /// <param name="msg">Message to be handled</param>
        private void ParseClient(ClientHandler sender,string msg)
        {
            String[] parsed = msg.Split(' ');
            if (parsed.Length == 0) return;

            if (parsed[0] == "MSG" && parsed.Length >= 2)
            {
                // Reconstruct the message
                StringBuilder nmsg = new StringBuilder();
                for (int i = 1; i < parsed.Length; i++)
                {
                    nmsg.Append(parsed[i]);
                    nmsg.Append(" ");
                }
                // Prints the message
                Console.WriteLine("[{0}] {1}", sender.Name, nmsg.ToString());
                // Resends the message
                parentServer.SendAll("MSG " + sender.Name + " " + nmsg);
            }
            else if (parsed[0] == "PING" && parsed.Length == 1)
            {
 
            }
            else if (parsed[0] == "READY" && parsed.Length == 1)
            {
                // We are already in game, READY message is invalid
                if (parentServer.State == ServerState.INGAME)
                {
                    InvalidData(sender, msg);
                    return;
                }
                sender.IsReady = true;
                // Informs all clients about the new ready player
                parentServer.SendAll("READY " + sender.Id);
                parentServer.RefreshStates();
            }
            else if (parsed[0] == "SETSTART" && parsed.Length == 3)
            {
                // We have to be in game to receive this message
                if (parentServer.State != ServerState.INGAME)
                {
                    InvalidData(sender, msg);
                    return;
                }
                try
                {
                    int x = Int32.Parse(parsed[1]);
                    int y = Int32.Parse(parsed[2]);
                    parentServer.SetStart(sender.Id, x, y);
                }
                catch (FormatException e)
                {
                    InvalidData(sender, msg);
                    return;
                }
            }
            else if (parsed[0] == "SETSTART" && parsed.Length == 3)
            {
                // We have to be in game to receive this message
                if (parentServer.State != ServerState.INGAME)
                {
                    InvalidData(sender, msg);
                    return;
                }
                try
                {
                    int x = Int32.Parse(parsed[1]);
                    int y = Int32.Parse(parsed[2]);
                    parentServer.SetStart(sender.Id, x, y);
                }
                catch (FormatException e)
                {
                    InvalidData(sender, msg);
                    return;
                }
            }
            else if (parsed[0] == "PLACERAIL" && parsed.Length == 4)
            {
                // We have to be in game to receive this message
                if (parentServer.State != ServerState.INGAME)
                {
                    InvalidData(sender, msg);
                    return;
                }
                try
                {
                    int x = Int32.Parse(parsed[1]);
                    int y = Int32.Parse(parsed[2]);
                    Direction d = GetDirection(parsed[3]);
                    parentServer.PlaceRail(sender.Id, x, y, d);
                }
                catch (FormatException e)
                {
                    InvalidData(sender, msg);
                    return;
                }
            }
            else
            {
                InvalidData(sender, msg);
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

        /// <summary>
        /// Reports corrupted / invalid message
        /// </summary>
        /// <param name="sender">Sender of the message</param>
        /// <param name="data">Message content</param>
        private void InvalidData(ClientHandler sender, String data)
        {
            Console.WriteLine("Invalid data received from {0}:", sender.Name);
            Console.WriteLine(data);
        }
    }
}
