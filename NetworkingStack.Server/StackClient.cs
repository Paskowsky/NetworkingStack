using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NetworkingStack.Server
{
    public class StackClient : Core.EncryptedNetworkStack
    {
        private StackListener listener;

        public StackClient(StackListener listener, Socket socket)
        {
            this.listener = listener;

            this.Socket = socket;

            this.ReadBuffer += listener.OnClientReadData;

            this.Exception += listener.OnClientException;

            this.StatusChange += listener.OnClientStatusChanged;

            this.OpenAsServer();
        }
       
    }
}
