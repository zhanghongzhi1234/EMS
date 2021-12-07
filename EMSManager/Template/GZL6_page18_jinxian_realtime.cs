using System;
using System.Collections.Generic;
using System.Data;
//using System.Data.Linq.SqlClient;
//using System.Data.Objects.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Visifire.Charts;


namespace TemplateProject
{
    public partial class GZL6_page18_jinxian_realtime
    {
        private const string CLASS_NAME = "TemplateProject.GZL6_page18_jinxian_realtime";
        string titleBasic = "实时电能趋势图";
        DatabaseServer dbServer;
        DataTemplate headerTemplate1;
        DataTemplate headerTemplate2;
        int PollingInterval;            //Polling Interval for datapoints in Seconds, config in config.ini
        int TimeFrame;                  //Chart showing total length in Seconds, config in config.ini
        const int maxPoint = 6;         //max point show in 1 Chart
        DispatcherTimer timerPoll;
        EmsNode currentNode = null;
        //string currentLocationKey = "7";
        int maxDPKept;                  //max datapoint kept for trending chart, if exceed, the early datapoint will be removed. this value is calculated from TimeFrame
        List<RawTable> data1_1 = new List<RawTable>();
        Random random = new Random();
        Queue<double> chart4ValuesQ = new Queue<double>();

        //will called by constructor
        void InitScript()
        {
            btnExpandAll.Click += btnExpandAll_Click;
            btnExpandAll.MouseEnter += menuButton_MouseEnter;
            btnExpandAll.MouseLeave += menuButton_MouseLeave;
        }

