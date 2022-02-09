using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using NetMQ;
using NetMQ.Sockets;

namespace ZMQ_E
{
	class Requester
	{

        public void run()
        {
            //Console.WriteLine("Connecting to hello world server…");
            Program.print($"[{DateTime.Now}]\tConnecting to hello world server…\tTID: {Task.CurrentId}", ConsoleColor.Yellow);
            using (var requester = new RequestSocket())
            {
                requester.Connect("tcp://localhost:5555");

                int requestNumber;
                for (requestNumber = 0; requestNumber != 10; requestNumber++)
                {
                    //Console.WriteLine("Sending Hello {0}...", requestNumber);
                    Program.print($"[{DateTime.Now}]\tSending Hello {requestNumber}...\tTID: {Task.CurrentId}", ConsoleColor.Yellow);
                    requester.SendFrame("Hello");
                    string str = requester.ReceiveFrameString();
                    //Console.WriteLine("Received World {0}", requestNumber);
                    Program.print($"[{DateTime.Now}]\tReceived World {requestNumber}\tTID: {Task.CurrentId}", ConsoleColor.Yellow);
                    Task.Delay(100).Wait();
                    str = requester.ReceiveFrameString();
                }
            }
        }
    }

}
