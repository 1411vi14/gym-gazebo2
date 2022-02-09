using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZMQ_E
{
	class Responder
	{
        public void run()
        {
            using (var responder = new ResponseSocket())
            {
                responder.Bind("tcp://*:5555");

                while (true)
                {
                    string str = responder.ReceiveFrameString();
                    Program.print($"[{DateTime.Now}]\tReceived Hello\tTID: {Task.CurrentId}", ConsoleColor.Blue);
                    //Console.WriteLine("Received Hello");
                    //Thread.Sleep(1000);  //  Do some 'work'
                    responder.SendFrame("World");
                    Task.Delay(1000).Wait();
                    responder.SendFrame("World32");
                }
            }
        }
    }
}
