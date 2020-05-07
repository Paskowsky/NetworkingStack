using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChatroomClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter Host :");
            string host = Console.ReadLine();

            ushort port;

            do
            {
                Console.WriteLine("Enter Port :");
            } while (!ushort.TryParse(Console.ReadLine(), out port));

            new ChatroomClient(host, port);

            Thread.CurrentThread.Join();
        }
    }
}
