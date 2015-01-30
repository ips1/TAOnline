using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TAServer
{
    /// <summary>
    /// One message in the queue for the server to process
    /// </summary>
    /// <remarks>
    /// Sender contains message sender, null sender represents local message (console command)
    /// </remarks>
    class Message
    {
        public ClientHandler Sender { get; private set; }
        public string Content { get; private set; }

        /// <summary>
        /// Creates message with specified sender client
        /// </summary>
        /// <param name="sender">Sender client</param>
        /// <param name="content">Content of the message</param>
        public Message(ClientHandler sender, string content)
        {
            this.Sender = sender;
            this.Content = content;
        }

        /// <summary>
        /// Creates message without specified sender client (local message)
        /// </summary>
        /// <param name="content">Content of the message</param>
        public Message(string content)
        {
            this.Sender = null;
            this.Content = content;
        }
    }

    /// <summary>
    /// Queue containing all the messages for server to process
    /// </summary>
    class MessageQueue
    {
        private List<Message> queue = new List<Message>();
        private object queueLock = new object();

        /// <summary>
        /// Adds new message to the queue
        /// </summary>
        /// <param name="msg">Message to be added</param>
        public void Add(Message msg)
        {
            lock (queueLock)
            {
                queue.Add(msg);
                Monitor.Pulse(queueLock);
            }
        }

        /// <summary>
        /// Gets one message from the queue
        /// </summary>
        /// <remarks>
        /// If the queue is empty, blocks itself and waits for message to come
        /// </remarks>
        /// <returns>One message from the queue</returns>
        public Message Get()
        {
            Message msg;
            lock (queueLock)
            {
                while (queue.Count == 0)
                {
                    Monitor.Wait(queueLock);
                }
                msg = queue[0];
                queue.RemoveAt(0);
            }
            return msg;
        }
    }
}
