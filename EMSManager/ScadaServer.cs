using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Visifire.Charts;
using System.Data;
using System.Threading;

namespace TemplateProject
{
    public class ScadaServer : Server
    {
        const string dllPath = "datapointaccess.dll";
        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void init([In] string cmdLine);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr createDataPoint([In] string name);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr createDataPoint([In] int pkey, [In] string name);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr createDataPoints([In] string names);            //names is datapoint name split by ,

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getDataPoint([In] string name);

        [DllImport(dllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string getName([In] IntPtr dp);

        [DllImport(dllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string getStringValue([In] IntPtr dp);

        [DllImport(dllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool getBoolValue([In] IntPtr dp);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern double getNumValue([In] IntPtr dp);

        [DllImport(dllPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string updatedDataPoints();

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getQuality([In] IntPtr dp);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenEventA(uint lpEventAttributes, bool bInheritHandle, string lpName);

        [DllImport("kernel32.dll")]
        static extern uint WaitForSingleObject(IntPtr handle, uint dwMilliseconds);

        //DispatcherTimer timerPoll;
        //int pollInterval = 2000;            //Polling interval for server
        //List<RawTable> data = new List<RawTable>();
        //public List<string> dpNameList = new List<string>();
        public Dictionary<string, ScadaDataPoint> dicDPs = new Dictionary<string, ScadaDataPoint>();            //store value for all DPs
        Chart chart;
        bool isRunning = true;
        Task tUpdateAllDp;

        public ScadaServer(string name)
        {
            this.name = name;
            this.serverType = ServerType.SCADA;
            Init();
            //log.Info("Init SCADA Server");
        }

        public override void Init()
        {
            string bin = "F:/P4/GZL13_TIP/bin/win32_nd", lib = "F:/LocalTest/lib_GZL13";
            /*if (CachedMap.Instance.isSetRunParam("bin"))
                bin = CachedMap.Instance.GetRunParam("bin");
            if (CachedMap.Instance.isSetRunParam("lib"))
                lib = CachedMap.Instance.GetRunParam("lib");*/
            string path = Environment.GetEnvironmentVariable("PATH");
            //path += @";" + bin + ";" + lib;
            path += @";" + lib;
            log.Info("Path=" + path);
            Environment.SetEnvironmentVariable("PATH", path);

            try
            {
                init("--run-param-file=config_transactive.ini --debug-file=EMSManager_DPPolling.log --debug-level=DEBUG");
                //Start();
            }
            catch (Exception ex)
            {
                log.Error("SCADA init fail: " + ex.ToString());
            }

            //Thread th = new Thread(UpdateAllDataPoints);
            //th.Start();
            tUpdateAllDp = new Task(UpdateAllDataPoints);
            tUpdateAllDp.Start();
        }

        /*public void AddDPToOPCServer(List<string> dpList)
        {
            if (dpList != null)
            {
                List<string> temp = new List<string>();
                foreach (string item in dpList)
                {
                    string dpName = item.Trim();
                    if (dpName != "" && (dicDPs.ContainsKey(dpName) == false))
                    {
                        try
                        {
                            ScadaDataPoint dp = new ScadaDataPoint(dpName);
                            dicDPs[dpName] = dp;
                            temp.Add(dpName);
                        }
                        catch (System.Exception ex)
                        {
                            DebugUtil.Instance.LOG.Error("Exception got when add datapoint: " + dpName + ", exception is: " + ex.ToString());
                        }
                    }
                }
                if (temp.Count > 0)
                {
                    string names = String.Join(",", temp);
                    createDataPoints(names);
                }
            }
        }*/

        public void AddDPToOPCServer(Dictionary<int, string> dicDatapoint)
        {
            if (dicDatapoint != null)
            {
                foreach (KeyValuePair<int, string> pair in dicDatapoint)
                {
                    string dpName = pair.Value.Trim();
                    if (dpName != "" && dicDPs.ContainsKey(dpName) == false)
                    {
                        try
                        {
                            ScadaDataPoint dp = new ScadaDataPoint(pair.Key, dpName);
                            dicDPs[dpName] = dp;
                        }
                        catch (System.Exception ex)
                        {
                            DebugUtil.Instance.LOG.Error("Exception got when add datapoint: " + dpName + ", exception is: " + ex.ToString());
                        }
                    }
                }
                /*if (temp.Count > 0)
                {
                    string names = String.Join(",", temp);
                    createDataPoints(names);
                }*/
            }
        }

        //currently this function is not used
        public void SetObserver(Chart chart, string dpNames)
        {
            //dpNameList.Clear();
            /*string[] temp = dpNames.Split(',');

            dpNameList.AddRange(temp);
            this.chart = chart;*/
        }

        public override DataTable GetQueryData(string sqlstr)
        {
            return null;
        }

        public override bool SendData(string command)
        {
            return true;
        }

        public override List<RawTable> GetRawData(string name, bool exactMatch = false)
        {
            /*if (data.Count == 0)
            {
                string[] dpNames = name.Split(',');
                foreach (string dpName in dpNames)
                {
                    RawTable rawTable = new RawTable(dpName, 10);
                    data.Add(rawTable);
                }
            }*/
            return null;
        }

        private void UpdateAllDataPoints()
        {
            try
            {
                //IntPtr dp1 = createDataPoint("OCC.TIS.STIS.SEV.aiiTISC-CurrentSTISLibraryVersion");
                //IntPtr dp2 = createDataPoint("OCC.TIS.STIS.SEV.aiiTISC-NextSTISLibraryVersion");
                IntPtr e = OpenEventA(0x000F0000u | 0x00100000u | 0x3u, false, "DATAPOINT_UPDATE");
                while (isRunning)
                {
                    if (dicDPs.Count() > 0)
                    {
                        //WaitForSingleObject(e, 0xFFFFFFFFu);
                        List<string> dpNameList = dicDPs.Keys.ToList();
                        foreach (KeyValuePair<string, ScadaDataPoint> pair in dicDPs)
                        {
                            if (pair.Value.isObserved == false)
                            {
                                createDataPoint(pair.Value.pkey, pair.Value.name);        //check if add new datapoint
                                Console.WriteLine("createDataPoint:" + pair.Value.name);
                                //ScadaDataPoint dp = new ScadaDataPoint(dpName);
                                //ScadaDataPoint dp = new ScadaDataPoint(dpName);
                                //dicDPs[dpName] = dp;
                            }
                        }

                        string dps = updatedDataPoints();
                        foreach (string s in dps.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            for (int i = 0; i < dpNameList.Count; i++)
                            {
                                if (dpNameList[i] == s)
                                {
                                    IntPtr d = getDataPoint(s);
                                    //this.Dispatcher.Invoke(() =>
                                    //System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        dicDPs[s].value = getNumValue(d);
                                        dicDPs[s].quality = (int)getQuality(d);
                                    }
                                    //});
                                }
                            }
                        }
                    }
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                log.Error("update datapoint fail: " + ex.ToString());
            }
        }

        public override void Close()
        {
            isRunning = false;
        }

        //only return good quality, for bad quality it will keep last value
        public bool GetLatestDataPointValue(string dpName, ref double dpValue)
        {
            bool ret = false;
            try
            {
                if (dicDPs.ContainsKey(dpName) == false)
                    DebugUtil.Instance.LOG.Warn("Can't find this datapoint in the group.");
                else
                {
                    ScadaDataPoint dp = dicDPs[dpName];
                    /*if (dp.isObserved == false)
                    {
                        createDataPoint(dp.pkey, dp.name);
                        dp.isObserved = true;
                    }*/
                    DebugUtil.Instance.LOG.Debug("Datapoint " + dpName + ": value=: " + dp.ToString() + "quality=" + dp.quality.ToString());

                    if (dp.value != null && dp.value.ToString() != "" && dp.quality == 192)
                    {   //all datapoint is numeric type in EMS
                        dpValue = Convert.ToDouble(dp.value);
                        ret = true;
                    }
                    else
                    {
                        DebugUtil.Instance.LOG.Warn("Abnormal DP Value for Datapoint:" + dpName + ", value:" + dp.value.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                DebugUtil.Instance.LOG.Error(ex.ToString());
            }

            return ret;
        }
    }
}
