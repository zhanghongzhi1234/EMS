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
using Visifire.Charts;

namespace TemplateProject
{
    public partial class GZL6_page27_fullline_power
    {
        Random random = new Random();
        void InitScript()
        {
            btnQuery.Click += btnQuery_Click;
        }

        void LoadScript()
        {
            DebugUtil.Instance.LOG.Info("Init full line page start");
            dpStartDate.SelectedDate = DateTime.Now.Date;
            dpEndDate.SelectedDate = DateTime.Now.Date;

            AddItemForPowerType("正向有功电能", "SumActivePower");
            AddItemForPowerType("负向有功电能", "NeSumActivePower");
            AddItemForPowerType("正向无功电能", "SumReactivePower");
            AddItemForPowerType("负向无功电能", "NeSumReactivePower");
            cmbPowerType.SelectedIndex = 0;
            DebugUtil.Instance.LOG.Info("Init full line page end");
        }

        private void AddItemForPowerType(string content, string tag)
        {
            ComboBoxItem item = new ComboBoxItem();
            item.Content = content;
            item.Tag = tag;
            cmbPowerType.Items.Add(item);
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            DateTime startDate = dpStartDate.SelectedDate.Value.Date;
            DateTime endDate = dpEndDate.SelectedDate.Value.Date;
            if (endDate < startDate)
                System.Windows.MessageBox.Show("End date is small than start date!");

            DebugUtil.Instance.LOG.Info("Query startDate=" + startDate.ToShortDateString() + ", endDate=" + endDate.ToShortDateString());
            int seed = (endDate - startDate).Days;
            ComboBoxItem item = (ComboBoxItem)cmbPowerType.SelectedItem;
            txtInfo.Text = item.Content.ToString() + " " + startDate.ToShortDateString() + " - " + endDate.ToShortDateString();

            //calculate power
            List<RawTable> Data1_1 = GetAllLocationPowerByLevel("照明用电", startDate, endDate, item.Tag.ToString());
            List<RawTable> Data1_2 = GetAllLocationPowerByLevel("动力用电", startDate, endDate, item.Tag.ToString());
            List<RawTable> Data1_3 = GetAllLocationPowerByLevel("商业用电", startDate, endDate, item.Tag.ToString());

            dataSourceMap["Data1_1"].ReloadChartData(Data1_1);
            dataSourceMap["Data1_2"].ReloadChartData(Data1_2);
            dataSourceMap["Data1_3"].ReloadChartData(Data1_3);
            for (int i = 0; i < Data1_1.Count(); i++)
            {
                Chart1.Series[0].DataPoints[i].ToolTipText = "照明用电: " + Data1_1[i].YValue;
                Chart1.Series[1].DataPoints[i].ToolTipText = "动力用电: " + Data1_2[i].YValue;
                Chart1.Series[2].DataPoints[i].ToolTipText = "商业用电: " + Data1_3[i].YValue;
                Chart1.Series[2].DataPoints[i].LabelText = (Data1_1[i].YValue + Data1_2[i].YValue + Data1_3[i].YValue).ToString();
            }

            /*Random random = new Random(seed);
            for (int i = 1; i <= 2; i++)
            {
                List<RawTable> data = new List<RawTable>();
                foreach (string location in locations)
                {
                    int value = random.Next(90, 110) * seed / 10;
                    RawTable rawTable = new RawTable(location, value);
                    data.Add(rawTable);
                }
                string name = "Data1_" + i;
                dataSourceMap[name].ReloadChartData(data);
            }*/
            //Reload(true);
        }

        private List<RawTable> GetAllLocationPowerByLevel(string subsystemName, DateTime startDate, DateTime endDate, string nameRule)
        {
            List<RawTable> data = new List<RawTable>();
            foreach (KeyValuePair<string, Location> pair in CachedMap.Instance.dicLocation)
            {
                string locationkey = pair.Key;
                double value = 0;
                //IEnumerable<EmsNode> leafNodeList = CachedMap.Instance.GetLeafEMSEntityBySubsystem(subsystemName, locationkey);
                List<EmsNode> leafNodeList = CachedMap.Instance.GetLeafEMSEntityBySubsystem(subsystemName, locationkey).ToList();
                foreach (EmsNode node in leafNodeList)
                {
                    int entitykey = 0;
                    if (nameRule == "SumActivePower")
                    {
                        entitykey = node.SumActiveKey;
                    }
                    else if (nameRule == "NeSumActivePower")
                    {
                        entitykey = node.NeSumActiveKey;
                    }
                    else if (nameRule == "SumReactivePower")
                    {
                        entitykey = node.SumReactiveKey;
                    }
                    else if (nameRule == "NeSumReactivePower")
                    {
                        entitykey = node.NeSumReactiveKey;
                    }
                    //double valueStart = DAIHelper.Instance.GetDataLogForEntityOnTime(entitykey, startDate);
                    //double valueEnd = DAIHelper.Instance.GetDataLogForEntityOnTime(entitykey, endDate + new TimeSpan(1, 0, 0, 0));
                    double valueStart = 0;
                    double valueEnd = 0;
                    Dictionary<DateTime, double> dicValue = DAIHelper.Instance.GetDataLogForEntityBetweenTime(entitykey, startDate, endDate);
                    if (dicValue.Count >= 1)
                    {
                        valueStart = dicValue.First().Value;
                        valueEnd = dicValue.Last().Value;
                    }
                    if(valueEnd >= valueStart)
                        value += valueEnd - valueStart;
                }
                if (CachedMap.Instance.useDummyData == true)
                {
                    value = random.Next(90, 110) / 10;
                }
                RawTable rawTable = new RawTable(pair.Value.display_name, value);
                data.Add(rawTable);
            }
            return data;
        }
    }
}
