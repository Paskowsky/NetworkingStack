using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Chatroom
{
    class Program
    {
        static void Main(string[] args)
        {
           
            ushort port;

            do
            {
                Console.WriteLine("Enter Port :");
            } while (!ushort.TryParse(Console.ReadLine(), out port));
            
            new ChatroomServer(port);

            Thread.CurrentThread.Join();
        }
    }
}
