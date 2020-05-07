using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChatroomClient
{
    class ChatroomClient
    {
        private NetworkingStack.Client.StackClient client;

        public string Host { get; private set; }
        public ushort Port { get; private set; }

        public ChatroomClient(string host, ushort port)
        {
            Host = host;
            Port = port;
            DoConnect();

        }

        public void StartChatLoop()
        {
            Thread.Sleep(1000);

            while (true)
            {
                
                if (!client.Connected)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (!client.Encrypted)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (client.Tag == null)
                {
                    Console.WriteLine("Enter nickname :");
                    client.Tag = Console.ReadLine();
                    client.Write(Encoding.UTF8.GetBytes(client.Tag as string));
                    continue;
                }

                string input = Console.ReadLine();
                client.Write(Encoding.UTF8.GetBytes(input));
            }
        }

        private void DoConnect()
        {
            client = new NetworkingStack.Client.StackClient();
            client.Exception += Client_Exception;
            client.ReadBuffer += Client_ReadBuffer;
            client.StatusChange += Client_StatusChange;
            client.Connect(Host, Port);
        }

        private void Client_StatusChange(object sender, int status)
        {
            switch (status)
            {
                case 0:
                    Console.WriteLine("Disconnected");
                    DoConnect();
                    break;
                case 1:
                    Console.WriteLine("Connected");
                    break;
                case 2:
                    Console.WriteLine("Secure");
                    new Thread(new ThreadStart(StartChatLoop)).Start();
                    break;
            }
        }

        private void Client_ReadBuffer(object sender, byte[] buffer)
        {
            string message = Encoding.UTF8.GetString(buffer);
            
            Console.WriteLine(message);

            //say my name!
            if (message.ToLowerInvariant().Contains("heisenberg"))
            {
                Console.Clear();
            }
        }

        private void Client_Exception(object sender, Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
