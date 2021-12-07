using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TemplateProject
{
    public partial class GZL6_page28_day_power
    {
        Random random = new Random();
        void InitScript()
        {
            btnQuery.Click += btnQuery_Click;
        }

        void LoadScript()
        {
            DebugUtil.Instance.LOG.Info("Init day power page start");
            dpDate.SelectedDate = DateTime.Now.Date;
            FillLevel1();
            DebugUtil.Instance.LOG.Info("Init day power page end");
        }

        //fill station names
        private void FillLevel1()
        {
            cmbLevel1.Items.Clear();
            foreach (KeyValuePair<string, Location> pair in CachedMap.Instance.dicLocation)
            {
                string locationkey = pair.Key;
                string locationName = pair.Value.display_name;
                ComboBoxItem item = new ComboBoxItem();
                //item.Name = locationName;
                item.Content = locationName;
                item.Tag = locationkey;
                cmbLevel1.Items.Add(item);
                //cmbLevel1.Items.Add(locationName);
            }
            cmbLevel1.SelectedIndex = 0;
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (dpDate.SelectedDate.HasValue == false)
            {
                MessageBox.Show("请选择日期!");
                dpDate.Focus();
                return;
            }

            DateTime selectedDate = dpDate.SelectedDate.Value.Date;
            string locationkey = ((ComboBoxItem)cmbLevel1.SelectedItem).Tag.ToString();
            DebugUtil.Instance.LOG.Info("Query date=" + selectedDate.ToShortDateString() + ", locationkey=" + locationkey);

            List<string> subsystemList = CachedMap.Instance.GetSubsystemsByLocation(locationkey);
            List<double> sumPowerList = new List<double>();
            foreach (string subsystem in subsystemList)
            {
                double sumPower = GetDaySumPowerByLevel(subsystem, selectedDate, locationkey);
                sumPowerList.Add(sumPower);
            }
            if (CachedMap.Instance.useDummyData == true)
            {
                DebugUtil.Instance.LOG.Info("Use dummy data");
                sumPowerList.Add(3);
                sumPowerList.Add(4);
                sumPowerList.Add(5);
            }
            {   //fill data for table 1
                DataTable dt = new DataTable();
                dt.Columns.Add("Device");
                dt.Columns.Add("Power");
                for (int i = 0; i < subsystemList.Count; i++)
                {
                    DataRow row = dt.NewRow();
                    row["Device"] = subsystemList[i];
                    row["Power"] = sumPowerList[i];
                    dt.Rows.Add(row);
                }
                FillListViewWithDataTable(listView1, dt);
            }
            {   //fill data for Chart1
                List<RawTable> Data1_1 = new List<RawTable>();
                for (int i = 0; i < subsystemList.Count; i++)
                {
                    RawTable rawTable = new RawTable(subsystemList[i], sumPowerList[i]);
                    Data1_1.Add(rawTable);
                }
                dataSourceMap["Data1_1"].ReloadChartData(Data1_1);
            }
            {   //fill data for Chart2
                List<string> dataSourceList = new List<string>();
                for (int i = 0; i < subsystemList.Count; i++)
                {
                    DataSource dataSource = new DataSource();
                    dataSource.name = "Data2_" + (i + 1).ToString();
                    dataSource.DataType = "Static";
                    dataSource.ChartType = "StackedColumn";
                    dataSource.LegendText = subsystemList[i];
                    dataSource.LabelEnabled = (i == subsystemList.Count - 1) ? "true" : "false";
                    dataSource.LabelStyle = "OutSide";
                    dataSource.CreateDataSeries();
                    dataSourceMap[dataSource.name] = dataSource;

                    dataSourceList.Add(dataSource.name);
                }

                ChartData chartData = new ChartData();
                chartData.name = "Chart2";
                if (dataSourceList.Count > 0)
                {
                    chartData.dataSourceList = dataSourceList;
                    chartDataMap[chartData.name] = chartData;
                    SetChart(chartData.name, chartData);
                }

                List<double> totalValue = new List<double>();
                for (int i = 0; i < subsystemList.Count; i++)
                {
                    List<RawTable> Data2 = GetDayPowerListByLevel(subsystemList[i], selectedDate, locationkey);
                    dataSourceMap["Data2_" + (i + 1).ToString()].ReloadChartData(Data2);
                    
                    for (int j = 0; j < Data2.Count; j++)
                    {
                        Chart2.Series[i].DataPoints[j].ToolTipText = subsystemList[i] + ": " + Data2[j].YValue;
                        if (i == 0)
                        {
                            totalValue.Add(Data2[j].YValue);
                        }
                        else
                        {
                            totalValue[j] += Data2[j].YValue;
                        }
                        if (i == subsystemList.Count - 1)
                        {
                            Chart2.Series[i].DataPoints[j].LabelText = totalValue[j].ToString();
                        }
                    }
                }
            }
            this.exportTitle = cmbLevel1.Text.ToString() + " " + selectedDate.ToString("yyyy年M月dd日");
        }

        private List<RawTable> GetDayPowerListByLevel(string levelKey, DateTime selectedDate, string locationkey)
        {
            selectedDate = selectedDate.Date;
            List<RawTable> data = new List<RawTable>();
            for(int i = 0; i <= 23; i++)
            {
                DateTime endTime = selectedDate + new TimeSpan(i, 0, 0);
                DateTime startTime = endTime - new TimeSpan(1, 0, 0);
                double value = 0;
                IEnumerable<EmsNode> leafNodeList = CachedMap.Instance.dicNode.Values.Where(p => p.locationkey == locationkey && p.power_subsystem == levelKey);
                foreach (EmsNode node in leafNodeList)
                {
                    int entitykey = node.SumActiveKey;
                    double valueStart = DAIHelper.Instance.GetDataLogForEntityOnTime(entitykey, startTime);
                    double valueEnd = DAIHelper.Instance.GetDataLogForEntityOnTime(entitykey, endTime);

                    if (valueStart != -Double.MaxValue && valueEnd != -Double.MaxValue && valueEnd >= valueStart)
                        value += valueEnd - valueStart;
                }
                if (CachedMap.Instance.useDummyData == true)
                {
                    value = random.Next(90, 110) / 10;
                }
                RawTable rawTable = new RawTable(i, value);
                data.Add(rawTable);
            }
            return data;
        }

        private double GetDaySumPowerByLevel(string subsystemName, DateTime selectedDate, string locationkey)
        {
            selectedDate = selectedDate.Date;
            double value = 0;
            DateTime endTime = selectedDate + new TimeSpan(1, 0, 0, 0);
            IEnumerable<EmsNode> leafNodeList = CachedMap.Instance.dicNode.Values.Where(p => p.locationkey == locationkey && p.power_subsystem == subsystemName);
            foreach (EmsNode node in leafNodeList)
            {
                int entitykey = node.SumActiveKey;
                double valueStart = DAIHelper.Instance.GetDataLogForEntityOnTime(entitykey, selectedDate);
                double valueEnd = DAIHelper.Instance.GetDataLogForEntityOnTime(entitykey, endTime);

                if (valueStart != -Double.MaxValue && valueEnd != -Double.MaxValue && valueEnd >= valueStart)
                    value += valueEnd - valueStart;
            }

            return value;
        }
    }
}
