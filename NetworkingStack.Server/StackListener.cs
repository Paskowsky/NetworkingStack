using NetworkingStack.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkingStack.Server
{
    public class StackListener
    {
        private static readonly int sendBufferSize = 64 * 1024;
        private static readonly int receiveBufferSize = 64 * 1024;
        
        private Socket server;

        public bool Listening { get; private set; }

        public void Listen(ushort port)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);

            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendBufferSize = sendBufferSize,
                ReceiveBufferSize = receiveBufferSize,
            };

            server.Bind(ipEndPoint);
            server.Listen(500);
            server.BeginAccept(Process, null);
            Listening = true;
        }

        private void Process(IAsyncResult r)
        {
            try
            {
                new StackClient(this, server.EndAccept(r));
            }
            catch (Exception ex)
            {
                OnServerException(ex);
            }
            finally
            {
                server.BeginAccept(Process, null);
            }
        }

        public event ExceptionHandler ServerException;
        
        public event StatusChangedHandler ClientStatusChanged;

        public event ExceptionHandler ClientException;

        public event ReadBufferHandler ClientReadData;

        internal void OnClientReadData(object sender, byte[] buffer)
        {
            if (ClientReadData != null)
            {
                ClientReadData(sender, buffer);
            }
        }

        internal void OnClientException(object sender, Exception ex)
        {
            if (ClientException != null)
            {
                ClientException(sender, ex);
            }
        }

        internal void OnClientStatusChanged(object sender, int status)
        {
            if (ClientStatusChanged != null)
            {
                ClientStatusChanged(sender, status);
            }
        }

        internal void OnServerException(Exception ex)
        {
            if(ServerException != null)
            {
                ServerException(this, ex);
            }
        }
    }
}
