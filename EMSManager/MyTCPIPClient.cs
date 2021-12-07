using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace TemplateProject
{
    class MyTCPIPClient
    {
        string IP;
        string port;
        int failCount = 0;
        //bool isConnected = false;

        bool isRunning = true;
        bool acceptConsoleInput = false;
        List<string> dataSend = new List<string>();     //queue for data to be sent
        public string dataReceived;

        private System.Net.Sockets.TcpClient tcpClient;
        Action<string> callback;

        public MyTCPIPClient(string IP, string port, Action<string> callback)
        {
            this.IP = IP;
            this.port = port;
            this.callback = callback;
            if (acceptConsoleInput == true)
            {
                var th = new Thread(ReadConsoleInput);
                th.Start();
            }
            Console.WriteLine(">> TCPIP Client Started");
        }

        public void Start()
        {
            isRunning = true;
            Connect();
            while (isRunning)
            {
                try
                {
                    //if (isConnected == true && tcpClient.Connected == true)
                    if (IsConnected == true)
                    {
                        NetworkStream networkStream = tcpClient.GetStream();
                        ReadCycle(networkStream);
                        WriteCycle(networkStream);
                    }
                    else
                    {
                        //var th = new Thread(Connect);
                        //th.Start();
                        throw (new Exception());
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    failCount++;
                    if (failCount >= 3 || tcpClient.Connected == false)
                    {
                        //Reconnect(serverSocket, tcpClient);
                        Reconnect();
                    }
                    //System.Threading.Thread.Sleep(2000);
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            CloseConnection();
        }

        private void ReadCycle(NetworkStream networkStream)
        {
            //read cycle
            if (networkStream.CanRead)
            {
                byte[] myReadBuffer = new byte[1024];
                StringBuilder myCompleteMessage = new StringBuilder();
                int numberOfBytesRead = 0;
                // Incoming message may be larger than the buffer size.
                while (networkStream.DataAvailable)
                {
                    numberOfBytesRead = networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
                    myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
                }
                string bodyText = myCompleteMessage.ToString();
                if (bodyText != "")
                {
                    Console.WriteLine(">> Received: " + bodyText);
                    ProcessResponse(bodyText);
                }
            }
            else
            {
                Console.WriteLine("Sorry.  You cannot read from this NetworkStream.");
            }
        }

        private void WriteCycle(NetworkStream networkStream)
        {
            //write cycle
            if (dataSend.Count() > 0)
            {
                string bodyText = dataSend[0];
                if (bodyText != "")
                {
                    byte[] outStream = Encoding.ASCII.GetBytes(bodyText);
                    string logText = " >> Data Sent: " + bodyText;
                    Console.WriteLine(logText);
                    DebugUtil.Instance.LOG.Debug(logText);
                    networkStream.Write(outStream, 0, outStream.Length);
                    networkStream.Flush();
                }
                dataSend.RemoveAt(0);
            }
        }

        public void ReadConsoleInput()
        {
            while (isRunning)
            {
                string cmd = Console.ReadLine();
                if (cmd == "exit")
                {
                    isRunning = false;
                }
                else if (cmd == "help")
                {
                    Console.WriteLine("1 Trigger normal dispatcher call");
                    Console.WriteLine("3 Trigger normal HP call");
                    Console.WriteLine("3 Trigger normal PABX call");
                    Console.WriteLine("4 Trigger normal train call");
                    Console.WriteLine("5 Trigger emergency dispatcher call");
                    Console.WriteLine("6 Trigger emergency HP call");
                    Console.WriteLine("7 Trigger emergency PABX call");
                    Console.WriteLine("8 Trigger emergency train call");
                    Console.WriteLine("9 send SDS message from dispatcher");
                    Console.WriteLine("10 send SDS message from HP");
                    Console.WriteLine("11 send SDS message from train");
                }
                else
                {
                    dataSend.Add(cmd);
                    /*string[] temp = cmd.Split(' ');
                    if (temp.Count() >= 2)
                    {
                        int n;
                        bool isNumeric = int.TryParse(temp[1], out n);
                        if (temp[0] == "runsim" && isNumeric == true)
                        {
                            dataSend.Add(cmd);
                        }
                    }*/
                }
            }
        }

        public void ProcessResponse(string bodyText)
        {
            callback(bodyText);
            /*bool ret = true;
            dataReceived = bodyText;
            string[] temp = bodyText.Split(' ');
            if (temp.Count() >= 2)
            {
                int n;
                bool isNumeric = int.TryParse(temp[1], out n);
                if (temp[0] == "runsim" && isNumeric == true)
                {
                    string MsgContent = "Test Message";
                    if (temp.Count() >= 3)
                        MsgContent = temp[2];
                    switch (n)
                    {
                        case 1:
                            break;
                        case 2:
                            break;
                        case 3:
                            break;
                        case 4:
                            break;
                        case 5:
                            break;
                        case 6:
                            break;
                        case 7:
                            break;
                        case 8:
                            break;
                        case 9:
                            break;
                        case 10:
                            break;
                        case 11:
                            break;
                    }
                }
            }*/
        }

        private void Connect()
        {
            tcpClient = new System.Net.Sockets.TcpClient();      //If the connection represented by the TcpClient is broken, you can't use that object for further communicating, nor can you connect it again. Create a new TcpClient object.
            while (IsConnected == false)
            {
                try
                {
                    Console.WriteLine("Connected to " + IP + ":" + port);
                    tcpClient.Connect(IP, Convert.ToInt32(port));
                    Console.WriteLine("Connection established");
                    failCount = 0;
                    //isConnected = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Connection failed");
                    System.Threading.Thread.Sleep(4000);
                }
            }
            if (IsConnected)
            {
                //loginTimer.Start();
            }
        }

        private void CloseConnection()
        {
            tcpClient.Close();
            Console.WriteLine(" >> Client socket disconnected");
            DebugUtil.Instance.LOG.Info(" >> Client socket disconnected");
        }

        private void Reconnect()
        {
            //Console.WriteLine(" >> Client socket disconnected");
            //DebugUtil.Instance.LOG.Info(" >> Client socket disconnected");
            //tcpClient.Close();
            CloseConnection();
            //isConnected = false;
            //Console.WriteLine(" >> Close client socket");
            //DebugUtil.Instance.LOG.Info(" >> Close client socket");
            System.Threading.Thread.Sleep(2000);
            Connect();
        }

        public bool IsConnected
        {
            get
            {
                try
                {
                    if (tcpClient != null && tcpClient.Client != null && tcpClient.Client.Connected)
                    {
                        /* pear to the documentation on Poll:
                         * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                         * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                         * -or- true if data is available for reading; 
                         * -or- true if the connection has been closed, reset, or terminated; 
                         * otherwise, returns false
                         */

                        // Detect if client disconnected
                        if (tcpClient.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public void sendData(string data)
        {
            dataSend.Add(data);
        }

        public string GetReceivedData()
        {
            return dataReceived;
        }
    }
}
