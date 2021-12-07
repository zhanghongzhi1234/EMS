using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TemplateProject
{
    public sealed class CachedMap
    {
        private static volatile CachedMap instance;        //singleton
        private static object syncRoot = new Object();

        private Dictionary<string, string> runParams = new Dictionary<string, string>();

        public Dictionary<string, EmsNode> dicNode = new Dictionary<string, EmsNode>();         //all EMSNode, pkey->EMSNode
        public Dictionary<string, Location> dicLocation = new Dictionary<string, Location>();       //all Location, pkey->Location
        public Dictionary<int, string> dicAllEntity = new Dictionary<int, string>();      //all EMS entity, pkey->name

        DatabaseServer dbServer = null;
        public OPCServer opcServer = null;
        public ScadaServer scadaServer = null;

        public bool useDummyData = false;

        private CachedMap()
        {
            ParseCommandLine();
            ReadConfigFile();
            //LoadConfigFromDB();     //cannot add here, because db will call CachedMap to get runparam, endless loop
            if (isSetRunParam("OPCServerName"))
            {
                opcServer = new OPCServer(GetRunParam("OPCServerName"));
                DebugUtil.Instance.LOG.Info("Create OPC Server Successfully");
            }

            if (isSetRunParam("ScadaServerName"))
            {
                scadaServer = new ScadaServer(GetRunParam("ScadaServerName"));
                DebugUtil.Instance.LOG.Info("Create Scada Server Successfully");
            }

            if (isSetRunParam("UseDummyData"))
            {
                if (GetRunParam("UseDummyData") == "true")
                {
                    useDummyData = true;
                    DebugUtil.Instance.LOG.Info("UseDummyData");
                }
            }
            DebugUtil.Instance.LOG.Info("CachedMap init successfully");
        }

        ~CachedMap()
        {
            if(opcServer != null)
                opcServer.Close();
            if(scadaServer != null)
                scadaServer.Close();
        }

        public static CachedMap Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new CachedMap();
                    }
                }

                return instance;
            }
        }

        /** Add a parameter
		  * @param name Name of parameter
          * @param value Value of parameter
          * Pre: name and value are not NULL
		  */
        public void SetRunParam(string name, string value)
        {
            runParams[name] = value;
        }

        /**Retrieve a parameter value
          * @return Value of parameter
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public string GetRunParam(string name)
        {
            return runParams[name];
        }

        /** Determine whether a parameter with the given name has been set
          * @return True (parameter set), False (parameter not set)
		  * @param name Name of parameter
          * Pre: name is not NULL
          */
        public bool isSetRunParam(string name)
        {
            bool isSet = runParams.ContainsKey(name);
            return isSet;
        }

        public void ParseCommandLine()
        {
            DebugUtil.Instance.LOG.Info("ParseCommandLine Start");
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                try
                {
                    int index = arg.IndexOf('=');
                    //string[] sArray = strReadLine.Split('=');
                    string name = arg.Substring(0, index);  //sArray[0];
                    string value = arg.Substring(index + 1, arg.Length - index - 1); //sArray[1];
                    runParams[name] = value;
                }
                catch (Exception)
                {
                }
            }
            DebugUtil.Instance.LOG.Info("ParseCommandLine End");
        }

        // Read start.ini config file to parse the db connection parameter
        public void ReadConfigFile()
        {
            DebugUtil.Instance.LOG.Info("ReadConfigFile Start");
            // 读取文件的源路径及其读取流
            string strReadFilePath = "EMSManager.ini";
            Dictionary<string, string> ret = new Dictionary<string, string>();
            if (File.Exists(strReadFilePath))
            {
                StreamReader srReadFile = new StreamReader(strReadFilePath);

                // 开始解析文件
                try
                {
                    while (!srReadFile.EndOfStream)
                    {
                        string strReadLine = srReadFile.ReadLine().Trim();
                        if (strReadLine == "" || strReadLine.StartsWith("[") || strReadLine.StartsWith("//"))
                            continue;
                        int index = strReadLine.IndexOf('=');
                        //string[] sArray = strReadLine.Split('=');
                        string name = strReadLine.Substring(0, index).Trim();  //sArray[0];
                        name = name.TrimStart('-');
                        string value = strReadLine.Substring(index + 1, strReadLine.Length - index - 1).Trim(); //sArray[1];
                        runParams[name] = value;
                    }
                }
                catch (System.Exception e)
                {
                }
                // 关闭读取流文件
                srReadFile.Close();
            }
            DebugUtil.Instance.LOG.Info("ReadConfigFile End");
        }

        // load config data from db
        public void LoadConfigFromDB()
        {
            dbServer = DAIHelper.Instance.DbServer;
            if (dbServer != null)
            {
                LoadAllLocationFromDB();
                LoadAllEMSEntityFromDB();
                LoadAllNodesFromDB();
            }
            else
            {
                DebugUtil.Instance.LOG.Error("No database connection!");
            }
        }

        private void LoadAllLocationFromDB()
        {
            DebugUtil.Instance.LOG.Info("LoadAllLocationFromDB Start");
            string sqlstr = "select * from location where pkey=2 or (pkey>=5 and pkey<=80) order by pkey asc";
            if (dbServer != null)
            {
                DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    Location item = new Location();
                    item.pkey = myField["pkey"].ToString();
                    item.name = myField["name"].ToString();
                    item.display_name = myField["display_name"].ToString();
                    dicLocation[item.pkey] = item;
                }
                DebugUtil.Instance.LOG.Info("LoadAllLocationFromDB End");
            }
            else
            {
                DebugUtil.Instance.LOG.Error("No database connection!");
            }
        }

        private void LoadAllEMSEntityFromDB()
        {
            DebugUtil.Instance.LOG.Info("LoadAllEMSEntityFromDB Start");
            //string sqlstr = "select pkey,name from entity where (name like '%aiiEMS-%' or name like '%miiEMS-%') and deleted=0";
            string sqlstr = "select pkey,name from entity where (name like '%EMS%aii%' or name like '%EMS%mii%') and deleted=0";
            DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            //For each field in the table...
            foreach (DataRow myField in schemaTable.Rows)
            {
                //EmsNode item = new EmsNode();
                int pkey = Convert.ToInt32(myField["pkey"]);
                string name = myField["name"].ToString();
                dicAllEntity[pkey] = name;
            }
            DebugUtil.Instance.LOG.Info("LoadAllEMSEntityFromDB End");
        }

        private void LoadAllNodesFromDB()
        {
            DebugUtil.Instance.LOG.Info("LoadAllNodesFromDB Start");
            string sqlstr = "select * from ems_node_tree";
            DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            //For each field in the table...
            foreach (DataRow myField in schemaTable.Rows)
            {
                EmsNode item = new EmsNode();
                item.pkey = myField["pkey"].ToString();
                item.name = myField["name"].ToString();
                item.locationkey = myField["locationkey"].ToString();
                item.parentkey = myField["parentkey"].ToString();
                item.equipment_code = myField["equipment_code"].ToString();
                item.power_subsystem = myField["power_subsystem"].ToString();
                item.calculateDpKeyForLeaf();
                dicNode[item.pkey] = item;
            }
            DebugUtil.Instance.LOG.Info("LoadAllNodesFromDB End");
        }

        //convert SQL like statement to Regex statement, where % means zero-or-more characters and _ means one character
        public Regex LikeToRegex(string likeExpresson)
        {
            Regex regLike = new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(likeExpresson, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline);
            return regLike;
        }

        public void GetLeafEMSEntityUnderNode(string nodeKey, List<EmsNode> leafNodeList, string locationkey)
        {
            List<EmsNode> ret = new List<EmsNode>();
            List<EmsNode> childList = dicNode.Values.ToList().Where(p=>p.parentkey == nodeKey && p.locationkey == locationkey).ToList();
            if (childList != null && childList.Count > 0)
            {
                foreach (EmsNode node in childList)
                {
                    if (node.equipment_code != "")
                    {   //it is leaf node
                        leafNodeList.Add(node);
                    }
                    else
                    {
                        GetLeafEMSEntityUnderNode(node.pkey, leafNodeList, locationkey);
                    }
                }
            }
        }

        public IEnumerable<EmsNode> GetLeafEMSEntityBySubsystem(string subsystemName, string locationkey)
        {
            IEnumerable<EmsNode> leafNodeList = CachedMap.Instance.dicNode.Values.Where(p => p.locationkey == locationkey && p.power_subsystem == subsystemName);
            return leafNodeList;
        }

        public List<string> GetSubsystemsByLocation(string locationKey)
        {
            List<string> fixedList = new List<string>() { "照明用电", "动力用电", "商业用电" };
            List<string> subsystemList = CachedMap.Instance.dicNode.Values.Where(p => p.locationkey == locationKey && !String.IsNullOrEmpty(p.power_subsystem) && !fixedList.Contains(p.power_subsystem)).Select(p => p.power_subsystem).Distinct().ToList();

            return fixedList.Concat(subsystemList).ToList();
        }
    }
}
