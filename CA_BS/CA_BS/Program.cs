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
        public static System.Timers.Timer timer2 = new System.Timers.Timer(100);

        private static void AutoRead(client client)
        {
            timer1.Elapsed += (s_, e_) => client.ReadFromBuffer_ClientToOutside();
            timer1.AutoReset = true;
            timer1.Enabled = true;
        }

        private static void AutoWrite(client client)
        {
            timer2.Elapsed += (s_, e_) => InstoreData(client);
            timer2.AutoReset = true;
            timer2.Enabled = true;
        }

        private static void InstoreData(client client)
        {
            //for (int i = 0; i < 10; i++)
            //{
            //    byte[] a = System.Text.Encoding.Unicode.GetBytes("state");
            //    byte[] b = System.Text.Encoding.Unicode.GetBytes("key");
            //    byte[] c = System.Text.Encoding.Unicode.GetBytes("lab");
            //    byte[] d = System.Text.Encoding.Unicode.GetBytes("of");
            //    byte[] e = System.Text.Encoding.Unicode.GetBytes("integrated");
            //    byte[] f = System.Text.Encoding.Unicode.GetBytes("service");
            //    byte[] g = System.Text.Encoding.Unicode.GetBytes("networks");
            //    client.WriteToBuffer_OutsideToClient(a);
            //    client.WriteToBuffer_OutsideToClient(b);
            //    client.WriteToBuffer_OutsideToClient(c);
            //    client.WriteToBuffer_OutsideToClient(d);
            //    client.WriteToBuffer_OutsideToClient(e);
            //    client.WriteToBuffer_OutsideToClient(f);
            //    client.WriteToBuffer_OutsideToClient(g);
            //}
            Random rn = new Random();
            //byte[] data = BitConverter.GetBytes(rn.Next(100));
            byte[] data = System.Text.Encoding.Unicode.GetBytes(rn.Next(100).ToString() + "qubaobao");
            
            client.WriteToBuffer_OutsideToClient(data);
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
            //Random rn = new Random();
            //byte[] data=BitConverter.GetBytes(rn.Next(2)) ;
            //client_one.WriteToBuffer_OutsideToClient(data);
            AutoWrite(client_one);
            client_one.autoSend("ky");
            

            AutoRead(client_one);
            client_one.ServerResponse();
            Console.WriteLine("the number of the clients is :{0}", client_one.clients.Count);
            
            //Console.WriteLine("set the timer1:");
            
           // int interval1 = int.Parse(Console.ReadLine());
           // timer1.Interval = interval1;
           
        }
    }
}

