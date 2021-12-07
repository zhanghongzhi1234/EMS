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
using System.Windows;
using System.Text.RegularExpressions;
using OpcLibrary;


namespace TemplateProject
{   //it is OPCClient actually
    public class OPCServer : Server
    {
        public string OPCServerName;
        public string hostname = "localhost";           //currently we only use local OPC server, remote OPC server should also available but we never test it
        public Dictionary<string, ScadaDataPoint> dicAllDps = new Dictionary<string, ScadaDataPoint>();
        private OpcServer _opcServer;
        //private IRequest _request = null;
        //private int _handle = 0;
        OpcGroup opcGroup;

        public OPCServer(string name)
        {
            this.OPCServerName = name.Trim();
            this.serverType = ServerType.OPC;
            Init();
        }

        public override void Init()
        {
            Connect();
            AddGroup("EMS_Group_1", 1000);
        }

        public bool isConnected()
        {
            if (this._opcServer != null)
                return true;
            else
                return false;
        }

        public void Connect()
        {
            if (OPCServerName != "")
            {
                if (this._opcServer != null)
                {
                    this._opcServer.Disconnect();
                    this._opcServer = null;
                }

                Type tp = Type.GetTypeFromProgID(OPCServerName);
                this._opcServer = new OpcServer(tp.GUID.ToString(), hostname);

                try
                {
                    this._opcServer.Connect();
                }
                catch (Exception ex)
                {
                    string strMsg = "Cannot Connect to " + OPCServerName + ", Check If OPCBridge is Started, reason = " + ex.ToString();
                    DebugUtil.Instance.LOG.Error(strMsg);
                }
            }
        }

        //updateRate: ms
        public void AddGroup(string groupName, int updateRate = 1000)
        {
            try
            {
                opcGroup = this._opcServer.AddGroup(groupName, updateRate, true);
                if (opcGroup != null)
                {
                    opcGroup.DataChanged += OnDataChange;
                }
            }
            catch (Exception ex)
            {
                string strMsg = "Add group(" + groupName + ") fail, reason = " + ex.ToString();
                DebugUtil.Instance.LOG.Error(strMsg);
            }
        }

        public void AddDPToOPCServer(List<string> nameList)
        {
            if (nameList.Count <= 0)
                return;

            List<string> filterList = nameList.Where(p => dicAllDps.ContainsKey(p) == false).ToList();
            List<string> filterValueNameList = filterList.Select(p => p + ".Value").ToList();
            if (opcGroup != null)
            {
                try
                {
                    foreach (string name in filterList)
                    {
                        ScadaDataPoint dp = new ScadaDataPoint(name);
                        dicAllDps[name] = dp;
                    }
                    ItemResult[] results = opcGroup.AddItems(filterValueNameList.ToArray());
                    foreach (ItemResult result in results)
                    {
                        if (result.ResultID.Failed())
                        {
                            string strMsg = "Failed to add opc item \'" + result.ItemName + "\'" + " Error: " + result.ResultID.Name;
                            DebugUtil.Instance.LOG.Error(strMsg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugUtil.Instance.LOG.Error(ex.Message);

                } // end catch
            }
        }

        private void OnDataChange(object subscriptionHandle, object requestHandle, ItemValueResult[] values)
        {
            /*if (InvokeRequired)
            {
                BeginInvoke(new DataChangedEventHandler(OnDataChange), new object[] { subscriptionHandle, requestHandle, values });
                return;
            }*/

            foreach (ItemValueResult value in values)
            {
                // item value should never have a null client handle.
                if (value.ClientHandle == null)
                {
                    continue;
                }
                UpdateDataPoints(value);
            }
        }

        private void UpdateDataPoints(ItemValueResult value)
        {
            ScadaDataPoint dp = dicAllDps.Values.ToList().Where(p => p.valueName == value.ItemName).FirstOrDefault();
            if (dp != null)
            {
                if (value.Value != null)
                {
                    dp.value = value.Value;
                }
                dp.quality = (int)value.Quality.QualityBits;
            }
        }

        public void RemoveAllDPs()
        {   //not implement yet, need remove item from opcGroup by client handle
            //opcGroup.
            dicAllDps.Clear();
        }

        public bool GetLatestDataPointValue(string dpName, ref double dpValue, ref int quality)
        {
            bool res = false;
            if (dicAllDps.ContainsKey(dpName))
            {
                ScadaDataPoint dp = dicAllDps[dpName];
                quality = dp.quality;
                if (dp.value != null)
                {
                    Double.TryParse(dp.value.ToString(), out dpValue);
                    DebugUtil.Instance.LOG.Debug("Come out GetLatestDataPointValue(),return :" + res);
                    return res;
                }
            }
            DebugUtil.Instance.LOG.Warn("Can't find this datapoint in the group.");
            return false;
        }

        public void ProcessResponse(string receivedData)
        {
            //Console.WriteLine(">> Received:" + data);
            //DebugUtil.Instance.LOG.Debug(receivedData);
            //data = JsonConvert.DeserializeObject<Dictionary<string, List<RawTable>>>(receivedData);
        }

        public override DataTable GetQueryData(string sqlstr)
        {
            return null;
        }

        //use GetLatestDataPointValue is more convenient
        public override List<RawTable> GetRawData(string name, bool exactMatch = false)
        {
            List<RawTable> ret = null;
            List<string> dpNameList1 = new List<string>();
            if (exactMatch == true)
            {
                dpNameList1.Add(name);
            }
            else
            {
                Regex regLike = CachedMap.Instance.LikeToRegex(name);
                dpNameList1 = dicAllDps.Keys.ToList().Where(p => regLike.IsMatch(p)).ToList();
            }
            foreach (string dpName in dpNameList1)
            {
                double dpValue = 0;
                int quality = 0;
                GetLatestDataPointValue(dpName, ref dpValue, ref quality);
                RawTable rawTable = new RawTable(DateTime.Now, dpValue);
                ret.Add(rawTable);
            }
            return ret;
        }

        public override bool SendData(string command)
        {
            //myServer.sendData(command);
            return true;
        }

        public override void Close()
        {
            try
            {
                if (_opcServer != null)
                {
                    _opcServer.Disconnect();
                    _opcServer = null;
                }
            }
            catch (System.Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Exception: " + ex.ToString());
            }
        }

        private void RemoveOpcItems(List<string> items)
        {   //remove item need client handle, not implement yet
        }
    }
}