        void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button.Content.ToString() == "全部展开")
            {
                ExpandAll(treeView1, true);
                button.Content = "全部收缩";
            }
            else
            {
                ExpandAll(treeView1, false);
                button.Content = "全部展开";
            }
        }

        private void ExpandAllTreeViewItem(TreeViewItem item, bool expand)
        {
            foreach (TreeViewItem subItem in item.Items)
            {
                //ItemsControl childControl = items.ItemContainerGenerator.ContainerFromItem(obj) as ItemsControl;
                //if (childControl != null)
                //{
                ExpandAllTreeViewItem(subItem, expand);
                //}
                subItem.IsExpanded = expand;
            }
        }

        private void ExpandAll(TreeView treeView, bool expand)
        {

            foreach (TreeViewItem item in treeView.Items)
            {
                ExpandAllTreeViewItem(item, expand);
                item.IsExpanded = expand;
            }
        }

        private void item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)e.Source;
            if (!item.IsSelected)       //double click will trigger event for all the parent node
                return;
            //clear all chart first;
            ClearAllDataSource();
            TreeViewItem itemParent = (TreeViewItem)item.Parent;
            string header = ((TreeViewItem)e.Source).Header.ToString();
            //txtDevice.Text = itemParent.Header.ToString() + "/" + header;
            txtDevice.Text = header;
            string pkey = item.Tag.ToString();
            if (!CachedMap.Instance.dicNode.ContainsKey(pkey))
            {
                MessageBox.Show(header + " is not configed in node tree!");
                return;
            }
            currentNode = CachedMap.Instance.dicNode[pkey];
            string equipment_code = currentNode.equipment_code;
            string locationKey = CachedMap.Instance.dicNode[pkey].locationkey;
            DebugUtil.Instance.LOG.Info("Select equipment=" + equipment_code + ", locationkey=" + locationKey);
            if (equipment_code != "")
            {
                //CachedMap.Instance.opcServer.RemoveAllDPs();
                //CachedMap.Instance.opcServer.AddDPToOPCServer(dpNameList);
                List<string> dpNameList = GetDpNameList(equipment_code, locationKey);
                CachedMap.Instance.opcServer.AddDPToOPCServer(dpNameList);
                //Dictionary<int, string> dpList = GetDpList(equipment_code, locationKey);
                //CachedMap.Instance.scadaServer.AddDPToOPCServer(dpList);

                /*List<RawTable> alarmNumberList1 = new List<RawTable>();
                RawTable rawTable1, rawTable2, rawTable3, rawTable4, rawTable5, rawTable6;
                int YValue1 = 0, YValue2 = 0, YValue3 = 0, YValue4 = 0, YValue5 = 0, YValue6 = 0;
                DateTime XValue = new DateTime(2012, 1, 1);
                alarmNumberList1.Add(rawTable1);
                dataSourceMap["Data5_1"].ReloadChartData(alarmNumberList1);*/
            }
            /*foreach (KeyValuePair<string, DataSource> pair in dataSourceMap)
            {
                pair.Value.dataSeries.DataPoints.Clear();
            }*/
            //System.Threading.Thread.Sleep(2000);
        }

        private List<string> GetDpNameList(string equipment_code, string locationkey)
        {
            string locationname = CachedMap.Instance.dicLocation[locationkey].name;
            string sqlstr = "select name from entity where name like '" + locationname + "%." + equipment_code + ".%iiEMS%'";      //for example: SHG%.IPA01.aiiEMS ,    it will select all EMS datapoint of this node, include aii,dii,mii
            //string sqlstr = "select name from entity where name like '" + locationname + "%." + equipment_code + ".%iiEMS%Acurrent'";      //for example: SHG%.IPA01.aiiEMS ,    it will select all EMS datapoint of this node, include aii,dii,mii
            List<string> nameList = DAIHelper.Instance.DbServer.GetVectorData(sqlstr);
            return nameList;
        }

        private List<string> GetDpKeyList(string equipment_code, string locationkey)
        {
            string locationname = CachedMap.Instance.dicLocation[locationkey].name;
            string sqlstr = "select pkey from entity where name like '" + locationname + "%." + equipment_code + ".%iiEMS%'";      //for example: SHG%.IPA01.aiiEMS ,    it will select all EMS datapoint of this node, include aii,dii,mii
            //string sqlstr = "select pkey from entity where name like '" + locationname + "%." + equipment_code + ".%iiEMS%current'";      //for example: SHG%.IPA01.aiiEMS ,    it will select all EMS datapoint of this node, include aii,dii,mii
            List<string> pkeyList = DAIHelper.Instance.DbServer.GetVectorData(sqlstr);
            return pkeyList;
        }

        private Dictionary<int, string> GetDpList(string equipment_code, string locationkey)
        {
            Dictionary<int, string> ret = new Dictionary<int, string>();
            string locationname = CachedMap.Instance.dicLocation[locationkey].name;
            string sqlstr = "select pkey,name from entity where name like '" + locationname + "%." + equipment_code + ".%iiEMS%'";      //for example: SHG%.IPA01.aiiEMS ,    it will select all EMS datapoint of this node, include aii,dii,mii
            //string sqlstr = "select pkey from entity where name like '" + locationname + "%." + equipment_code + ".%iiEMS%current'";      //for example: SHG%.IPA01.aiiEMS ,    it will select all EMS datapoint of this node, include aii,dii,mii
            DataTable dt = DAIHelper.Instance.DbServer.GetQueryData(sqlstr);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int pkey = Convert.ToInt32(dt.Rows[i]["pkey"].ToString());
                string name = dt.Rows[i]["name"].ToString();
                ret[pkey] = name;
            }

            return ret;
        }

		void LoadScript()
        {
            DebugUtil.Instance.LOG.Info("Init realtime page start");
            dbServer = DAIHelper.Instance.DbServer;
            headerTemplate1 = (DataTemplate)treeView1.FindResource("headerTemplate1");
            headerTemplate2 = (DataTemplate)treeView1.FindResource("headerTemplate2");
            InitTreeView();

            try { PollingInterval = Convert.ToInt32(CachedMap.Instance.GetRunParam("PollingInterval")); }       catch (Exception) { PollingInterval = 5; }
            try { TimeFrame = Convert.ToInt32(CachedMap.Instance.GetRunParam("TimeFrame")); }                   catch (Exception) { TimeFrame = 5 * 60; }
            maxDPKept = TimeFrame / PollingInterval;

            InitAllChart();
            //Start the timerPoll
            timerPoll = new DispatcherTimer();
            timerPoll.Interval = TimeSpan.FromSeconds(PollingInterval);
            timerPoll.Tick += new EventHandler(timerPoll_Tick);
            timerPoll.Start();

            DebugUtil.Instance.LOG.Info("Init realtime page end");
        }

        private void InitTreeView()
        {
            treeView1.Items.Clear();
            foreach (KeyValuePair<string, Location> pair in CachedMap.Instance.dicLocation)
            {
                string locationkey = pair.Key;
                string locationName = pair.Value.display_name;
                TreeViewItem item = CreateItemForLocation(locationName, locationkey);
                treeView1.Items.Add(item);
            }
        }

        private TreeViewItem CreateItemForLocation(string locationName, string locationkey)
        {
            TreeViewItem itemStation = new TreeViewItem();
            itemStation.Header = locationName;
            itemStation.IsExpanded = false;
            itemStation.HeaderTemplate = headerTemplate1;

            AddChildForItem(itemStation, locationkey);
            return itemStation;
        }

        private void AddChildForItem(TreeViewItem itemParent, string locationkey, string parentkey = "")
        {
            List<EmsNode> listChild;
            if (parentkey == "")        //top level in this location
                listChild = CachedMap.Instance.dicNode.Values.Where(p => p.parentkey == "" && p.locationkey == locationkey).ToList();
            else
                listChild = CachedMap.Instance.dicNode.Values.Where(p => p.parentkey == parentkey && p.locationkey == locationkey).ToList();

            if (listChild.Count == 0)
                return;

            foreach (EmsNode node in listChild)
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = node.name;
                item.IsExpanded = false;
                item.PreviewMouseDoubleClick += item_MouseDoubleClick;
                item.GiveFeedback += item_GiveFeedback;
                item.Tag = node.pkey;
                itemParent.Items.Add(item);

                if (hasChild(node.pkey))
                {
                    item.HeaderTemplate = headerTemplate1;
                    AddChildForItem(item, locationkey, node.pkey);
                }
                else
                {
                    item.HeaderTemplate = headerTemplate2;
                }
            }
        }

        private bool hasChild(string parentkey)
        {
            return CachedMap.Instance.dicNode.Values.ToList().Exists(p => p.parentkey == parentkey);
        }

        private void InitAllChart()
        {
            initChart(Chart1);
            initChart(Chart2);
            initChart(Chart4);
            initChart(Chart6);
        }

        private void initChart(Chart chart)
        {
            chart.AxesX[0].Interval = TimeFrame / maxPoint;
            chart.AxesX[0].IntervalType = IntervalTypes.Seconds;
        }

        //for Chart 1,2,4,6, the XValue and timespan is same.
        private void timerPoll_Tick(object sender, EventArgs e)
        {
            if (currentNode != null && currentNode.equipment_code != "")
            {
                UpdateDataSource("Data1_1");
                UpdateDataSource("Data1_2");
                UpdateDataSource("Data1_3");
                UpdateDataSource("Data2_1");
                UpdateDataSource("Data2_2");
                UpdateDataSource("Data2_3");
                UpdateDataSource("Data2_4");
                UpdateDataSource("Data4_1");
                UpdateDataSource("Data4_2");
                UpdateDataSource("Data4_3");
                UpdateDataSource("Data5_1");
                UpdateDataSource("Data5_2");
                UpdateDataSource("Data5_3");
                UpdateDataSource("Data6_1");
                UpdateDataSource("Data6_2");
                UpdateDataSource("Data6_3");
                UpdateDataSource("Data6_4");
                UpdateDataSource("Data6_5");
                UpdateDataSource("Data6_6");

                UpdateChart(Chart1);
                UpdateChart(Chart2);
                UpdateChart(Chart4);
                UpdateChart(Chart6);
            }
        }

        //dataName = Data1_1, Data1_2.....
        private void UpdateDataSource(string dataSourceName)
        {
            DataSource dataSource = dataSourceMap[dataSourceName];
            string Channel = dataSource.Channel;
            if (CachedMap.Instance.isSetRunParam(Channel))
            {
                string locationname = CachedMap.Instance.dicLocation[currentNode.locationkey].name;
                string dpNameRule = CachedMap.Instance.GetRunParam(Channel);
                string dpNameLike = locationname + "%." + currentNode.equipment_code + "." + dpNameRule;      //for example: SHG%.IPA01.aiiEMS-Acurrent
                //List<string> dpNameList1 = CachedMap.Instance.opcServer.dpNameList.Where(p => SqlMethods.Like(p, dpNameLike) == true).ToList();
                //List<string> dpNameList1 = CachedMap.Instance.opcServer.dpNameList.Where(p => SqlFunctions.PatIndex(dpNameLike, p) > 0).ToList();
                Regex regLike = CachedMap.Instance.LikeToRegex(dpNameLike);
                List<string> dpNameList1 = CachedMap.Instance.opcServer.dicAllDps.Keys.ToList().Where(p => regLike.IsMatch(p)).ToList();
                if (CachedMap.Instance.useDummyData == true)
                {
                    dpNameList1.Add("Dummy");
                }
                //List<string> dpNameList1 = CachedMap.Instance.scadaServer.dicDPs.Keys.ToList().Where(p => regLike.IsMatch(p)).ToList();
                List<RawTable> data1 = new List<RawTable>();
                if (dataSource.ChartType == "Line")        //it is trending chart
                {
                    foreach (string dpName in dpNameList1)
                    {
                        double dpValue = 0;
                        int quality = 0;
                        CachedMap.Instance.opcServer.GetLatestDataPointValue(dpName, ref dpValue, ref quality);
                        if (dataSource.name.Contains("Data4"))
                        {
                            dpValue = 380 + random.Next(-10, 10);
                        }
                        //CachedMap.Instance.scadaServer.GetLatestDataPointValue(dpName, ref dpValue);
                        //for the new datapoint list, it should only have 1 item and it is the last value because 1 line mapping to 1 datapoint currently
                        /*if (dataSource.dataSeries.DataPoints.Count + 1 > maxDPKept)
                        {
                            dataSource.RemoveDataPoint(0);
                        }*/
                        RawTable rawTable = new RawTable(DateTime.Now, dpValue);
                        dataSource.AddDataPoint(rawTable);
                        DebugUtil.Instance.LOG.Debug("Update " + dataSourceName + ", read '" + dpName + "', new value=" + dpValue);
                        if (dataSource.name.Contains("Data4"))
                        {
                            chart4ValuesQ.Enqueue(dpValue);
                            Chart4.AxesY[0].AxisMaximum = chart4ValuesQ.Max() + 5;
                            Chart4.AxesY[0].AxisMinimum = chart4ValuesQ.Min();
                            if (chart4ValuesQ.Count() > maxDPKept * 3)
                                chart4ValuesQ.Dequeue();
                            //AdjustAxisYRangeByNewValue(Chart4, dpValue);
                        }
                    }
                }
                else
                {   //we suppose it is column chart for harmonic wave map
                    foreach (string dpName in dpNameList1)
                    {
                        double dpValue = 0;
                        int quality = 0;
                        CachedMap.Instance.opcServer.GetLatestDataPointValue(dpName, ref dpValue, ref quality);
                        //CachedMap.Instance.scadaServer.GetLatestDataPointValue(dpName, ref dpValue);
                        string[] temp = dpName.Split('-');
                        if (temp.Count() >= 1)
                        {
                            string temp1 = temp[temp.Count() - 1];
                            int pos = dpNameRule.IndexOf('%');
                            if (pos != -1)
                            {
                                string suffix = dpNameRule.Substring(pos + 1, dpNameRule.Length - pos - 1);
                                string temp2 = temp1.Substring(0, temp1.IndexOf(suffix));       //temp2 should be 3,5,7,11,13
                                RawTable rawTable = new RawTable(temp2, dpValue);
                                data1.Add(rawTable);
                            }
                        }
                    }
                    dataSource.ReloadChartData(data1);
                }
            }
        }

        private void UpdateChart(Chart chart)
        {
            chart.AxesX[0].AxisMaximum = DateTime.Now;
            chart.AxesX[0].AxisMinimum = DateTime.Now - new TimeSpan(0, 0, TimeFrame);

        }

        private void ClearAllDataSource()
        {
            ClearDataSource("Data1_1");
            ClearDataSource("Data1_2");
            ClearDataSource("Data1_3");
            ClearDataSource("Data2_1");
            ClearDataSource("Data2_2");
            ClearDataSource("Data2_3");
            ClearDataSource("Data2_4");
            ClearDataSource("Data4_1");
            ClearDataSource("Data4_2");
            ClearDataSource("Data4_3");
            ClearDataSource("Data5_1");
            ClearDataSource("Data5_2");
            ClearDataSource("Data5_3");
            ClearDataSource("Data6_1");
            ClearDataSource("Data6_2");
            ClearDataSource("Data6_3");
            ClearDataSource("Data6_4");
            ClearDataSource("Data6_5");
            ClearDataSource("Data6_6");
        }

        //dataName = Data1_1, Data1_2.....
        private void ClearDataSource(string dataSourceName)
        {
            DataSource dataSource = dataSourceMap[dataSourceName];
            dataSource.dataSeries.DataPoints.Clear();
        }

        /*private List<RawTable> CreateTrendData(double value)
        {
            List<RawTable> ret;

            return ret;
        }*/

        /*private void AdjustAxisYRangeByNewValue(Chart chart, double newValue)
        {
            AdjustAxisYRangeByNewValue(chart, Convert.ToInt32(newValue));
        }

        private void AdjustAxisYRangeByNewValue(Chart chart, int newValue)
        {
            int temp = (newValue / 20) * 20;
            int axisMin = temp - 20;
            int axisMax = temp + 20;
            chart.AxesY[0].AxisMinimum = axisMin;
            chart.AxesY[0].AxisMaximum = axisMax;
        }*/
    }
}
