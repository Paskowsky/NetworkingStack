using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NetworkingStack.Core
{
    public class HashedNetworkStack : NetworkStack
    {
        public override byte[] Read()
        {
            return ReadDataHash(base.Read());
        }

        public override void Write(byte[] data)
        {
            base.Write(WriteDataHash(data));
        }

        private static byte[] WriteDataHash(byte[] data)
        {
            List<byte> buffer = new List<byte>(data);

            using (SHA1CryptoServiceProvider hash = new SHA1CryptoServiceProvider())
            {
                buffer.AddRange(hash.ComputeHash(data));
            }

            return buffer.ToArray();
        }

        private static byte[] ReadDataHash(byte[] data)
        {
            byte[] hashData;
            using (SHA1CryptoServiceProvider hash = new SHA1CryptoServiceProvider())
            {
                hashData = hash.ComputeHash(data);

                hashData = hash.ComputeHash(data, 0, data.Length - hashData.Length);

                for (int i = 0; i < hashData.Length; i++)
                {
                    if (hashData[i] != data[i + data.Length - hashData.Length])
                        throw new Exception();
                }
            }

            byte[] buffer = new byte[data.Length - hashData.Length];

            Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length);

            return buffer;
        }
    }
}
