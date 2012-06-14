using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using server;

namespace server
{
    delegate void msg_Handle(string msg);

    public partial class Form1 : Form
    {
        //clients数组保存当前在线用户的Client对象
        internal static Hashtable clients = new Hashtable();

        //该服务器默认的监听的端口号
        private TcpListener listener;

        //服务器可以支持的最多的客户端的连接数
        static int MAX_NUM = 100;

        //开始服务的标志
        internal static bool SocketServiceFlag = false;

        private string[] Username=new string[100];
        //private int Count=0;

        public Form1()
        {
            InitializeComponent();
        }

        //当单击“开始”按钮时，便开始监听指定的Socket端口
        private void btnSocketStart_Click(object sender, EventArgs e)
        {
            SocketStart();
        }

        //private int getValidPort(string port)
        //{
        //    int lport;

        //    //测试端口号是否有效
        //    try
        //    {
        //        //是否为空
        //        if (port == "")
        //        {
        //            throw new ArgumentException(
        //                "端口号为空，不能启动服务器");
        //        }
        //        lport = System.Convert.ToInt32(port);
        //    }
        //    catch (Exception e)
        //    {
        //        //ArgumentException, 
        //        //FormatException, 
        //        //OverflowException
        //        Console.WriteLine("无效的端口号：" + e.ToString());
        //        this.rtbSocketMsg.AppendText("无效的端口号：" + e.ToString() + "\n");
        //        return -1;
        //    }
        //    return lport;
        //}

        //private string getIPAddress()
        //{
        //    // 获得本机局域网IP地址
        //    string ipAddress = "";
        //    IPAddress[] AddressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
        //    if (AddressList.Length < 1)
        //    {
        //        return "";
        //    }
        //    foreach (IPAddress ip in AddressList)
        //    {
        //        if (ip.AddressFamily.ToString() == "InterNetwork")
        //            ipAddress = ip.ToString();
        //        else ipAddress = "";
        //    }
        //    return ipAddress;
        //}

        private void SocketStart()
        {
            int port = 3333;
            string ip = "127.0.0.1";
            try
            {
                IPAddress ipAdd = IPAddress.Parse(ip);
                //创建服务器套接字
                listener = new TcpListener(ipAdd, port);
                //开始监听服务器端口
                listener.Start();
                this.rtbSocketMsg.AppendText("Socket服务器已经启动，正在监听" +
                    ip + " 端口号：" + port + "\n");
                //启动一个新的线程，执行方法this.StartSocketListen，
                //以便在一个独立的线程中执行确认与客户端Socket连接的操作
                Form1.SocketServiceFlag = true;
                Thread thread = new Thread(new ThreadStart(this.StartSocketListen));
                thread.Start();
                this.btnSocketStart.Enabled = false;
                this.btnSocketStop.Enabled = true;
            }
            catch (Exception ex)
            {
                this.rtbSocketMsg.AppendText(ex.Message.ToString() + "\n");
            }
        }

        //在新的线程中的操作，它主要用于当接收到一个客户端请求时，确认与客户端的连接，
        //并且立刻启动一个新的线程来处理和该客户端的信息交互。
        private void StartSocketListen()
        {
            while (Form1.SocketServiceFlag)
            {
                try
                {
                    //当接收到一个客户端请求时，确认与客户端的连接
                    if (listener.Pending())//检查是否连接请求，如果有则为True
                    {
                        Socket socket = listener.AcceptSocket();
                        if (clients.Count >= MAX_NUM)
                        {
                            //msg_Handle msg_Handle1 = new msg_Handle(this.rtbSocketMsg.AppendText);
                            //this.rtbSocketMsg.Invoke(msg_Handle1, new object[]{"已经达到了最大连接数：" + 
                            //    MAX_NUM + "，拒绝新的连接\n"});
                            ////this.rtbSocketMsg.AppendText("已经达到了最大连接数：" + 
                            ////	MAX_NUM + "，拒绝新的连接\n");
                            socket.Close();
                        }
                        else
                        {
                            //启动一个新的线程，
                            //执行方法this.ServiceClient，处理用户相应的请求
                            Client client = new Client(this, socket);
                            Thread clientService = new Thread(
                                new ThreadStart(client.ServiceClient));
                            clientService.Start();
                        }
                    }
                    Thread.Sleep(200);//主线程休息200ms
                }
                catch (Exception ex)
                {
                    msg_Handle msg_Handle2 = new msg_Handle(this.rtbSocketMsg.AppendText);
                    this.rtbSocketMsg.Invoke(msg_Handle2, new object[] { ex.Message.ToString() + "\n" });
                }
            }
        }

