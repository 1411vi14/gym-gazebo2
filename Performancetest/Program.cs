using System;
using System.Threading;
using ROS2;
using std_msgs;
using NetMQ;
using NetMQ.Sockets;
using System.Diagnostics;
using UDP_Com;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ROS2Test
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			UDP_Com uDP_Com = new UDP_Com(1000);
			//string jsonString = JsonSerializer.Serialize(uDP_Com.delays);
			StringBuilder sb = new StringBuilder();
            for (int i = 0; i < uDP_Com.delays.Count; i++)
            {
				sb.Append(uDP_Com.delays[i] + "\n");
			}
			string stringdata = sb.ToString();
			Console.WriteLine(stringdata);
			Console.ReadLine();
			//doZMQ_Check(10);
			/*
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			stopwatch.Restart();
			var ZeroMQ_ = new ZeroMQ();
			stopwatch.Stop();
			Console.WriteLine("ZeroMQ: " + stopwatch.ElapsedMilliseconds + "ms");

			stopwatch.Restart();
			var UDP_Com_ = new UDP_Com();
			stopwatch.Stop();
			Console.WriteLine("UDP_Com: " + stopwatch.ElapsedMilliseconds + "ms");

			stopwatch.Restart();
			var ros_ = new ROS();
			stopwatch.Stop();
			Console.WriteLine("ROS: " + stopwatch.ElapsedMilliseconds + "ms");
			*/
		}
		//31.01.2022 21:20 - 21:56
		//22:00 - 
		static void doZMQ_Check(int count)
		{
			PublisherSocket publisher = new PublisherSocket("@tcp://*:5556");
			SubscriberSocket subscriber = new SubscriberSocket();
			subscriber.Connect("tcp://127.0.0.1:5556");

			for (int i = 0; i < count; i++)
            {
				DateTime dateTime = DateTime.Now;
				publisher.SendMoreFrame("TopicA Pub1").SendFrame(dateTime.Ticks.ToString());

				bool more, rec;
				int part = 0;
				do
                {
					part++;
					rec = publisher.TryReceiveFrameString(new TimeSpan(0, 0, 0, 0, 150), out string topic, out more);
                    if (rec)
                    {
						Console.WriteLine($"Publisher received Part {i}:\t" + topic);
                    }
				} while (more);

				part = 0;
				do
				{
					part++;
					rec = subscriber.TryReceiveFrameString(new TimeSpan(0, 0, 0, 0, 150), out string topic, out more);
					if (rec)
					{
						Console.WriteLine($"Subscriber received Part {i}:\t" + topic);
					}
				} while (more);

            }
		}
	}



	class ROS
    {
		static int i = 1;
		static Publisher<std_msgs.msg.String> chatter_pub;
		static Publisher<std_msgs.msg.String> chatter_sub;
		static int received = 0;
		public ROS()
        {
			Console.WriteLine("Talker starting");
			Ros2cs.Init();
			INode node = Ros2cs.CreateNode("talker");
			chatter_pub = node.CreatePublisher<std_msgs.msg.String>("chatter");
			std_msgs.msg.String msg = new std_msgs.msg.String();

			ISubscription<std_msgs.msg.String> chatter_sub = node.CreateSubscription<std_msgs.msg.String>(
				"chatter", callback: chatter_sub_cb);

            for (int i = 0; i < 1000; i++)
            {
				msg.Data = "Hello World: " + i;
				chatter_pub.Publish(msg);
				
			}
			Ros2cs.Spin(node);
			Ros2cs.Shutdown();
		}

		private static void chatter_sub_cb(std_msgs.msg.String msg)
		{
			Console.WriteLine("I heard: [" + msg.Data + "]");
			//Thread.Sleep(1);
			received++;
		}
	}

	class ZeroMQ
    {
		PublisherSocket publisher;
		SubscriberSocket subscriber;
		static int received = 0;
		CancellationToken CancellationToken;
		CancellationTokenSource cancellationTokenSource;
		public ZeroMQ()
        {
			publisher = new PublisherSocket("@tcp://*:5556");
			subscriber = new SubscriberSocket(">tcp://127.0.0.1:5556");
			subscriber.Subscribe("chatter");
			cancellationTokenSource = new CancellationTokenSource();
			CancellationToken = cancellationTokenSource.Token;
			cancellationTokenSource.CancelAfter(new TimeSpan(0,0,30));

			using (var runtime = new NetMQRuntime())
			{
				runtime.Run(CancellationToken, PublisherAsync(), SubscriberAsync());
			}

		}

		async Task PublisherAsync()
		{
			using (var publisher = new PublisherSocket("@tcp://*:5556"))
			{
				for (int i = 0; i < 1000; i++)
				{
					publisher.SendMoreFrame("chatter") // Topic
					.SendFrame("Hello World: " + i.ToString()); // Message
				}
			}
		}

		async Task SubscriberAsync()
		{
			using (var subscriber = new SubscriberSocket(">tcp://127.0.0.1:5556"))
			{
				for (int i = 0; i < 1000; i++)
				{
					var (message, more) = await subscriber.ReceiveFrameStringAsync();
					Console.WriteLine("I heard: [" + message + "]");
					received++;
					// TODO: process reply

					// await Task.Delay(100);
				}
				cancellationTokenSource.Cancel();
			}
		}

	}

	class UDP_Com
    {
		public List<double> delays;
		readonly int count;
		static int received = 0;
		CancellationTokenSource cancellationToken;
		public UDP_Com(int count)
        {
			this.count = count;
			cancellationToken = new CancellationTokenSource();
			Transceiver sender = new Transceiver(cancellationToken.Token);
			Transceiver receiver = new Transceiver(cancellationToken.Token);
			IPEndPoint local_End = new IPEndPoint(IPAddress.Loopback, sender.Port);
			delays = new List<double>(count);
			sender.createTopic(1);
			receiver.subscribe(1,local_End);
			//sender.subscribeToTopic(1, local_End);
			Thread.Sleep(10);
			receiver.OnDataReceived += chatter_sub_cb;
            for (int i = 0; i < count; i++)
            {
				DateTime send_time = DateTime.Now;
				long send_ticks = send_time.Ticks;
				byte[] bytes = BitConverter.GetBytes(send_ticks);

				//byte[] bytes = Encoding.UTF8.GetBytes(now);

				sender.send(bytes, bytes.Length, 1);
				Thread.Sleep(10);
			}
			/*
            while (!cancellationToken.IsCancellationRequested)
            {
				Thread.Sleep(100);
            }
			*/
			Thread.Sleep(5000);

		}

        private void chatter_sub_cb(IPEndPoint iPEndPoint, string? command, byte[] data)
        {
			DateTime received_time = DateTime.Now;
			//var msg = Encoding.UTF8.GetString(data, 0, data.Length);
			long send_ticks = BitConverter.ToInt64(data, 0);
			DateTime send_time = new DateTime(send_ticks);
			TimeSpan timediff = received_time - send_time;
			//Console.WriteLine("I heard: [" + msg + "]");
			delays.Add(timediff.TotalMilliseconds);
			received++;
            if (received == count)
            {
				cancellationToken.Cancel();

			}
		}
    }

}