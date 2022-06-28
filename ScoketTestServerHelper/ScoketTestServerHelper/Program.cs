using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;

namespace ScoketTestServerHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            onLineClients = new List<string>();
            ServerHelper serverHelper = new ServerHelper(AddOnline, RecMsg);
            serverHelper.Connection("127.0.0.1", "19999");
            while (true) { 
            }
        }

        private static void RecMsg(Socket socket, byte[] buffers, object lockObj)
        {
            try
            {
                lock (lockObj)
                {
                    string fileimage = Encoding.UTF8.GetString(buffers, 0, buffers.Length).Replace("\0", "").Replace("\n", "").Trim();
                    //Console.WriteLine(fileimage);
                    if (File.Exists(fileimage))
                    {
                        int[] result_array = new int[100];
                        result_array[0] = 1;

                        var str = string.Join(",", result_array);

                        var values = System.Text.Encoding.UTF8.GetBytes(str);
                        socket.Send(values);
                        // Thread.Sleep(2000);
                        //Console.WriteLine(str);
                    }
                    else
                    {
                        Console.WriteLine("图片未找到");
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

        }

        private static void AddOnline(string url, bool bl)
        {
            if (bl)
                onLineClients.Add(url);

            else
                onLineClients.Remove(url);
        }
        static List<string> onLineClients;
    }

    public class ServerHelper
    {
        public ServerHelper(Action<string, bool> action, Action<string> appendAction)
        {
            myAddOnline = action;
            AppendAction = appendAction;
        }
        public ServerHelper(Action<string, bool> action, Action<Socket, byte[], object> receiveSocket)
        {
            myAddOnline = action;
            this.ReceiveSocket = receiveSocket;
        }
        //创建套接字
        Socket sock = null;

        //创建负责客户端连接的线程
        Thread threadListen = null;

        //创建URL与Socket的字典集合
        Dictionary<string, Socket> DicSocket = new Dictionary<string, Socket>();


        Action<string, bool> myAddOnline;

        Action<string> AppendAction;

        Action<Socket, byte[], object> ReceiveSocket;

        public bool Connection(string ip, string port)
        {
            //创建负责监听的套接字，注意其中参数：IPV4,字节流，TCP
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress address = IPAddress.Parse(ip);

            //根据IPAddress以及端口号创建IPE对象
            IPEndPoint endpoint = new IPEndPoint(address, int.Parse(port));
            try
            {

                sock.Bind(endpoint);
                AppendAction?.Invoke("服务器开启成功!");
                Console.WriteLine("开启服务成功!", "打开服务");
            }
            catch (Exception ex)
            {
                Console.WriteLine("开启服务失败" + ex.Message, "打开服务");
                return false;
            }
            sock.Listen(10);
            threadListen = new Thread(ListenConnecting);
            threadListen.IsBackground = true;// 后台线程
            threadListen.Start();
            // this.btn_StartServer.Enabled = false;
            return true;
        }

        static object lockObj = new object();
        private void ListenConnecting()
        {
            while (true)
            {
                //一旦监听到一个客户端的连接，将会创建一个与该客户端连接的套接字
                Socket sockClient = sock.Accept();

                string client = sockClient.RemoteEndPoint.ToString();
                DicSocket.Add(client, sockClient);
                myAddOnline.Invoke(client, true);

                //开启接收线程
                Thread thr = new Thread(ReceiveMsg);
                thr.IsBackground = true;
                thr.Start(sockClient);
            }
        }

        private void ReceiveMsg(object sockClient)
        {
            Socket sckclient = sockClient as Socket;
            while (true)
            {
                lock (lockObj)
                {
                    //定义一个2M的缓冲区
                    byte[] arrMsgRec = new byte[1024 * 1024 * 2];
                    int length = -1;
                    try
                    {
                        length = sckclient.Receive(arrMsgRec);

                    }
                    catch (Exception)
                    {
                        string str = sckclient.RemoteEndPoint.ToString();

                        //从列表中移除URL
                        myAddOnline.Invoke(str, false);
                        DicSocket.Remove(str);
                        break;
                    }


                    if (length == 0)//客户端断开连接
                    {
                        string str = sckclient.RemoteEndPoint.ToString();

                        //从列表中移除URL
                        myAddOnline.Invoke(str, false);
                        DicSocket.Remove(str);
                        break;

                    }
                    else//接收到客户端发送的信息
                    {
                        string strMsg = Encoding.UTF8.GetString(arrMsgRec, 0, length);


                        ReceiveSocket.Invoke(sckclient, arrMsgRec, lockObj);


                        //var buffers = ConvertHelper.hexStringToByteArray(strMsg);
                        //var str = ConvertHelper.byteArrayToBinString(buffers);

                        //string Msg = "[接收] " + ":" + str;
                        string Msg = "[接收] " + ":" + strMsg;
                        AppendAction?.Invoke(Msg);

                    }
                }
            }
        }

        public void Send(string msg, List<string> clientList)
        {
            byte[] arrMsg = Encoding.UTF8.GetBytes(msg);
            foreach (var item in clientList)
            {
                DicSocket[item].Send(arrMsg);
                string Msg = "[发送] " + ":" + msg;
                AppendAction.Invoke(Msg);
            }
        }
    }

}
