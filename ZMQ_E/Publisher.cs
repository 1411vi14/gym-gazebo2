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
	class Publisher
	{
		public void run(char identifier = 'A')
		{
            using (var publisher = new PublisherSocket())
            {
                publisher.Bind("tcp://*:5556");
                publisher.Bind("inproc://inproc-demo");
                //publisher.Bind("pgm://224.0.0.1:5555");

                int i = 0;

                while (true) // true
                {
                    byte[] bytey_topic;
					switch (new Random().Next(3))
					{
                        case 0:
                            bytey_topic = Encoding.UTF8.GetBytes("A");
                            break;
                        case 1:
                            bytey_topic = Encoding.UTF8.GetBytes("B");
                            break;
                        case 2:
                            UInt64 topic_l = 12;
                            bytey_topic = BitConverter.GetBytes(topic_l);
                            break;
                        default:
                            bytey_topic = new byte[8];
                            break;
					}
                    publisher
                        .SendMoreFrame(bytey_topic) // Topic
                        .SendFrame(identifier + i.ToString()); // Message
                    string topic;
                    if (bytey_topic.Length == sizeof(UInt64))
                    {
                        topic = BitConverter.ToUInt64(bytey_topic).ToString();
                    }
                    else
                    {
                        topic = Encoding.UTF8.GetString(bytey_topic);
                    }

                    Program.print($"[{DateTime.Now}]\tPub: Topic: {topic}\tMessage: {identifier + i.ToString()}\tTID: {Task.CurrentId}", ConsoleColor.Magenta);
                    i++;
                    
                    /*bool data = publisher.TryReceiveFrameString(new TimeSpan(100), out string Response);
					if (data)
					{
                        Program.print($"[{DateTime.Now}]\tPub-Response: Topic: A\tMessage: {Response}\tTID: {Task.CurrentId}", ConsoleColor.Magenta);
					}*/
                    Thread.Sleep(1000);
                }
            }

        }
    }
}