        private void btnSocketStop_Click(object sender, System.EventArgs e)
        {
            SocketStop();   
        }

        private void SocketStop()
        {
            this.rtbSocketMsg.AppendText("Socket服务器已经关闭");
            Form1.SocketServiceFlag = false;
            this.btnSocketStart.Enabled = true;
            this.btnSocketStop.Enabled = false;
        }

        public void addUser(string username)
        {
            msg_Handle msg = new msg_Handle(this.rtbSocketMsg.AppendText);


            if (this.rtbSocketMsg.InvokeRequired)
                this.rtbSocketMsg.Invoke(msg, new object[] { username + " 已经加入\n" });
            else
                this.rtbSocketMsg.AppendText(username + " 已经加入\n");//追加文本
            //将刚连接的用户名加入到当前在线用户列表中
            Username[clients.Count]=username;
            //Count++;

        }

        public void removeUser(string username)
        {
            if (this.rtbSocketMsg.InvokeRequired)
                this.rtbSocketMsg.Invoke(new msg_Handle(this.rtbSocketMsg.AppendText), new object[] { username + " 已经离开\n" });
            else
                this.rtbSocketMsg.AppendText(username + " 已经离开\n");
            //将刚连接的用户名加入到当前在线用户列表中
            //Count--;
        }

        public string GetUserList()
        {
            string Rtn = "";
            for (int i = 0; i <clients.Count; i++)
            {
                Rtn +=Username[i+1].ToString()+ "|";
            }
            return Rtn;
        }

