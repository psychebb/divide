using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;



namespace server
{
    public class Server
    {
        //clients数组保存当前在线用户的Client对象
        internal static Hashtable clients = new Hashtable();

        //该服务器默认的监听的端口号
        private TcpListener listener;

        //服务器可以支持的最多的客户端的连接数
        static int MAX_NUM = 100;

        //开始服务的标志
        internal static bool SocketServiceFlag = false;

        private string getIPAddress()
        {
            // 获得本机局域网IP地址
            string ipAddress = "";
            IPAddress[] AddressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            if (AddressList.Length < 1)
            {
                return "";
            }
            foreach (IPAddress ip in AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                    ipAddress = ip.ToString();
                else ipAddress = "";
            }
            return ipAddress;
        }

        private int getValidPort(string port)
        {
            int lport;

            //测试端口号是否有效
            try
            {
                //是否为空
                if (port == "")
                {
                    throw new ArgumentException(
                        "端口号为空，不能启动服务器");
                }
                lport = System.Convert.ToInt32(port);
            }
            catch (Exception e)
            {
                //ArgumentException, 
                //FormatException, 
                //OverflowException
                Console.WriteLine("无效的端口号：" + e.ToString());
                return -1;
            }
            return lport;
        }

        private void SocketStart()
        {
            int port = getValidPort(port);
            if (port < 0)
            {
                return;
            }

            string ip = "127.0.0.1";
            try
            {
                IPAddress ipAdd = IPAddress.Parse(ip);
                //创建服务器套接字
                listener = new TcpListener(ipAdd, port);
                //开始监听服务器端口
                listener.Start();
                Console.WriteLine("Socket服务器已经启动，正在监听" +
                    ip + " 端口号：" + port + "\n"); 

                //启动一个新的线程，执行方法this.StartSocketListen，
                //以便在一个独立的线程中执行确认与客户端Socket连接的操作
                Server.SocketServiceFlag = true;
                Thread thread = new Thread(new ThreadStart(this.StartSocketListen));
                thread.Start();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\n");
            }
        }

