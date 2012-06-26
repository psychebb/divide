using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Collections;

namespace CA_BS
{
    class client
    {
        private int Count;
        private string name;
        //clients数组保存当前在线用户
        public Hashtable clients = new Hashtable();

        //与服务器的连接
        TcpClient tcpClient;

        //与服务器数据交互的流通道
        private NetworkStream Stream;

        //客户端的状态
        private static string CLOSED = "closed";
        private static string CONNECTED = "connected";
        private string state = CLOSED;

        private bool stopFlag;

        //buffer
        public Queue<byte[]> ClientToOutsideBuffer = new Queue<byte[]>();//client端存入接受到的数据，外部程序读取
        bool downwardreaderFlag=false;
        public Queue<byte[]> OutsideToClientBuffer = new Queue<byte[]>();//外部程序存入需要发送的数据，client端读取
        bool upwardreaderFlag = false;


        //timer
        public System.Timers.Timer timer = new System.Timers.Timer(200);

        public client(string name)
        {
            this.name = name;

        }

        public void add_user(string name)
        {
            this.clients.Add(name, true);
        }

        //组帧
        private byte[] CreateFrame(string command)
        {
            string frame = command + "|" + this.name + "|";
            byte[] outbytes1 = new byte[1024];
            byte[] outbytes2 = System.Text.Encoding.Unicode.GetBytes(frame);
            Buffer.BlockCopy(outbytes2, 0, outbytes1, 0, outbytes2.GetLength(0));
            //outbytes2.CopyTo(outbytes1, 0);
            return outbytes1;
        }

        private byte[] CreateFrame(string command, string receiver, string msg)
        {
            string frame = command + "|" + this.name + "|" + receiver + "|" + msg + "|";
            byte[] outbytes1 = new byte[1024];
            byte[] outbytes2 = System.Text.Encoding.Unicode.GetBytes(frame);
            Buffer.BlockCopy(outbytes2, 0, outbytes1, 0, outbytes2.GetLength(0));
            //outbytes2.CopyTo(outbytes1, 0);
            return outbytes1;
        }

