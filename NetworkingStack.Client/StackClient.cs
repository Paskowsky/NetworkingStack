using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkingStack.Client
{
    public class StackClient : Core.EncryptedNetworkStack
    {
        private static readonly int sendBufferSize = 64 * 1024;
        private static readonly int receiveBufferSize = 64 * 1024;

        public StackClient() : base()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveBufferSize = sendBufferSize,
                SendBufferSize = receiveBufferSize,
            };
        }

        public bool Connect(IPAddress address, ushort port)
        {
            try
            {
                Socket.Connect(address, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            this.OpenAsClient();

            return Socket.Connected;
        }

        public bool Connect(string host, ushort port)
        {
            IPAddress[] ipAddresses = Dns.GetHostAddresses(host);

            foreach (IPAddress ipAddress in ipAddresses)
            {
                if (Connect(ipAddress, port))
                {
                    break;
                }
            }

            if (!Socket.Connected)
            {
                OnStatusChange(0);
            }

            return Socket.Connected;
        }

    }
}
