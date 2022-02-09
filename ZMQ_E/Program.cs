using NetMQ;
using NetMQ.Sockets;
using System;
using System.Threading.Tasks;

namespace ZMQ_E
{
	class Program
	{
		static readonly object _console = new object();
		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			
			//TaskScheduler

			/*
			var pub = new Publisher();
			var t_pub = Task.Run(() => pub.run('B'));
			var sub = new Subscriber();
			var t_sub = Task.Run(() => sub.run());
			Task.Run(() => sub.run(connectionmode: Connectionmode.inproc));
			*/
			var t_req = Task.Run(() => new Requester().run());
			var t_resp = Task.Run(() => new Responder().run());
			/*
			for (int i = 0; i < 10; i++)
			{
				Task.Run(() => sub.run(connectionmode: Connectionmode.tcp));
			}
			
			//Task.Run(() => pub.run('A'));
			Task.WaitAll(t_pub, t_sub);
			*/
			Task.WaitAll(t_req, t_resp);
		}

		public static void print(object output, ConsoleColor consoleColor)
		{
			lock (_console) {
				Console.ForegroundColor = consoleColor;
				Console.WriteLine(output);
				Console.ResetColor();
			}
		}
	}
}
