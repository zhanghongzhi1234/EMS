using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TemplateProject
{
    public class TCPIPServer : Server
    {
        string IP;
        string port;

        public Dictionary<string, List<RawTable>> data = new Dictionary<string, List<RawTable>>();  //Protocol Message
        
        /*DispatcherTimer timerPoll;
        int pollInterval = 1000;            //Polling interval for server
        private System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        bool previouslyConnected = false;*/
        MyTCPIPClient myServer;

        public TCPIPServer(string name, string IP, string port)
        {
            this.name = name;
            this.serverType = ServerType.TCPIP;
            this.IP = IP;
            this.port = port;

            myServer = new MyTCPIPClient(IP, port, ProcessResponse);
        }

        public override void Init()
        {
            //Start the timerPoll
            /*timerPoll = new DispatcherTimer();
            timerPoll.Interval = TimeSpan.FromMilliseconds(pollInterval);
            timerPoll.Tick += new EventHandler(timerPoll_Tick);
            timerPoll.Start();*/
            var th = new Thread(myServer.Start);
            th.Start();
        }

        public void ProcessResponse(string receivedData)
        {
            //Console.WriteLine(">> Received:" + data);
            DebugUtil.Instance.LOG.Debug(receivedData);
            data = JsonConvert.DeserializeObject<Dictionary<string, List<RawTable>>>(receivedData);
        }

        //retrieve from 
        /*private void timerPoll_Tick(object sender, EventArgs e)
        {
            if(myServer.IsConnected == true)
            {
                string returndata = myServer.GetReceivedData();
                data = JsonConvert.DeserializeObject<Dictionary<string, List<RawTable>>>(returndata);
            }
        }*/

        public override DataTable GetQueryData(string sqlstr)
        {
            return null;
        }

        public override List<RawTable> GetRawData(string name, bool exactMatch = false)
        {
            List<RawTable> ret = null;
            if (data != null && data.ContainsKey(name))
            {
                ret = data[name];
            }
            return ret;
        }

        public override bool SendData(string command)
        {
            myServer.sendData(command);
            return true;
        }

        public override void Close()
        {
            myServer.Stop();
        }
    }
}
