using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace TAServer
{
    class ClientHandler
    {
        private MessageQueue queue;
        private Socket s;
        private Server parentServer;
        private bool shaken = false;
        private bool closed = false;
        private NetworkStream nstream;
        private StreamReader input;
        private StreamWriter output;
        private string protocol;
        private long id;
        private string name;
        public int Points { get; private set; }

        private bool isReady;

        // Should get Player asociated as well!

        public bool Closed
        {
            get { return closed; }
        }

        public bool IsReady
        {
            get { return isReady; }
            set { isReady = value; }
        }

        public long Id
        {
            get { return id; }
        }

        public ClientHandler(Socket s, long id, Server parentServer, MessageQueue queue)
        {
            this.queue = queue;
            this.parentServer = parentServer;
            this.s = s;
            this.id = id;
            nstream = new NetworkStream(s);
            input = new StreamReader(nstream);
            output = new StreamWriter(nstream);
            output.AutoFlush = true;
            name = "client" + id;
            isReady = false;
            Points = 0;
        }

        // Attempts to do the handshake process with the client
        public bool Handshake()
        {

            nstream.ReadTimeout = 500;
            try
            {
                String s = input.ReadLine();
                if (s == null)
                {
                    output.WriteLine("ERROR UnkonwnHandshake");
                    return false;
                }
                String[] parsed = s.Split(' ');
                if (parsed.Length < 3 || parsed[0] != "HANDSHAKE" || parsed[1] != "TAOnline")
                {
                    output.WriteLine("ERROR UnkonwnHandshake");
                    return false;
                }
                name = parsed[2];
                output.WriteLine("ACCEPT TAOnline");
                s = input.ReadLine();
                if (s != "ACK")
                {
                    return false;
                }
            }
            catch (IOException)
            {
                output.WriteLine("ERROR UnkonwnHandshake");
                return false;
            }
            nstream.ReadTimeout = Timeout.Infinite;
            return true;
        }

        // Adds one point to the client
        public void AddPoint()
        {
            Points++;
        }

        public int GetPoints()
        {
            return Points;
        }

        // Resets the point count for the client
        public void ResetPoints()
        {
            Points = 0;
        }

        // Prints message, notifies other clients, unregisters client and closes the connection
        public void End()
        {
            Console.WriteLine("Client disconnected: {0}", name);
            parentServer.Unregister(this);
            if (parentServer.State == ServerState.INGAME || parentServer.State == ServerState.STARTING)
            {
                parentServer.ForceQuitGame("Player disconnected: " + name);
            }
            parentServer.SendAll("DISCONNECTED " + id + " " + name);
            Close();
            parentServer.RefreshStates();
        }

        // Closes the connection to the client
        public void Close()
        {
            closed = true;
            try
            {
                s.Shutdown(SocketShutdown.Both);
            }
            catch (ObjectDisposedException)
            {

            }
            s.Close();
        }

        // Initiates the handler and starts receiving messages from the client
        public void Init()
        {
            Console.WriteLine("Client connected: {0}", name);
            Receive();
        }

        // Asynchroniously receives and dispatches messages from the client
        private async void Receive()
        {
            while (true)
            {
                String s = null;
                try
                {
                    s = await input.ReadLineAsync();
                    if (s == null)
                    {
                        throw new IOException();
                    }
                }
                catch (IOException e)
                {
                    End();
                    return;
                }

                EnqueueMessage(s);
            }
        }

        /// <summary>
        /// Adds specified message to the message queue to be parsed by the main server thread
        /// </summary>
        /// <param name="message">Message to be enqueued</param>
        private void EnqueueMessage(String message)
        {
            queue.Add(new Message(this, message));
        }

        // Sends the message to asociated client using the NetworkStream
        public void Send(String s)
        {
            Console.WriteLine("Sending <{0}> to [{1}]", s, name);
            try
            {
                output.WriteLine(s);
            }
            catch (IOException e)
            { }
        }

        public string Name
        {
            get { return name; }
        }
    }
}
