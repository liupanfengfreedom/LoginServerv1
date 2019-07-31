#define UTF16
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
namespace LoginServer
{

    // public delegate void OnReceivedCompleted(List<byte>mcontent);
    public delegate void OnReceivedCompleted(byte[] buffer);
    public delegate void OnFileReceivedCompleted(ref String content);
    class TCPClient
    {
        public String map { private set; get; }
        public String vip { private set; get; }
        public String rank { private set; get; }
        public String nvn { private set; get; }

        String TCPClient_username;
        String TCPClient_password;
        String TCPClient_MapName;
        /// <summary>
        /// //////////////////////////////////////////////
        /// </summary>
        public bool isinmatchpool = false;
        public bool mclosed = false;
        Socket clientsocket;
         OnReceivedCompleted OnReceivedCompletePointer = null;
         OnFileReceivedCompleted onfilereceivedcompleted = null;
        const int BUFFER_SIZE = 65536;
        public byte[] receivebuffer = new byte[BUFFER_SIZE];
        string filestringpayload;
        String filestringpayloadsendtoclient;
        bool isfile = false;
        bool isfilegoing = false;
        bool isclientsidefilereceiveok = false;
        String[] receives;
        Int32 filelength;
        Thread ReceiveThread;
        Thread SendFileThread;
        public TCPClient(Socket msocket)
        {
            Console.WriteLine("TCPClient " + msocket.RemoteEndPoint);
            clientsocket = msocket;
            OnReceivedCompletePointer += messagehandler;
            onfilereceivedcompleted += ReceiveFilehandler;
           // onfilereceivedcompleted += (ref String s) => { Send("hi completed!");  };
            ReceiveThread = new Thread(new ThreadStart(ReceiveLoop));
            ReceiveThread.IsBackground = true;
            ReceiveThread.Start();
        }
        ~TCPClient()
        {
            Console.WriteLine("TCPClient In destructor.");
        }
        public void Send(byte[] buffer)
        {
            if (clientsocket != null)
            {
                clientsocket.Send(buffer);
            }
        }
        public void Send(String message)
        {
#if UTF16
            UnicodeEncoding asen = new UnicodeEncoding();
#else
            ASCIIEncoding asen = new ASCIIEncoding();
#endif
            //Console.WriteLine(message);
            if (clientsocket != null)
            {
                clientsocket.Send(asen.GetBytes(message));
            }
        }
        void ReceiveLoop()
        {
            while (true)
            {
                try
                {
                    Array.Clear(receivebuffer, 0, receivebuffer.Length);
                    clientsocket.Receive(receivebuffer);
                    OnReceivedCompletePointer?.Invoke(receivebuffer);
                    Thread.Sleep(30);
                }
                catch (SocketException)
                {
                    mclosed = true;
                    CloseSocket();
                   // room.Remove(this);
                    ReceiveThread.Abort();
                }
            }

        }
        public void CloseSocket()
        {
            clientsocket.Close();
        }
        void ReceiveFilehandler(ref String str)
        {

            MySQLOperation msqlo = MySQLOperation.getinstance();
            string cmd = String.Format(
             "SELECT {0} FROM {1} WHERE UserName='{2}'", TCPClient_MapName, MySQLOperation.tablebasename, TCPClient_username
            );
            bool b = msqlo.find(cmd);
            if (!b)
            {
                msqlo.addcolumnv1(TCPClient_MapName);
            }
            cmd = String.Format(
                  " UPDATE {0} SET {1} = '{2}' WHERE UserName='{3}'", MySQLOperation.tablebasename, TCPClient_MapName, filestringpayload, TCPClient_username
                   );
            msqlo.modify(cmd);
            filestringpayload = null;
        }
        void sendfilework(Object pobject)
        {
            do
            {
                String file_str = filestringpayloadsendtoclient.Length > 32768 ? filestringpayloadsendtoclient.Substring(0,32768) : filestringpayloadsendtoclient;//string should be encode by unicode
                Send(file_str);
                filestringpayloadsendtoclient = filestringpayloadsendtoclient.Length> 32768 ? filestringpayloadsendtoclient.Substring(32768) : filestringpayloadsendtoclient.Substring(filestringpayloadsendtoclient.Length);
                Console.WriteLine("send file frame");
                while (!isfilegoing)
                {
                   Thread.Sleep(10);
                }
                isfilegoing = false;
            } while (!String.IsNullOrEmpty(filestringpayloadsendtoclient));
            FMessagePackage filesend = new FMessagePackage();
            filesend.MT = MessageType.CLIENT_FILEEND;//receive ok           
            string strsend = JsonConvert.SerializeObject(filesend);
            Send(strsend);
            while (!isclientsidefilereceiveok)
            {
                Thread.Sleep(20);
            }
            isclientsidefilereceiveok = false;
            filesend = new FMessagePackage();
            filesend.MT = (MessageType)pobject;//tell client what infor is in this file           
            strsend = JsonConvert.SerializeObject(filesend);
            Send(strsend);
            SendFileThread.Abort();
        }
        void messagehandler(byte[] buffer)
        {
            FMessagePackage mp;
            try
            {
#if UTF16
                var str = System.Text.Encoding.Unicode.GetString(buffer);
#else
            var str = System.Text.Encoding.UTF8.GetString(buffer);
#endif
            var strfile = System.Text.Encoding.UTF8.GetString(buffer);
                String[] seperator = {"\r\n"};
                receives = strfile.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                //"POST / HTTP/1.1"
                //"Accept: */*"
                //"Accept-Encoding: deflate, gzip"
                //"User-Agent: X-UnrealEngine-Agent"
                //"Content-Type: application/json"
                //"Accepts: application/json"
                //"UserName: 002"
                //"Password: 123"
                //"Content-Length: 2"
                //"\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"
                string[] seperator1 = { "\0" };
                if (!isfile)
                {
                    bool b = receives.Length == 12 && receives[0].Equals("POST / HTTP/1.1");
                    if (b)
                    {
                        string length = receives[10].Split(':')[1];
                        if (!Int32.TryParse(length, out filelength))
                        {
                        }
                        TCPClient_username = receives[7].Split(':')[1].Replace(" ", String.Empty);
                        TCPClient_password = receives[8].Split(':')[1].Replace(" ", String.Empty);
                        TCPClient_MapName = receives[9].Split(':')[1].Replace(" ", String.Empty);
                        String[] contents = receives[11].Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
                        if (contents.Length > 0)
                        {
                            filestringpayload = contents[0];
                            if (filestringpayload.Length == filelength)
                            {
                                onfilereceivedcompleted.Invoke(ref filestringpayload);
                            }
                        }
                        isfile = true;
                        return;
                    }
                    //message windows here
                }
                else{
                    String[] contents = strfile.Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
                    if (contents.Length > 0)
                    {
                        filestringpayload += contents[0];
                        if (filestringpayload.Length == filelength)
                        {
                            onfilereceivedcompleted.Invoke(ref filestringpayload);
                        }
                    }
                    return;
                }
      
                mp = JsonConvert.DeserializeObject<FMessagePackage>(str);
                String[] payloads;
                switch (mp.MT)
                {
                    #region MessageType.SINGUP singup    
                    case MessageType.SINGUP:
                        payloads = mp.PayLoad.Split('?');
                        MySQLOperation msqlo = MySQLOperation.getinstance();
                        String cmd;
                        Dictionary<String, String> value;
                        cmd = String.Format(
                          "SELECT UserName FROM {0} WHERE UserName='{1}'", MySQLOperation.tablebasename, payloads[0]
                           );
                        bool b = msqlo.find(cmd);
                        if (!b)
                        {
                            //cmd = "INSERT INTO students(name,class) VALUES(@name,@class)";
                            cmd = String.Format(
                               "INSERT INTO {0}(UserName,PassWord) VALUES(@UserName,@PassWord)", MySQLOperation.tablebasename
                                );
                            value = new Dictionary<string, string>();
                            value.Add("@UserName", payloads[0]);
                            value.Add("@PassWord", payloads[1]);
                            msqlo.add(cmd, value);
                        }
                        FMessagePackage feedback = new FMessagePackage();
                        feedback.MT = MessageType.SINGUP;
                        feedback.PayLoad = b ? "failed" : "succeed";
                        String strsend = JsonConvert.SerializeObject(feedback);
                        Send(strsend);
                        break;
                    #endregion
                    #region  MessageType.LOGIN login    

                    case MessageType.LOGIN:
                         payloads = mp.PayLoad.Split('?');
                        TCPClient_username = payloads[0];
                        TCPClient_password = payloads[1];
                        msqlo = MySQLOperation.getinstance();
                        cmd = String.Format(
                          "SELECT UserName FROM {0} WHERE UserName='{1}'", MySQLOperation.tablebasename, payloads[0]
                           );
                        b = msqlo.find(cmd);

                        List<List<object>> values;
                        if(b)
                        {
                            cmd = String.Format(
                                // "SELECT * FROM {0} WHERE UserName='{1}'", MySQLOperation.tablebasename, payloads[0]
                                 "SELECT PassWord FROM {0} WHERE UserName='{1}'", MySQLOperation.tablebasename, payloads[0]
                                  );
                            values = msqlo.get(cmd);
                            foreach (var vr in values)
                            {
                                if (vr[0].ToString().Equals(payloads[1]))//password match
                                {
                                    cmd = String.Format(
                                        " UPDATE {0} SET IsOnLine = '1' WHERE UserName='{1}'", MySQLOperation.tablebasename, payloads[0]
                                         );
                                    msqlo.modify(cmd);
                                }
                                else {
                                    b = false;
                                }
                                // Console.WriteLine(vr[0] + ":" + vr[1] + ":" + vr[2] + ":" + vr[3]);
                            }
                        }
                        feedback = new FMessagePackage();
                        feedback.MT = MessageType.LOGIN;
                        feedback.PayLoad = !b ? "failed" : "succeed";
                        strsend = JsonConvert.SerializeObject(feedback);
                        Send(strsend);
                        //////////////////////////////////////////////////////////////////////////////////////
                        ///test area

                        break;
                    #endregion
                    #region MessageType.SAVEMAPACTORINFOR save map actor infor    

                    case MessageType.SAVEMAPACTORINFOR:
                        //payloads = mp.PayLoad.Split('?');
                        string mapname = mp.PayLoad;
                        msqlo = MySQLOperation.getinstance();
                        cmd = String.Format(
                         "SELECT {0} FROM {1} WHERE UserName='{2}'", mapname, MySQLOperation.tablebasename,TCPClient_username
                        );
                        b = msqlo.find(cmd);
                        if (!b)
                        {
                            msqlo.addcolumnv1(mapname);
                        }
                        cmd = String.Format(
                              " UPDATE {0} SET {1} = '{2}' WHERE UserName='{3}'", MySQLOperation.tablebasename, mapname, filestringpayload, TCPClient_username
                               );
                        msqlo.modify(cmd);
                        filestringpayload = null;
                        break;
                    #endregion
                    #region MessageType.GETMAPACTORINFOR get map actor infor    
                    case MessageType.GETMAPACTORINFOR:
                        payloads = mp.PayLoad.Split('?');
                        string map = payloads[0];
                        string name = payloads[1];
                        msqlo = MySQLOperation.getinstance();
                        cmd = String.Format(
                            // "SELECT * FROM {0} WHERE UserName='{1}'", MySQLOperation.tablebasename, payloads[0]
                            "SELECT {0} FROM {1} WHERE UserName='{2}'", map, MySQLOperation.tablebasename, name
                             );
                        b = msqlo.find(cmd);
                        if (b)
                        {
                            values = msqlo.get(cmd);
                            foreach (var vr in values)
                            {
                                filestringpayloadsendtoclient = vr[0].ToString();
                                // Console.WriteLine(vr[0] + ":" + vr[1] + ":" + vr[2] + ":" + vr[3]);
                            }
                            //Thread.Sleep(10000);
                            feedback = new FMessagePackage();
                            feedback.MT = MessageType.CLIENT_FILE;
                            strsend = JsonConvert.SerializeObject(feedback);
                            Send(strsend);
                            Console.WriteLine(strsend);
                            Thread.Sleep(200);//here must has enough delay for filestringpayloadsendtoclient maybe a big file so cpu must cost more time to handle it
                            Console.WriteLine("Thread.Sleep(10000)");
                            SendFileThread = new Thread(new ParameterizedThreadStart(sendfilework));
                            SendFileThread.IsBackground = true;
                            SendFileThread.Start(MessageType.MAPACTORINFORSENDOK);
                        }
                        break;
                    #endregion
                    case MessageType.EXITGAME:
                        OnClientExit();
                        break;
                    case MessageType.CLIENT_FILE: //client say keep sending
                         isfilegoing = true;
                        break;
                    case MessageType.CLIENT_FILERECEIVEOK:
                        isclientsidefilereceiveok = true;
                        break;
                    default:
     
                        break;
                }
            }
            catch (Newtonsoft.Json.JsonSerializationException)
            {//buffer all zero//occur when mobile client force kill the game client
                OnClientExit();
            }
        }
        public void OnClientExit()
        {
            string cmd = String.Format(
                  "SELECT UserName FROM {0} WHERE UserName='{1}'", MySQLOperation.tablebasename, TCPClient_username
                   );
            MySQLOperation msqlo = MySQLOperation.getinstance();
           bool b = msqlo.find(cmd);
            if (b)
            {
                cmd = String.Format(
                     // "SELECT * FROM {0} WHERE UserName='{1}'", MySQLOperation.tablebasename, payloads[0]
                     "SELECT PassWord FROM {0} WHERE UserName='{1}'", MySQLOperation.tablebasename, TCPClient_username
                      );
                List<List<object>> values = msqlo.get(cmd);
                foreach (var vr in values)
                {
                    if (vr[0].ToString().Equals(TCPClient_password))//password match
                    {
                        cmd = String.Format(
                            " UPDATE {0} SET IsOnLine = '0' WHERE UserName='{1}'", MySQLOperation.tablebasename, TCPClient_username
                             );
                        msqlo.modify(cmd);
                    }
                }
            }
            mclosed = true;
            CloseSocket();
            ReceiveThread.Abort();
        }

    }
}
