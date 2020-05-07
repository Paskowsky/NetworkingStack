using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkingStack.Core
{
    public class NetworkStack : NetworkStackBase
    {
        private static readonly int bufferSize = 1024 * 64;
        private static readonly int maxPacketSize = 1024 * 1024;

        public override event ReadBufferHandler ReadBuffer;
        public override event StatusChangedHandler StatusChange;
        public override event ExceptionHandler Exception;

        public bool Connected { get { return Socket == null ? false : Socket.Connected; } }

        public IPAddress IpAddress { get; private set; }

        public Socket Socket { get; set; }

        public object Tag { get; set; }

       
        private Random random;
        private bool writing;
        private byte[] writeBuffer;
        private int writeIndex;
        private byte[] readBuffer;
        private int readIndex;
        private byte[] tempBuffer;
        private Queue<byte[]> writeQueue;

        public NetworkStack()
        {
            this.random = new Random();
            this.writeQueue = new Queue<byte[]>();
            this.tempBuffer = new byte[bufferSize];
            this.readBuffer = new byte[0];
            this.writeBuffer = new byte[0];
            this.readIndex = 0;
            this.writeIndex = 0;
        }

        public override void Open()
        {
            IpAddress = ((IPEndPoint)Socket.RemoteEndPoint).Address;
            WaitNext();
            OnStatusChange(1);
        }

        public override void Close()
        {
            if (Socket != null)
                Socket.Close();

            //Socket = null;

            if (writeQueue != null)
            {
                lock (writeQueue)
                {
                    writeQueue.Clear();
                }
            }

            writeQueue = null;
            lock (writeBuffer)
                writeBuffer = new byte[0];

            lock (readBuffer)
                readBuffer = new byte[0];

            lock (tempBuffer)
                tempBuffer = new byte[0];

            Tag = null;

            OnStatusChange(0);
        }

        public override byte[] Read()
        {
            //copy buffer over
            return new List<byte>(readBuffer).ToArray();
        }

        public override void Write(byte[] buffer)
        {
            try
            {
                if (Socket == null)
                    return;

                lock (writeQueue)
                {
                    writeQueue.Enqueue(buffer);
                }

                if (!writing)
                {
                    writing = true;
                    HandleWriteQueue();
                }

            }
            catch (Exception ex)
            {
                OnException(ex);
                Close();
            }
        }

        public override void OnStatusChange(int status)
        {
            if (StatusChange != null)
            {
                StatusChange(this, status);
            }
        }

        public override void OnException(Exception ex)
        {
            if (Exception != null)
            {
                Exception(this, ex);
            }
        }

        public override string ToString()
        {
            return IpAddress == null ? "Unknown" : IpAddress.ToString();
        }
        
        private void ProcessReceive(IAsyncResult r)
        {
            try
            {
                int receivedLen = Socket.EndReceive(r);
                ContinueRead(0, receivedLen);
                WaitNext();
            }
            catch (Exception ex)
            {
                OnException(ex);
                Close();
            }

        }

        private void ProcessSend(IAsyncResult r)
        {
            try
            {
                int sentLen = Socket.EndSend(r);
                ContinueWrite(sentLen);
            }
            catch (Exception ex)
            {
                OnException(ex);
                Close();
            }
        }

        private void WaitNext()
        {
            lock (tempBuffer)
                this.Socket.BeginReceive(tempBuffer, 0, tempBuffer.Length, SocketFlags.None, ProcessReceive, null);
        }

        private void OnRead(byte[] buffer)
        {
            if (buffer == null)
                return;

            if (ReadBuffer != null)
            {
                ReadBuffer(this, buffer);
            }
        }

        private void ContinueRead(int index, int len)
        {
            int bytesToRead;
            lock (readBuffer)
            {
                lock (tempBuffer)
                {
                    if (readIndex >= readBuffer.Length)
                    {
                        readIndex = 0;
                        if (len < 8)
                        {
                            throw new Exception("Corrupted");
                        }

                        int packet_len = ReadHeader(tempBuffer, ref index);

                        if (packet_len > maxPacketSize || packet_len < 0)
                        {
                            throw new Exception("Corrupted");
                        }

                        Array.Resize(ref readBuffer, packet_len);
                    }

                    bytesToRead = Math.Min(readBuffer.Length - readIndex, len - index);

                    Buffer.BlockCopy(tempBuffer, index, readBuffer, readIndex, bytesToRead);
                }

                readIndex += bytesToRead;

                if (readIndex >= readBuffer.Length)
                {
                    OnRead(Read());
                }
            }

            if (bytesToRead < (len - index))
            {
                ContinueRead(index + bytesToRead, len);
            }

        }

        private void ContinueWrite(int len)
        {
            lock (writeBuffer)
            {
                writeIndex += len;
                bool endOfStream = false;

                if (writeIndex >= writeBuffer.Length)
                {
                    endOfStream = true;
                }

                lock (writeQueue)
                {
                    if (writeQueue.Count == 0 && endOfStream)
                    {
                        writing = false;
                    }
                }
            }

            if (writing)
            {
                HandleWriteQueue();
            }


        }

        private void HandleWriteQueue()
        {
            lock (writeBuffer)
            {
                if (writeIndex >= writeBuffer.Length)
                {
                    writeIndex = 0;
                    lock (writeQueue)
                    {
                        if (writeQueue.Count == 0)
                            return;

                        writeBuffer = WriteHeader(writeQueue.Dequeue());
                    }
                }

                int bytesToWrite = Math.Min(writeBuffer.Length - writeIndex, bufferSize);

                Socket.BeginSend(writeBuffer, writeIndex, bytesToWrite, SocketFlags.None, ProcessSend, null);
            }
        }

        private byte[] WriteHeader(byte[] buffer)
        {
            byte[] data = new byte[buffer.Length + sizeof(int) + sizeof(int)];
            int key = random.Next();
            Buffer.BlockCopy(BitConverter.GetBytes((int)buffer.Length ^ key), 0, data, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes((int)key), 0, data, sizeof(int), sizeof(int));
            Buffer.BlockCopy(buffer, 0, data, sizeof(int) + sizeof(int), buffer.Length);
            return data;
        }

        private int ReadHeader(byte[] buffer, ref int index)
        {
            int packet_len = BitConverter.ToInt32(buffer, index);
            index += 4;
            int packet_len_key = BitConverter.ToInt32(buffer, index);
            index += 4;
            return packet_len ^ packet_len_key;
        }
    }
}