        public void updateUI(string msg)
        {
            if (this.rtbSocketMsg.InvokeRequired)
                this.rtbSocketMsg.Invoke(new msg_Handle(this.rtbSocketMsg.AppendText), new object[] { msg + "\n" });
            else
                this.rtbSocketMsg.AppendText(msg + "\n");
        }

        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Form1.SocketServiceFlag = false;
        }

        
    }

    public class Client
    {
        private string name;
        private Socket currentSocket = null;
        private string ipAddress;
        private Form1 server;
        
        //缓存供外部数据读取
        private int getdata;
        private byte[] buffer = new byte[6144];
        private int offset;


        //保留当前连接的状态：
        //closed --> connected --> closed
        private string state = "closed";

        public Client(Form1 server, Socket clientSocket)
        {
            this.server = server;
            this.currentSocket = clientSocket;
            ipAddress = getRemoteIPAddress();
        }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        public Socket CurrentSocket//属性
        {
            get
            {
                return currentSocket;
            }
            set
            {
                currentSocket = value;
            }
        }

        public string IpAddress
        {
            get
            {
                return ipAddress;
            }
        }

        private string getRemoteIPAddress()
        {
            return ((IPEndPoint)currentSocket.RemoteEndPoint).
                    Address.ToString();
        }

        //ServiceClient方法用于和客户端进行数据通信，包括接收客户端的请求，
        //根据不同的请求命令，执行相应的操作，并将处理结果返回到客户端
        public void ServiceClient()
        {
            string[] tokens = null;
            byte[] buff = new byte[1024];//可以改动
            bool keepConnect = true;

            msg_Handle msg_Handle = new msg_Handle(this.server.updateUI);

            //用循环来不断地与客户端进行交互，直到客户端发出“EXIT”命令，
            //将keepConnect置为false，退出循环，关闭连接，并中止当前线程
            while (keepConnect && Form1.SocketServiceFlag)
            {
                tokens = null;
                try
                {
                    if (currentSocket == null ||
                        currentSocket.Available < 1)
                    {
                        Thread.Sleep(300);
                        continue;
                    }

                    //接收数据并存入buff数组中
                    int len = currentSocket.Receive(buff);
                    //buff.CopyTo(buffer, 0);
                    //将字符数组转化为字符串
                    string clientCommand = System.Text.Encoding.Unicode.GetString(
                                                         buff, 0, len);

                    //server.updateUI(clientCommand);
                    this.server.rtbSocketMsg.Invoke(msg_Handle, new object[] { clientCommand });
                    // server.rtbSocketMsg.Invoke(new msg_Handle(this.server.updateUI), new object[] { clientCommand });
                    // msg_Handle msg_Handle=ne
                    //tokens[0]中保存了命令标志符（CONN、CHAT、PRIV、LIST或EXIT）
                    tokens = clientCommand.Split(new Char[] { '|' });

                    if (tokens == null)
                    {
                        Thread.Sleep(200);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    //server.updateUI("发生异常："+ e.ToString());
                    this.server.rtbSocketMsg.Invoke(msg_Handle, new object[] { "发生异常：" + e.ToString() });
                }


                if (tokens[0] == "CONN")
                {
                    //此时接收到的命令格式为：
                    //命令标志符（CONN）|发送者的用户名|，
                    //tokens[1]中保存了发送者的用户名
                    this.name = tokens[1];
                    //更新界面
                    if (Form1.clients.Contains(this.name))
                    {
                        SendToClient(this, "ERR|User " + this.name + " 已经存在");

                    }
                    else
                    {
                        Hashtable syncClients = Hashtable.Synchronized(
                            Form1.clients);
                        syncClients.Add(this.name, this);

                        server.addUser(this.name);
                        //server.rtbSocketMsg.Invoke(new msg_Handle(this.server.updateUI), new object[] { "发生异常：" + e.ToString() });


                        //对每一个当前在线的用户发送JOIN消息命令和LIST消息命令，
                        //以此来更新客户端的当前在线用户列表
                        System.Collections.IEnumerator myEnumerator =
                               Form1.clients.Values.GetEnumerator();
                        while (myEnumerator.MoveNext())
                        {
                            Client client = (Client)myEnumerator.Current;
                            //string msg = CreateFrame("JOIN", tokens[1]).ToString();
                            //SendToClient(client,"JOIN", msg);

                            SendToClient(client, "JOIN|" + tokens[1] + "|");
                            Thread.Sleep(100);
                        }

                        //更新状态
                        state = "connected";
                        //SendToClient(this, "ok");	

                        //向客户端发送LIST命令，以此更新客户端的当前在线用户列表
                        //string msgUsers= CreateFrame("LIST", server.GetUserList()).ToString();
                        //SendToClient(this, "LIST",msgUsers);
                        string msgUsers = "LIST|" + server.GetUserList();
                        SendToClient(this, msgUsers);
                        /********************************************************/

                    }

                }
                else if (tokens[0] == "LIST")
                {
                    if (state == "connnected")
                    {
                        //向客户端发送LIST命令，以此更新客户端的当前在线用户列表
                        //string msgUsers = CreateFrame("LIST", server.GetUserList()).ToString();
                        //SendToClient(this, "LIST",msgUsers);
                        string msgUsers = "LIST|" + server.GetUserList();
                        SendToClient(this, msgUsers);
                    }
                    else
                    {
                        //send err to server
                        //string msg = CreateFrame("ERR", "state error，Please login first").ToString();
                        //SendToClient(this,"ERR", msg);
                        SendToClient(this, "ERR|state error，Please login first");

                    }
                }

                else if (tokens[0] == "PRIV")
                {
                    if (state == "connected")
                    {
                        //此时接收到的命令格式为：
                        //命令标志符（PRIV）|发送者用户名|接收者用户名|发送内容|
                        //tokens[1]中保存了发送者的用户名
                        string sender = tokens[1];
                        //tokens[2]中保存了接收者的用户名
                        string receiver = tokens[2];
                        //tokens[3]中保存了发送的内容
                        string content = tokens[3];
                        string msg = System.Text.Encoding.Unicode.GetString(CreateFrame("PRIV", tokens[1], tokens[2], tokens[3]));



                        //仅将信息转发给发送者和接收者
                        if (Form1.clients.Contains(sender))
                        {
                            //SendToClient(
                            //        (Client)ClientSeverForm.clients[sender], msg);
                            SendToClient(
                                (Client)Form1.clients[sender], "单发成功！|");
                        }

                        if (Form1.clients.Contains(receiver))
                        {
                            SendToClient(
                                (Client)Form1.clients[receiver], msg);
                        }

                        instoredatatobuffer(buff);
                    }
                    else
                    {

                        SendToClient(this, "ERR|state error，Please login first");

                    }
                }
                else if (tokens[0] == "EXIT")
                {
                    //此时接收到的命令的格式为：命令标志符（EXIT）|发送者的用户名
                    //向所有当前在线的用户发送该用户已离开的信息
                    if (Form1.clients.Contains(tokens[1]))
                    {
                        Client client = (Client)Form1.clients[tokens[1]];

                        //将该用户对应的Client对象从clients中删除
                        Hashtable syncClients = Hashtable.Synchronized(
                            Form1.clients);
                        syncClients.Remove(client.name);
                        server.removeUser(client.name);

                        //向客户端发送QUIT命令

                        //string msg = CreateFrame("QUIT", tokens[1]).ToString();
                        string message = "QUIT|" + tokens[1];

                        System.Collections.IEnumerator myEnumerator =
                            Form1.clients.Values.GetEnumerator();
                        while (myEnumerator.MoveNext())
                        {
                            Client c = (Client)myEnumerator.Current;
                            //if(c.name!=this.name)
                            //SendToClient(c,"QUIT", msg);
                            SendToClient(c, message);
                            // else
                            // SendToClient(c, "成功退出！");
                        }

                        
                    }

                    //退出当前线程
                    break;
                }
                Thread.Sleep(200);
            }
        }
        
        //把数据存入缓存 供外部调用
        public void instoredatatobuffer(byte[] buf)
        {
            if (getdata <= 6)
            {
                buf.CopyTo(buffer, offset);
                offset = offset + 1024;
                getdata++;
                
            }
            else
            {
                getdata = 0;
                offset = 0;
            }
        }

        //组帧
        private byte[] CreateFrame(string command, string sender)
        {
            string frame = command + "|" + sender + "|";
            byte[] outbytes1 = new byte[1024];
            byte[] outbytes2 = System.Text.Encoding.Unicode.GetBytes(frame);
            outbytes2.CopyTo(outbytes1, 0);
            return outbytes1;
        }

        private byte[] CreateFrame(string command, string sender, string receiver, string msg)
        {
            string frame = command + "|" + sender + "|" + receiver + "|" + msg + "|";
            byte[] outbytes1 = new byte[1024];
            byte[] outbytes2 = System.Text.Encoding.Unicode.GetBytes(frame);
            outbytes2.CopyTo(outbytes1, 0);
            return outbytes1;
        }

        //SendToClient()方法实现了向客户端发送命令请求的功能
        private void SendToClient(Client client, string command, string msg)
        {

            //string msg = System.Text.Encoding.Unicode.GetString(CreateFrame(command, msg));
            byte[] message = CreateFrame(command, msg);
            
            client.CurrentSocket.Send(message, message.Length, 0);
        }

        private void SendToClient(Client client, string msg)
        {
            System.Byte[] message = System.Text.Encoding.Unicode.GetBytes(
                    msg.ToCharArray());
            client.CurrentSocket.Send(message, message.Length, 0);
        }
            }
        }
 