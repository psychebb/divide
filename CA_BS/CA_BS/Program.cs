using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace CA_BS
{
    class Program
    {
        public static System.Timers.Timer timer1 = new System.Timers.Timer(5000);

        private static void AutoRead(client client)
        {
            timer1.Elapsed += (s_, e_) => client.ReadFromBuffer();
            timer1.AutoReset = true;
            timer1.Enabled = true;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("please input the name of the new client:");
            string client_name=Console.ReadLine();
            client client_one = new client(client_name);
            client_one.ClientInitial();
            //Console.WriteLine("set the timer:");
           // int interval =int.Parse(Console.ReadLine());
            //client_one.SetTimer(interval);
          // client_one.autoSend("haha");
            AutoRead(client_one);
            client_one.ServerResponse();
            Console.WriteLine("the number of the clients is :{0}", client_one.clients.Count);

            //Console.WriteLine("set the timer1:");
            
            int interval1 = int.Parse(Console.ReadLine());
           // timer1.Interval = interval1;
           
        }
    }
}

