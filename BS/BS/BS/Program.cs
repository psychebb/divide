using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.Run(new client());
            client client_one = new client("psyche");
            client_one.ClientInitial();
            client_one.add_user("haha");
            //以下为测试buffer，读取6次后写buffer，在读取发送
            //byte[] buf=new byte[5400];
            //for (int i = 0; i < 5400;i++)
            //{
            //    buf[i]=1;
            //}
            //client_one.set_buffer(buf);
            //for (int i = 0; i < 100; i++)
            //{
            //    client_one.send("haha");
            //    if (client_one.getdata==6)
            //    {
            //        client_one.set_buffer(buf);
            //        client_one.bufferstate = "read";
            //    }
            //}
            client_one.send("haha");
            client_one.ServerResponse();


        }
    }
}