        //在新的线程中的操作，它主要用于当接收到一个客户端请求时，确认与客户端的连接，
        //并且立刻启动一个新的线程来处理和该客户端的信息交互。
        private void StartSocketListen()
        {
            while (Server.SocketServiceFlag)
            {
                try
                {
                    //当接收到一个客户端请求时，确认与客户端的连接
                    if (listener.Pending())//检查是否连接请求，如果有则为True
                    {
                        Socket socket = listener.AcceptSocket();
                        if (clients.Count >= MAX_NUM)
                        {
                            Console.WriteLine("已经达到了最大连接数：" +
                                MAX_NUM + "，拒绝新的连接\n");
                         
                            //this.rtbSocketMsg.AppendText("已经达到了最大连接数：" + 
                            //	MAX_NUM + "，拒绝新的连接\n");
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
                    Console.WriteLine(ex.Message.ToString() + "\n");
                }
            }
        }

        //public void addUser(string username)
        //{
        //    msg_Handle msg = new msg_Handle(this.rtbSocketMsg.AppendText);


        //    if (this.rtbSocketMsg.InvokeRequired)
        //        this.rtbSocketMsg.Invoke(msg, new object[] { username + " 已经加入\n" });
        //    else
        //        this.rtbSocketMsg.AppendText(username + " 已经加入\n");//追加文本
        //    //将刚连接的用户名加入到当前在线用户列表中
        //    if (this.lbSocketClients.InvokeRequired)
        //        this.lbSocketClients.Invoke(new add_Handle(this.lbSocketClients.Items.Add), new object[] { username });
        //    else
        //        this.lbSocketClients.Items.Add(username);
        //    if (this.tbSocketClientsNum.InvokeRequired)
        //        this.tbSocketClientsNum.Invoke(new msg_Handle(this.func), System.Convert.ToString(clients.Count));
        //    else
        //        this.tbSocketClientsNum.Text = System.Convert.ToString(clients.Count);

        //}

        class Program
        {
            static void Main(string[] args)
            {
            }
        }
    }

    public class Client
    {
        private string name;
        private Socket currentSocket = null;
        private string ipAddress;
        private Server server;




        //保留当前连接的状态：
        //closed --> connected --> closed
        private string state = "closed";

        public Client(Server server, Socket clientSocket)
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
        //创建完成

        //ServiceClient方法用于和客户端进行数据通信，包括接收客户端的请求，
        //根据不同的请求命令，执行相应的操作，并将处理结果返回到客户端
        public void ServiceClient()
        {
            string[] tokens = null;
            byte[] buff = new byte[1024];
            bool keepConnect = true;

            /*******************代理***********************/
            //msg_Handle msg_Handle = new msg_Handle(this.server.updateUI);

            /*******************代理***********************/

            //用循环来不断地与客户端进行交互，直到客户端发出“EXIT”命令，
            //将keepConnect置为false，退出循环，关闭连接，并中止当前线程
            while (keepConnect && Server.SocketServiceFlag)
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
                    //将字符数组转化为字符串
                    string clientCommand = System.Text.Encoding.Unicode.GetString(
                                                         buff, 0, len);

                    //server.updateUI(clientCommand);
                    Console.WriteLine(clientCommand);
                    
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
                    Console.WriteLine("发生异常：" + e.ToString());
                }


                if (tokens[0] == "CONN")
                {
                    //此时接收到的命令格式为：
                    //命令标志符（CONN）|发送者的用户名|，
                    //tokens[1]中保存了发送者的用户名
                    this.name = tokens[1];
                    if (Server.clients.Contains(this.name))
                    {
                        //string msg=CreateFrame("CONN", tokens[1]).ToString();
                        //SendToClient(this,"CONN", msg);
                        SendToClient(this, "ERR|User " + this.name + " 已经存在");

                    }
                    else
                    {
                        Hashtable syncClients = Hashtable.Synchronized(
                            Server.clients);
                        syncClients.Add(this.name, this);

                        //更新界面

                        //server.addUser(this.name);
                        //server.rtbSocketMsg.Invoke(new msg_Handle(this.server.updateUI), new object[] { "发生异常：" + e.ToString() });


                        //对每一个当前在线的用户发送JOIN消息命令和LIST消息命令，
                        //以此来更新客户端的当前在线用户列表
                        System.Collections.IEnumerator myEnumerator =
                            Server.clients.Values.GetEnumerator();
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
                        //server.updateUI(msgUsers);
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
                        if (Server.clients.Contains(sender))
                        {
                            //SendToClient(
                            //        (Client)Server.clients[sender], msg);
                            SendToClient(
                                (Client)Server.clients[sender], "单发成功！|");
                        }

                        if (Server.clients.Contains(receiver))
                        {
                            SendToClient(
                                (Client)Server.clients[receiver], msg);
                        }
                        //server.updateUI(sender + "to" + receiver);
                    }
                    else
                    {
                        //send err to server
                        //string msg = CreateFrame("ERR","state error，Please login first" ).ToString();
                        //SendToClient(this,"ERR", msg);
                        SendToClient(this, "ERR|state error，Please login first");

                    }
                }
                else if (tokens[0] == "EXIT")
                {
                    //此时接收到的命令的格式为：命令标志符（EXIT）|发送者的用户名
                    //向所有当前在线的用户发送该用户已离开的信息
                    if (Server.clients.Contains(tokens[1]))
                    {
                        Client client = (Client)Server.clients[tokens[1]];

                        //将该用户对应的Client对象从clients中删除
                        Hashtable syncClients = Hashtable.Synchronized(
                            Server.clients);
                        syncClients.Remove(client.name);
                        server.removeUser(client.name);

                        //向客户端发送QUIT命令

                        //string msg = CreateFrame("QUIT", tokens[1]).ToString();
                        string message = "QUIT|" + tokens[1];

                        System.Collections.IEnumerator myEnumerator =
                            Server.clients.Values.GetEnumerator();
                        while (myEnumerator.MoveNext())
                        {
                            Client c = (Client)myEnumerator.Current;
                            //if(c.name!=this.name)
                            //SendToClient(c,"QUIT", msg);
                            SendToClient(c, message);
                            // else
                            // SendToClient(c, "成功退出！");
                        }

                        //server.updateUI(message);
                    }

                    //退出当前线程
                    break;
                }
                Thread.Sleep(200);
            }
        }

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


        /***********************client类定义结束******************************************/
    }
}
