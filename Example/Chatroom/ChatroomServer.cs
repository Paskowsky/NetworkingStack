using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Chatroom
{
    class ChatroomServer
    {
        private NetworkingStack.Server.StackListener listener;
        private List<NetworkingStack.Server.StackClient> clients;


        public ChatroomServer(ushort port)
        {
            clients = new List<NetworkingStack.Server.StackClient>();

            listener = new NetworkingStack.Server.StackListener();

            listener.ClientStatusChanged += Listener_ClientStatusChanged;
            listener.ClientException += Listener_ClientException;
            listener.ClientReadData += Listener_ClientReadData;
            listener.ServerException += Listener_ServerException;
            listener.Listen(port);

            new Thread(new ThreadStart(EnjoyBroadcast)).Start();

        }
             

        private void EnjoyBroadcast()
        {
            while (listener.Listening)
            {
                Thread.Sleep(60 * 1000);
                Broadcast(Encoding.UTF8.GetBytes("Enjoy THE CHATROOM"));
            }
        }

        private bool ToClient(object sender, out NetworkingStack.Server.StackClient client)
        {
            client = sender as NetworkingStack.Server.StackClient;
            return client != null;
        }

        private void AddClient(object client)
        {
            NetworkingStack.Server.StackClient c;
            if (!ToClient(client, out c))
                return;

            lock (clients)
            {
                clients.Add(c);
            }
        }

        private void RemoveClient(object client)
        {
            NetworkingStack.Server.StackClient c;
            if (!ToClient(client, out c))
                return;
            
            lock (clients)
            {
                if (!clients.Contains(client))
                    return;

                clients.Remove(c);
            }
        }

        private NetworkingStack.Server.StackClient[] GetClients()
        {
            lock (clients)
            {
                return clients.ToArray();
            }
        }

        private void Broadcast(byte[] buffer, params object[] skipTags)
        {
            lock (clients)
            {
                foreach (NetworkingStack.Server.StackClient client in clients)
                {
                    if (!client.Connected)
                        continue;

                    if (!client.Encrypted)
                        continue;

                    if (client.Tag == null)
                        continue;

                    if (Array.IndexOf(skipTags, client.Tag) == -1)
                        client.Write(buffer);
                }
            }

        }

        private void BroadcastTo(byte[] buffer, params object[] tags)
        {
            lock (clients)
            {
                foreach (NetworkingStack.Server.StackClient client in clients)
                {
                    if (!client.Connected)
                        continue;

                    if (!client.Encrypted)
                        continue;

                    if (client.Tag == null)
                        continue;

                    if (Array.IndexOf(tags, client.Tag) != -1)
                        client.Write(buffer);
                }
            }
        }

        private void Listener_ClientReadData(object sender, byte[] buffer)
        {
            NetworkingStack.Server.StackClient client;
            if (!ToClient(sender, out client))
                return;

            string message = Encoding.UTF8.GetString(buffer);

            if (client.Tag == null)
            {
                client.Tag = message;

                message = client.Tag + " has joined THE CHATROOM";

                BroadcastTo(Encoding.UTF8.GetBytes("Welcome to THE CHATROOM " + client.Tag + " !"), client.Tag);
            }
            else
            {
                message = client.Tag + ":" + message;
            }

            buffer = Encoding.UTF8.GetBytes(message);

            Console.WriteLine(message);

            Broadcast(buffer, client.Tag);
        }

        private void Listener_ClientException(object sender, Exception ex)
        {
            Console.WriteLine(ex);
        }

        private void Listener_ClientStatusChanged(object sender, int status)
        {
            switch (status)
            {
                case 0://disconnected
                    Console.WriteLine(sender + " Disconnected");
                    RemoveClient(sender);
                    break;
                case 1://connected
                    Console.WriteLine(sender + " Connected");
                    AddClient(sender);
                    break;
                case 2://secure
                    Console.WriteLine(sender + " Secure");

                    break;
            }
        }

        private void Listener_ServerException(object sender, Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
