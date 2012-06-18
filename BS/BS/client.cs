using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BS
{
    public partial class client : Form
    {
        private int Count;
        

        public System.Timers.Timer timer = new System.Timers.Timer(200);

        private delegate void add_Handler(string msg);
        private delegate int listUsersAdd_Handler(object obj);
        private delegate void clear_Handler();

        //clients数组保存当前在线用户
        internal static Hashtable clients = new Hashtable();

        //与服务器的连接
        TcpClient tcpClient;

        //与服务器数据交互的流通道
        private NetworkStream Stream;
        
        //加buffer供外部数据使用
        private byte[] buffer = new byte[6000];
        private int getdata;
        private int index;


        //客户端的状态
        private static string CLOSED = "closed";
        private static string CONNECTED = "connected";
        private string state = CLOSED;

        private bool stopFlag;

        public client()
        {
            InitializeComponent();

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            ClientInitial();
        }

        private void ClientInitial()
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
                    Int32.Parse("3333"));
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
                // byte[] outbytes = CreateFrame("CONN");
                //Stream.Write(outbytes,0,outbytes.Length);

                string cmd = "CONN|psyche|";
                this.rtbMsg.AppendText("正在连接服务器……\n");/********************************************************/
                //将字符串转化为字符数组
                Byte[] outbytes = System.Text.Encoding.Unicode.GetBytes(
                    cmd.ToCharArray());
                Stream.Write(outbytes, 0, outbytes.Length);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void ServerResponse()
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
                        this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { "命令执行成功" });
                        //add("命令执行成功");
                    }
                    else if (tokens[0].ToUpper() == "ERR")
                    {
                        //命令执行错误
                        this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { "命令执行错误：" + tokens[1] });
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
                        this.lstUsers.Invoke(new clear_Handler(this.lstUsers.Items.Clear), new object[] { });
                        for (int i = 1; i < tokens.Length - 1; i++)
                        {
                            this.lstUsers.Invoke(new listUsersAdd_Handler(this.lstUsers.Items.Add), new object[] { tokens[i].Trim() });
                            //lstUsers.Items.Add(tokens[i].Trim());
                        }
                    }
                    else if (tokens[0] == "JOIN")
                    {
                        //此时从服务器返回的消息格式：
                        //命令标志符（JOIN）|刚刚登入的用户名|
                        //add(tokens[1]+" "+"已经进入了聊天室");
                        this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { tokens[1] + " " + "已经进入了通讯程序" });
                        //this.lstUsers.Items.Add(tokens[1]);
                        this.lstUsers.Invoke(new listUsersAdd_Handler(this.lstUsers.Items.Add), new object[] { tokens[1] });
                        this.state = CONNECTED;
                        
                    }
                    else if (tokens[0] == "QUIT")
                    {
                        //if (this.lstUsers.Items.IndexOf(tokens[1])>-1)
                        listUsersAdd_Handler listUsersAdd_Handler = new listUsersAdd_Handler(this.lstUsers.Items.IndexOf);
                        int index = Int32.Parse(this.lstUsers.Invoke(listUsersAdd_Handler, new object[] { tokens[1] }).ToString());
                        if (index > -1)
                        {
                            //this.lstUsers.Items.Remove(tokens[1]);
                            this.lstUsers.Invoke(new add_Handler(this.lstUsers.Items.Remove),
                                new object[] { tokens[1] });

                        }
                        //add("用户：" + tokens[1] + " 已经离开");
                        this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { "用户：" + tokens[1] + " 已经离开" });

                    }


                    else if (tokens[0] == "PRIV")
                    {
                        int flag = string.Compare(tokens[3], "a");
                        if (flag == 0)
                        {
                            Count = (Count + 1) % 300;
                            this.tbCount.Invoke(new add_Handler(this.add1), new object[] { Convert.ToString(Count) });

                            //this.tbCount.AppendText(Count.ToString());
                        }
                        this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { msg });
                        //this.tbCount.AppendText("0");
                        //tbCount.Text=Count.ToString();

                        //    //if (Count == 299)
                        //    //{
                        //        // Console.WriteLine(1-Count/300);
                        //        //this.tbCount.AppendText(Count.ToString());
                        //    //}
                    }
                    else
                    {
                        //tbCount.Text = "9";
                        //如果从服务器返回的其他消息格式，
                        //则在ListBox控件中直接显示
                        //add(msg);
                        //  int flag = string.Compare(tokens[3], "a");
                        //        string a = tokens[3];
                        //  if (flag == 0)
                        {
                            //Count = (Count + 1) % 300;
                            //this.tbCount.AppendText(Count.ToString());
                        }
                        //this.tbCount.AppendText("0");

                        //this.tbCount.Invoke(new add_Handler(this.add), new object[] { Convert.ToString(Count) });
                        this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { msg });
                    }


                }
                //关闭连接

                tcpClient.Close();
            }
            catch (Exception ex)
            {
                //add("网络发生错误");
                this.rtbMsg.Invoke(new add_Handler(this.add), new object[] { "网络发生错误" + ex.Message });
            }
        }

        private void add(string msg)
        {
            this.rtbMsg.SelectedText = msg + "\n";
            // this.rtbMsg.AppendText(msg + "\n");
        }

        private void add1(string msg)
        {

            this.tbCount.Text = msg;
        }

        //组帧
        private byte[] CreateFrame(string command)
        {
            string frame = command + "|psyche|";
            byte[] outbytes1 = new byte[1024];
            byte[] outbytes2 = System.Text.Encoding.Unicode.GetBytes(frame);
            Buffer.BlockCopy(outbytes2, 0, outbytes1, 0, outbytes2.GetLength(0));
            //outbytes2.CopyTo(outbytes1, 0);
            return outbytes1;
        }

        private byte[] CreateFrame(string command, string receiver, string msg)
        {
            string frame = command + "|psyche|" + receiver + "|" + msg + "|";
            byte[] outbytes1 = new byte[1024];
            byte[] outbytes2 = System.Text.Encoding.Unicode.GetBytes(frame);
            Buffer.BlockCopy(outbytes2, 0, outbytes1, 0, outbytes2.GetLength(0));
            //outbytes2.CopyTo(outbytes1, 0);
            return outbytes1;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            ClientExit();
        }

        private void ClientExit()
        {
            if (state == CONNECTED)
            {

                this.rtbMsg.AppendText("此客户端离开\n");
                //将字符串转化为字符数组


                //byte[] outbytes = CreateFrame("EXIT");
                //Stream.Write(outbytes, 0, outbytes.Length);

                this.state = CLOSED;
                this.stopFlag = true;
                this.lstUsers.Items.Clear();

                string message = "EXIT|psyche|";

                //将字符串转化为字符数组
                Byte[] outbytes = System.Text.Encoding.Unicode.GetBytes(
                    message.ToCharArray());
                Stream.Write(outbytes, 0, outbytes.Length);
            }
            //if(state==CLOSED)
            //{
            //    this.state = CLOSED;
            //    this.stopFlag = true;
            //    string message = "EXIT|psyche|";

            //    //将字符串转化为字符数组
            //    Byte[] outbytes = System.Text.Encoding.Unicode.GetBytes(
            //        message.ToCharArray());
            //    Stream.Write(outbytes, 0, outbytes.Length);
            //}
        }

        private void btnAutoSend_Click(object sender, EventArgs e)
        {
            timer.Enabled = true;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(send);
            timer.AutoReset = true; 
        }

        public void send(object source, System.Timers.ElapsedEventArgs e)
        {
            sendMessage();
            
            
        }

        public void sendMessage()
        {
            byte[] data=new byte[1000];
            try
            {
                int clientSelected = lstUsers.SelectedItems.Count;

                if (clientSelected != 1)
                {

                    MessageBox.Show("请在列表中选择一个用户", "提示信息",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                else
                {
                    //此时命令的格式是：
                    //命令标志符（CHAT）|发送者的用户名：发送内容|
                    string receiver = lstUsers.SelectedItem.ToString();
                    string mesg=datatosend(data);
                    this.rtbMsg.AppendText("psyche-->" + receiver + ": "+mesg+ "\n");
                    // tbSendContent.Text = "";
                    // tbSendContent.Focus();
                    //将字符串转化为字符数组
                    Byte[] outbytes = CreateFrame("PRIV", receiver, "a");
                    Stream.Write(outbytes, 0, outbytes.Length);

                }
            }

            catch
            {
                this.rtbMsg.AppendText("网络发生错误");
            }
        }

        //
        public string datatosend(byte[] buf)
        {
            if (getdata <= 6)
            {
                Array.Copy(buf, 0, buffer, index, buf.Length);
                index = index + 1000;
                getdata++;
            }
            else
            {
                getdata = 0;
                index = 0;
            }
            return buf.ToString();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timer.Enabled = false;
        }

        
    }
}
