//primary
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    enum MessageType
    {
        FILE,//changless
        FILEEND,//changless
        CLIENT_FILE,
        CLIENT_FILEEND,
        CLIENT_FILERECEIVEOK,//client side receive ok
        SINGUP,
        LOGIN,
        MATCH,
        SAVEMAPACTORINFOR,
        GETMAPACTORINFOR,
        MAPACTORINFORSENDOK,
        EntryMAP,
        EntryMAPOK,
        EXITGAME,
        FILERECEIVEOK,//server side receive ok
    }
    struct FMessagePackage
    {
        public MessageType MT;

        public string PayLoad;
        public FMessagePackage(string s)
        {
            MT = MessageType.MATCH;
            PayLoad = "";
        }
    }
    class Program
    {
        static List<TCPClient> singinpool = new List<TCPClient>();
        private static readonly object singinLock = new object();
        static void Main(string[] args)
        {
        String str1 = "histring";
            string str2 = str1.Substring(2);

            IPAddress ipAd = IPAddress.Parse("192.168.1.240");
            TcpListener myList = new TcpListener(ipAd, 8002);

            /* Start Listeneting at the specified port */
            myList.Start();
            while (true)
            {
                Socket st = myList.AcceptSocket();
                TCPClient tcpClient = new TCPClient(st);
                //lock (singinLock)
                //{
                //    singinpool.Add(tcpClient);
                //}
                //int len = singinpool.Count;
                //Console.WriteLine("singinpool " + len.ToString());
            }

        }
    }
}
