using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;


namespace TAClient
{
    /// <summary>
    /// Exception representing an error during the connection phase
    /// </summary>
    class ConnectionError : Exception
    {
        public ConnectionError(string msg) : base(msg) { }
 
    }

    /// <summary>
    /// Class representing the client side for communicating with the server
    /// </summary>
    public class Connection
    {
        private static readonly int port = 4686;

        private IPAddress targetIP;
        private IPEndPoint targetEP;
        private Socket targetSocket;
        private string nickname;
        private NetworkStream nstream;
        private StreamReader input;
        private StreamWriter output;
        private bool connected;
        private object receiveLock;

        private Action errorCallback;

        public long Pid { get; private set; }

        private bool closed;

        public bool IsConnected
        {
            get { return connected; }
        }

        /// <summary>
        /// Default constructor for the Client class
        /// </summary>
        /// <param name="targetIP">IP Address of the server</param>
        /// <param name="nickname">Nickname of this client on the server</param>
        public Connection(IPAddress targetIP, string nickname, object receiveLock)
        {
            this.targetIP = targetIP;
            this.nickname = nickname;
            this.targetEP = new IPEndPoint(targetIP, port);
            this.connected = false;
            this.closed = false;
            this.receiveLock = receiveLock;
        }

        /// <summary>
        /// Attempts connecting to the server
        /// </summary>
        public void Connect()
        {
            if (connected) return;
            targetSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Asynchronious call to ensure timeout on connection attempts
                IAsyncResult result = targetSocket.BeginConnect(targetIP, port, null, null);

                bool success = result.AsyncWaitHandle.WaitOne(500, true);

                if (!success)
                {
                    targetSocket.Close();
                    throw new SocketException();
                }
            }
            catch (SocketException e)
            {
                throw new ConnectionError("Unable to connect");
            }
            nstream = new NetworkStream(targetSocket);
            input = new StreamReader(nstream);
            output = new StreamWriter(nstream);
            output.AutoFlush = true;
            Handshake();
            // Connection succesfull, starting asynchronious receiving of messages
            connected = true;

            // Starting thread for maintaining periodical data flow to the server
            Thread pingThread = new Thread(StartPingThread);
            pingThread.IsBackground = true;
            pingThread.Start();
        }

        public void SetErrorCallback(Action errorCallback)
        {
            this.errorCallback = errorCallback;
        }

        /// <summary>
        /// Attempts a handshake during the connection to a server
        /// </summary>
        /// <remarks>Must be called from the Connect method!</remarks>
        private void Handshake()
        {
            nstream.ReadTimeout = 500;
            try
            {
                // First sent message
                output.WriteLine("HANDSHAKE TAOnline {0}", nickname);
                // First received message
                String s = input.ReadLine();
                if (s == null)
                {
                    throw new ConnectionError("Handshake error");
                }
                String[] parsed = s.Split(' ');
                if (parsed.Length < 2 || parsed[0] != "ACCEPT" || parsed[1] != "TAOnline")
                {
                    throw new ConnectionError("Handshake error");
                }
                // Second sent message
                output.WriteLine("ACK");
                // Second received message
                s = input.ReadLine();
                parsed = s.Split(' ');
                // Error message specifiing the type of error
                if (parsed[0] == "ERROR")
                {
                    StringBuilder message = new StringBuilder();
                    for (int i = 1; i < parsed.Length; i++)
                    {
                        message.Append(parsed[i]);
                        message.Append(" ");
                    }
                    throw new ConnectionError(message.ToString());
                }
                if ((parsed.Length != 2) || (parsed[0] != "ACK"))
                {
                    throw new ConnectionError("Handshake error");
                }
                try
                {
                    Pid = Int64.Parse(parsed[1]);
                }
                catch (FormatException e)
                {
                    throw new ConnectionError("Handshake error");
                }
            }
            catch (IOException)
            {
                throw new ConnectionError("Connection timeout");
            }
            nstream.ReadTimeout = Timeout.Infinite;
        }

        /// <summary>
        /// Asynchronious method which receives message and passes it to the parser in an infinite loop
        /// </summary>
        public async void Receive(MessageParser parser)
        {
            if (!connected) return;
            while (!closed)
            {
                String s = null;

                try
                {
                    s = await input.ReadLineAsync();
                    if (s == null) throw new IOException();
                }
                catch (IOException e)
                {
                    ForceDisconnect(true);
                    return;
                }
                lock (receiveLock)
                {
                    if (closed) return;
                    parser.ParseMessage(s);
                }

            }
        }

        /// <summary>
        /// Forces disconnection of the client from the server
        /// </summary>
        public void ForceDisconnect(bool showError)
        {
            if ((closed) || (!connected) || (targetSocket == null)) return;
            targetSocket.Close();
            closed = true;
            // If there is error callback method available and we are supposed to use it, we use it
            if ((errorCallback != null) && (showError)) errorCallback();
        }

        /// <summary>
        /// Sends message to using the connection to the server
        /// </summary>
        /// <param name="msg">Message to be sent</param>
        public void Send(string msg)
        {
            output.WriteLine(msg);
        }

        /// <summary>
        /// Main method of the thread for maintaining periodical data flow to the server
        /// </summary>
        public void StartPingThread()
        {
            while (!closed)
            {
                output.WriteLine("PING");
                Thread.Sleep(10000);
            }
        }

    }
}
