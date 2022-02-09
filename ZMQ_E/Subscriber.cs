using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZMQ_E
{
	class Subscriber
	{
		public void run(Connectionmode connectionmode = Connectionmode.tcp)
		{
			using (var subscriber = new SubscriberSocket())
			{
				switch (connectionmode)
				{
					case Connectionmode.tcp:
						subscriber.Connect("tcp://127.0.0.1:5556");
						break;
					case Connectionmode.inproc:
						subscriber.Connect("inproc://inproc-demo");
						break;
					case Connectionmode.pgm:
						subscriber.Connect("pgm://224.0.0.1:5555");
						break;
				}

				UInt64 int_topic_id = 12;
				byte[] bytes_topic_id = BitConverter.GetBytes(int_topic_id);
				subscriber.Subscribe("A");
				subscriber.Subscribe(bytes_topic_id);

				while (true)
				{
					byte[] bytes_topic = subscriber.ReceiveFrameBytes(out bool more);
					string topic;
					if (bytes_topic.Length == sizeof(UInt64))
					{
						topic = BitConverter.ToUInt64(bytes_topic).ToString();
					} else
					{
						topic = Encoding.UTF8.GetString(bytes_topic);
					}
					
					

					if (more)
					{
						var msg = subscriber.ReceiveFrameString();
						//Console.WriteLine("From Publisher: {0} {1}", topic, msg);

						Program.print($"[{DateTime.Now}]\tSub: Topic: {topic}\tMessage: {msg}\tTID: {Task.CurrentId}", ConsoleColor.Green);
						//subscriber.SendFrame(msg);
						//Program.print($"[{DateTime.Now}]\tSub-Request: Topic: {topic}\tMessage: {msg}\tTID: {Task.CurrentId}", ConsoleColor.Green);

					}


				}
			}

		}
	}

	enum Connectionmode
	{
		tcp,
		inproc,
		pgm
	}
}
