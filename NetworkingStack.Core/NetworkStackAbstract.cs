using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkingStack.Core
{
    public delegate void ReadBufferHandler(object sender, byte[] buffer);
    public delegate void StatusChangedHandler(object sender, int status);
    public delegate void ExceptionHandler(object sender, Exception ex);

    public abstract class NetworkStackBase
    {
        public abstract event ReadBufferHandler ReadBuffer;
        public abstract event StatusChangedHandler StatusChange;
        public abstract event ExceptionHandler Exception;

        public abstract void Open();

        public abstract void Close();
        
        public abstract byte[] Read();

        public abstract void Write(byte[] buffer);

        public abstract void OnStatusChange(int status);

        public abstract void OnException(Exception status);
    }
}