        public void ClientInitial()
        {
            if (state == CONNECTED)
            {
                return;
            }
            try
            {
                //创建一个客户端套接字，它是Login的一个公共属性，
                //将被传递给ChatClient窗体
                tcpClient = new TcpClient();
                //向指定的IP地址的服务器发出连接请求
                tcpClient.Connect(IPAddress.Parse("127.0.0.1"),
                    Int32.Parse("1234"));
                //获得与服务器数据交互的流通道（NetworkStream)
                Stream = tcpClient.GetStream();

                //启动一个新的线程，执行方法this.ServerResponse()，
                //以便来响应从服务器发回的信息
                Thread thread = new Thread(new ThreadStart(this.ServerResponse));
                // thread.
                thread.Start();

                //向服务器发送“CONN”请求命令，
                //此命令的格式与服务器端的定义的格式一致，
                //命令格式为：命令标志符（CONN）|发送者的用户名|
                //this.rtbMsg.AppendText("正在连接服务器……\n");/********************************************************/
                //将字符串转化为字符数组
                byte[] outbytes = CreateFrame("CONN");
                Stream.Write(outbytes, 0, outbytes.Length);

                //string cmd = "CONN|" + this.name + "|";
                ////this.rtbMsg.AppendText("正在连接服务器……\n");/********************************************************/
                ////将字符串转化为字符数组
                //Byte[] outbytes = System.Text.Encoding.Unicode.GetBytes(
                //    cmd.ToCharArray());
                //Stream.Write(outbytes, 0, outbytes.Length);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public void ServerResponse()
        {
            //定义一个byte数组，用于接收从服务器端发送来的数据，
            //每次所能接收的数据包的最大长度为1024个字节
            byte[] buff = new byte[1024];
            string msg;
            int len;
            try
            {
                if (!Stream.CanRead)
                {
                    return;
                }

                stopFlag = false;
                while (!stopFlag)
                {
                    //从流中得到数据，并存入到buff字符数组中
                    len = Stream.Read(buff, 0, buff.Length);//返回已读取的字节数,最大读取buff.length长度的字节数，存在buff里


                    if (len < 1)//即len为0，可能是读到流的最后，可能是并未到达最后，但已经无数据或不再需要数据（如关闭socket）
                    {
                        Thread.Sleep(200);
                        continue;
                    }



                    //将字符数组转化为字符串
                    msg = System.Text.Encoding.Unicode.GetString(buff, 0, len);
                    msg.Trim();
                    // this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { msg });/*******************************************/
                    string[] tokens = msg.Split(new Char[] { '|' });//发送的信息中存在|，所以此时用其分隔开，分别存放。返回的字符串数组包含此实例中的子字符串（由指定 Unicode 字符数组的元素分隔）。

                    //tokens[0]中保存了命令标志符（LIST或JOIN或QUIT）

                    if (tokens[0].ToUpper() == "OK")
                    {
                        //处理响应
                        //this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { "命令执行成功" });
                        //add("命令执行成功");
                    }
                    else if (tokens[0].ToUpper() == "ERR")
                    {
                        //命令执行错误
                        //this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { "命令执行错误：" + tokens[1] });
                        //add("命令执行错误：" + tokens[1]);
                    }
                    else if (tokens[0] == "LIST")
                    {
                        //此时从服务器返回的消息格式：
                        //命令标志符（LIST）|用户名1|用户名2|...（所有在线用户名）|
                        //add("获得用户列表");
                        // this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { "获得用户列表" });
                        //更新在线用户列表
                        //lstUsers.Items.Clear();
                        //this.lstUsers.Invoke(new clear_Handler(this.lstUsers.Items.Clear), new object[] { });
                        for (int i = 1; i < tokens.Length - 1; i++)
                        {

                            if (clients.Contains(tokens[i]))
                                clients[tokens[i]] = true;
                            else
                                clients.Add(tokens[i], true);
                            //lstUsers.Items.Add(tokens[i].Trim());
                        }
                    }
                    else if (tokens[0] == "JOIN")
                    {
                        //此时从服务器返回的消息格式：
                        //命令标志符（JOIN）|刚刚登入的用户名|
                        //add(tokens[1]+" "+"已经进入了聊天室");
                        //this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { tokens[1] + " " + "已经进入了通讯程序" });
                        //this.lstUsers.Items.Add(tokens[1]);
                        //this.lstUsers.Invoke(new listUsersAdd_Handler(this.lstUsers.Items.Add), new object[] { tokens[1] });
                        this.state = CONNECTED;
                        if (clients.Contains(tokens[1]))
                            clients[tokens[1]] = true;
                        else
                            clients.Add(tokens[1], true);


                    }
                    else if (tokens[0] == "QUIT")
                    {
                        //if (this.lstUsers.Items.IndexOf(tokens[1])>-1)
                        //listUsersAdd_Handler listUsersAdd_Handler = new listUsersAdd_Handler(this.lstUsers.Items.IndexOf);
                        //int index = Int32.Parse(this.lstUsers.Invoke(listUsersAdd_Handler, new object[] { tokens[1] }).ToString());
                        //if (index > -1)
                        //{
                        //    //this.lstUsers.Items.Remove(tokens[1]);
                        //    this.lstUsers.Invoke(new add_Handler(this.lstUsers.Items.Remove),
                        //        new object[] { tokens[1] });

                        //}
                        //add("用户：" + tokens[1] + " 已经离开");
                        //this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { "用户：" + tokens[1] + " 已经离开" });
                        clients[tokens[1]] = false;

                    }


                    else if (tokens[0] == "PRIV")
                    {
                        int flag = string.Compare(tokens[3], "a");
                        if (flag == 0)
                        {
                            Count = (Count + 1) % 300;
                            //this.tbCount.Invoke(new add_Handler(this.add1), new object[] { Convert.ToString(Count) });
                            this.WriteToBuffer_ClientToOutside(System.Text.Encoding.Unicode.GetBytes(tokens[3]));
                            //this.tbCount.AppendText(Count.ToString());
                        }
                        
                    }
  
                }
                //关闭连接

                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void ClientExit()
        {
            if (state == CONNECTED)
            {
                global::System.Console.WriteLine("此客户端离开\n");
                //将字符串转化为字符数组


                byte[] outbytes = CreateFrame("EXIT");
                Stream.Write(outbytes, 0, outbytes.Length);

                this.state = CLOSED;
                this.stopFlag = true;
                //this.lstUsers.Items.Clear();

                //string message = "EXIT|" + this.name + "|";

                //将字符串转化为字符数组
                //byte[] outbytes = System.Text.Encoding.Unicode.GetBytes(
                //    message.ToCharArray());
                //Stream.Write(outbytes, 0, outbytes.Length);
            }
        }

        public void ReadFromBuffer_ClientToOutside()
        { 
            lock(this)
            {
                if (!downwardreaderFlag)//如果现在不可读取
　　　　			{ 
　　　　　　			try
　　　　　　			{
　　　　　　				//等待WriteToCell方法中调用Monitor.Pulse()方法
　　　　　　				Monitor.Wait(this);
　　　　　　			}
　　　　　　			catch (SynchronizationLockException e)
　　　　　　			{
　　　　　　				Console.WriteLine(e);
　　　　　　			}

　　　　　　			catch (ThreadInterruptedException e)
　　　　　　			{
　　　　　　				Console.WriteLine(e);
　　　　　　			}
　　　　			}

                Console.WriteLine("the downward data is: {0}", System.Text.Encoding.Unicode.GetString(ClientToOutsideBuffer.Dequeue()));
               // Console.WriteLine("the downward data is:{0}",);
　　　　			downwardreaderFlag = false; //重置readerFlag标志，表示消费行为已经完成
　　　　			Monitor.Pulse(this); //通知WriteToCell()方法（该方法在另外一个线程中执行，等待中）
　　　　		}
}
    
        public void WriteToBuffer_ClientToOutside(byte[] data)
        {
            lock(this)
　　　　    {
　　　　		if (downwardreaderFlag)
　　　　		{
　　　　　　		try
　　　　　　		{
　　　　　　			Monitor.Wait(this);
　　　　　　		}
　　　　　　		catch (SynchronizationLockException e)
　　　　　　		{
　　　　　　			//当同步方法（指Monitor类除Enter之外的方法）在非同步的代码区被调用
　　　　　　			Console.WriteLine(e);
　　　　　　		}
　　　　　　		catch (ThreadInterruptedException e)
　　　　　　		{
　　　　　　			//当线程在等待状态的时候中止 
　　　　　　			Console.WriteLine(e);
　　　　　　		}
　　　　		}
                if (ClientToOutsideBuffer.Count<6)
	            {
                    ClientToOutsideBuffer.Enqueue(data);
	            }
　　　　		else
                    Console.WriteLine("the downward data is losing:{0}", System.Text.Encoding.Unicode.GetString(ClientToOutsideBuffer.Dequeue()));
　　　　		downwardreaderFlag = true; 
　　　　		Monitor.Pulse(this); //通知另外一个线程中正在等待的ReadFromCell()方法
　　　　}
　　}

        public string ReadFromBuffer_OutsideToClient()
        {
            lock (this)
            {
                if (!upwardreaderFlag)//如果现在不可读取
                {
                    try
                    {
                        //等待WriteToCell方法中调用Monitor.Pulse()方法
                        Monitor.Wait(this);
                    }
                    catch (SynchronizationLockException e)
                    {
                        Console.WriteLine(e);
                    }

                    catch (ThreadInterruptedException e)
                    {
                        Console.WriteLine(e);
                    }
                }
                upwardreaderFlag = false; //重置readerFlag标志，表示消费行为已经完成
                Monitor.Pulse(this); //通知WriteToCell()方法（该方法在另外一个线程中执行，等待中）
                return System.Text.Encoding.Unicode.GetString(OutsideToClientBuffer.Dequeue());
            }
        }

        public void WriteToBuffer_OutsideToClient(byte[] data)
        {
            lock (this)
            {
                if (upwardreaderFlag)
                {
                    try
                    {
                        Monitor.Wait(this);
                    }
                    catch (SynchronizationLockException e)
                    {
                        //当同步方法（指Monitor类除Enter之外的方法）在非同步的代码区被调用
                        Console.WriteLine(e);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        //当线程在等待状态的时候中止 
                        Console.WriteLine(e);
                    }
                }
                if (OutsideToClientBuffer.Count < 6)
                {
                    OutsideToClientBuffer.Enqueue(data);
                }
                else
                    Console.WriteLine("the OutsideToClientBuffer is full,the first data is losing:{0}", System.Text.Encoding.Unicode.GetString(OutsideToClientBuffer.Dequeue()));
                upwardreaderFlag = true;
                Monitor.Pulse(this); //通知另外一个线程中正在等待的ReadFromCell()方法
            }
        }

        public void SetTimer(int interval)
        {
            timer.Interval = interval;
        }

        public void autoSend(string receiver)
        {
            timer.Elapsed += (s_, e_) => Send(receiver);
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public void sayhello()
        {
            Console.WriteLine("hello");
        }

        public void Send(string receiver)
        {
            try
            {
                string data = ReadFromBuffer_OutsideToClient();
                Byte[] OutBytes = CreateFrame("PRIV", receiver, data);
                Stream.Write(OutBytes, 0, OutBytes.Length);
                this.WriteToBuffer_ClientToOutside(System.Text.Encoding.Unicode.GetBytes(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


       
    }
}

 