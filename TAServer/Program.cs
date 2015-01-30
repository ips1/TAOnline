using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;

namespace TAServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- TAOnline Server ---");
            Console.WriteLine("Initializing...");

            MessageQueue queue = new MessageQueue();


            Server server = new Server(queue);
            try
            {
                server.Start();
            }
            catch (SocketException e)
            {
                Console.WriteLine("ERROR: The server cannot be started (maybe another app is using the port " + Server.PortNo + ")");
                Console.ReadLine();
                return;
            }

            server.Run();
            
            MessageParser mp = new MessageParser(server, queue);

            Thread messageThread = new Thread(mp.RunParser);
            messageThread.Start();

            while (true)
            {
                string cmd = Console.ReadLine();
                if (cmd == "EXIT")
                {
                    return;
                }
                queue.Add(new Message(cmd));
            }
        }

    }
}
