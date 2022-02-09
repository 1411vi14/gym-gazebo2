using System;
using System.Threading;
using ROS2;
using std_msgs;

namespace ROS2Test
{
	class Program
	{
        static int i = 1;
        static Publisher<std_msgs.msg.String> chatter_pub;

        static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

            Console.WriteLine("Talker starting");
            Ros2cs.Init();
            INode node = Ros2cs.CreateNode("talker");
            chatter_pub = node.CreatePublisher<std_msgs.msg.String>("chatter");
            std_msgs.msg.String msg = new std_msgs.msg.String();

            ISubscription<std_msgs.msg.String> chatter_sub = node.CreateSubscription<std_msgs.msg.String>(
                "chatter", callback: chatter_sub_cb);


            msg.Data = "Hello World: " + i;
            i++;
            Console.WriteLine(msg.Data);
            chatter_pub.Publish(msg);
			Console.WriteLine("Test");

            Ros2cs.Spin(node);
            Ros2cs.Shutdown();
        }

		private static void chatter_sub_cb(std_msgs.msg.String msg)
		{
            Console.WriteLine("I heard: [" + msg.Data + "]");
            //Thread.Sleep(1);
            if (Ros2cs.Ok())
			{
                msg = new std_msgs.msg.String();
                msg.Data = "Hello World: " + i;
                i++;
                Console.WriteLine(msg.Data);
                chatter_pub.Publish(msg);
			}
            
        }
	}
}
