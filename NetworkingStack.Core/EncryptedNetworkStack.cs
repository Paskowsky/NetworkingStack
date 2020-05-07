using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace NetworkingStack.Core
{
    public class EncryptedNetworkStack : HashedNetworkStack
    {
        private RSACryptoServiceProvider rsa;
        private RijndaelManaged rijndael;
        private RNGCryptoServiceProvider rng;

        public bool Encrypted
        {
            get
            {
                if (rsa == null || rijndael == null)
                    return false;
                return true;
            }
        }

        public EncryptedNetworkStack() : base()
        {
            rng = new RNGCryptoServiceProvider();
        }

        public void OpenAsClient()
        {
            rijndael = null;
            rsa = new RSACryptoServiceProvider(4096);
            base.Open();
            base.Write(rsa.ExportCspBlob(false));
        }

        public void OpenAsServer()
        {
            rsa = null;
            InitializeRijndael();
            base.Open();
        }

        public override void Open()
        {
            throw new NotSupportedException();
        }

        public override byte[] Read()
        {
            byte[] buffer = base.Read();

            if (Encrypted)
            {
                return DecryptData(buffer);
            }
            else
            {
                if (rsa == null)
                {
                    HandleClientHandshake(buffer);
                }
                else
                {
                    HandleServerHandshake(buffer);
                }
                return null;
            }
        }

        public override void Write(byte[] data)
        {
            base.Write(EncryptData(data));
        }

        private void HandleClientHandshake(byte[] buffer)
        {
            rsa = new RSACryptoServiceProvider();

            rsa.ImportCspBlob(buffer);

            byte[] keyBlob = new byte[rijndael.Key.Length + rijndael.IV.Length];

            Buffer.BlockCopy(rijndael.Key, 0, keyBlob, 0, rijndael.Key.Length);
            Buffer.BlockCopy(rijndael.IV, 0, keyBlob, rijndael.Key.Length, rijndael.IV.Length);

            keyBlob = rsa.Encrypt(keyBlob, true);

            base.Write(keyBlob);

            OnStatusChange(2);
        }

        private void HandleServerHandshake(byte[] buffer)
        {
            byte[] keyBlob = rsa.Decrypt(buffer, true);

            InitializeRijndael();

            byte[] key = new byte[rijndael.Key.Length];
            byte[] iv = new byte[rijndael.IV.Length];

            Buffer.BlockCopy(keyBlob, 0, key, 0, key.Length);
            Buffer.BlockCopy(keyBlob, key.Length, iv, 0, iv.Length);

            rijndael.Key = key;
            rijndael.IV = iv;

            OnStatusChange(2);
        }

        private byte[] EncryptData(byte[] data)
        {
            if (!Encrypted)
                return data;

            byte[] iv = new byte[rijndael.IV.Length];

            rng.GetBytes(iv);

            using (ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, iv))
            {
                List<byte> buffer = new List<byte>();
                buffer.AddRange(encryptor.TransformFinalBlock(data, 0, data.Length));
                buffer.AddRange(iv);
                return buffer.ToArray();
            }
        }

        private byte[] DecryptData(byte[] data)
        {
            if (!Encrypted)
                return data;

            byte[] iv = new byte[rijndael.IV.Length];

            Buffer.BlockCopy(data, data.Length - iv.Length, iv, 0, iv.Length);

            using (ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, iv))
            {
                return decryptor.TransformFinalBlock(data, 0, data.Length - iv.Length);
            }
        }

        private void InitializeRijndael()
        {
            rijndael = new RijndaelManaged();
            rijndael.KeySize = 256;
            rijndael.BlockSize = 256;
            rijndael.Padding = PaddingMode.PKCS7;
            rijndael.Mode = CipherMode.CBC;
            rijndael.GenerateKey();
            rijndael.GenerateIV();
        }

    }
}
