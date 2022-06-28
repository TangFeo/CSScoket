using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace ScoketTestClientHelper
{
    class Program
    {
        public static int[] result_array;
        static CancellationTokenSource cancellationToken;

        static void Main(string[] args)
        {
            ClientHelper clientHelper;
            clientHelper = new ClientHelper(ReceiveMsg);
            clientHelper.Connection("127.0.0.1", "19999");
            string teststring = @"./TestImage/0.jpg";
            var buffers = System.Text.Encoding.UTF8.GetBytes(teststring);
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffers));
            cancellationToken = new CancellationTokenSource();
            clientHelper.Send(teststring);
            while (true) { 
            }
        }

        private static void ReceiveMsg(string msg)
        {
            try
            {
                //Console.WriteLine(msg);
                List<int> values = new List<int>();
                var str = msg.Replace("\0", "").Replace("\n", "").Trim();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    var datas = str.Split(',');
                    foreach (var item in datas)
                    {
                        if (int.TryParse(item, out int value))
                        {
                            values.Add(value);
                        }
                    }
                }

                result_array = values.ToArray();
                if (result_array.Length > 0)
                    cancellationToken?.Cancel();
                else
                {
                    Console.WriteLine(msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


    }

    public class ClientHelper
    {
        public ClientHelper(Action<string> appendAction)
        {
            AppendAction = appendAction;
        }

        Socket sockClient = null;

        Thread thrClient = null;
        //运行标志位
        private bool IsRunning = true;
        private void ReceiveMsg()
        {
            while (IsRunning)
            {
                //定义一个2M的缓冲区
                byte[] arrMsgRec = new byte[1024 * 1024 * 2];
                int length = -1;
                try
                {
                    length = sockClient.Receive(arrMsgRec);
                }
                catch (SocketException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AppendAction.Invoke("断开连接" + ex.Message + Environment.NewLine);
                    break;
                }


                if (length > 0)//客户端断开连接
                {
                    string strMsg = Encoding.UTF8.GetString(arrMsgRec, 0, length);
                    //var buffers = ConvertHelper.hexStringToByteArray(strMsg);
                    //var str = ConvertHelper.byteArrayToBinString(buffers);
                    //string Msg = "[接收] " + ":" + str + Environment.NewLine;
                    //string Msg = "[接收] " + ":" + strMsg;
                    //Console.WriteLine(Msg);
                    AppendAction.Invoke(strMsg);
                }
                Thread.Sleep(20);
            }
        }
        Action<string> AppendAction;
        public bool Connection(string ip, string port)
        {

            IPAddress address = IPAddress.Parse(ip);

            IPEndPoint ipe = new IPEndPoint(address, int.Parse(port));

            sockClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {

                AppendAction.Invoke("与服务器连接中......" + Environment.NewLine);
                sockClient.Connect(ipe);
            }
            catch (Exception ex)
            {
                //Application.Current.Shutdown();
                Console.WriteLine("连接失败" + ex.Message, "建立连接");
                return false;
            }
            AppendAction.Invoke("与服务器连接成功" + Environment.NewLine);
            thrClient = new Thread(ReceiveMsg);
            thrClient.IsBackground = true;
            thrClient.Start();
            return true;
        }

        public void Send(string strMsg)
        {
            byte[] arrMsg = Encoding.UTF8.GetBytes(strMsg);
            sockClient.Send(arrMsg);
            AppendAction.Invoke(("[发送]  " + ":" + strMsg + Environment.NewLine));
        }

        public void Close()
        {
            IsRunning = false;
            sockClient?.Close();
        }
    }
}
