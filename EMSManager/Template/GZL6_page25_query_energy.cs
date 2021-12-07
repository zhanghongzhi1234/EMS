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
    public partial class GZL6_page25_query_energy
    {
        DataTable dtResult = new DataTable();
        string nodeKey;
        string nodePath;

        void InitScript()
        {
            DebugUtil.Instance.LOG.Info("Init energy page start");
            
            dataGrid1.Columns.Add(CreateLabelColumn("时间", 150, "datetime"));
            dataGrid1.Columns.Add(CreateLabelColumn("路径", 330, "asset"));
            dataGrid1.Columns.Add(CreateLabelColumn("15分钟正向有功电度", 135, "15SumActive"));
            dataGrid1.Columns.Add(CreateLabelColumn("15分钟反向有功电度", 135, "15NeSumActive"));
            dataGrid1.Columns.Add(CreateLabelColumn("15分钟正向无功电度", 135, "15SumReactive"));
            dataGrid1.Columns.Add(CreateLabelColumn("15分钟反向无功电度", 135, "15NeSumReactive"));
            dataGrid1.Columns.Add(CreateLabelColumn("正向有功电度", 110, "SumActive"));
            dataGrid1.Columns.Add(CreateLabelColumn("反向有功电度", 110, "NeSumActive"));
            dataGrid1.ColumnHeaderHeight = 30d;
            dataGrid1.RowHeight = 30d;

            dtResult.Columns.Add("datetime");
            dtResult.Columns.Add("asset");
            dtResult.Columns.Add("15SumActive");
            dtResult.Columns.Add("15NeSumActive");
            dtResult.Columns.Add("15SumReactive");
            dtResult.Columns.Add("15NeSumReactive");
            dtResult.Columns.Add("SumActive");
            dtResult.Columns.Add("NeSumActive");

            btnDevice.Click += btnDevice_Click;

            dpStartDate.SelectedDate = DateTime.Now.Date;
            dpEndDate.SelectedDate = DateTime.Now.Date;
            btnQuery.Click += btnQuery_Click;

            DebugUtil.Instance.LOG.Info("Init energy page end");
        }

        void LoadScript()
        {
            
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            navWindow.deviceWindow.AnimationShow();
        }

        private System.Windows.Controls.DataGridTemplateColumn CreateLabelColumn(string text, int width, string binding)
        {
            System.Windows.Controls.DataGridTemplateColumn column1 = new System.Windows.Controls.DataGridTemplateColumn();
            column1.Header = text;
            column1.Width = width;
            //column1.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            //column1.CanUserSort = true;
            System.Windows.DataTemplate dt1 = new System.Windows.DataTemplate();
            System.Windows.FrameworkElementFactory label = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Label));
            label.SetBinding(System.Windows.Controls.Label.ContentProperty, new System.Windows.Data.Binding(binding));
            label.SetValue(System.Windows.Controls.Label.VerticalContentAlignmentProperty, System.Windows.VerticalAlignment.Center);
            label.SetValue(System.Windows.Controls.Label.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            //label.SetValue(System.Windows.Controls.Label.BackgroundProperty, System.Windows.Media.Brushes.Black);             //must not set when use AlternatingRowBackground
            //label.SetValue(System.Windows.Controls.Label.ForegroundProperty, System.Windows.Media.Brushes.White);
            label.SetValue(System.Windows.Controls.Label.BorderThicknessProperty, new System.Windows.Thickness(0));
            //label.SetValue(System.Windows.Controls.Label.HeightProperty, 40d);            //can set at dataGrid.rowheight

            dt1.DataType = typeof(System.Windows.Controls.Label);
            dt1.VisualTree = label;
            column1.CellTemplate = dt1;

            System.Windows.Style style = new System.Windows.Style();
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
            //style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, System.Windows.FontWeights.Bold));
            //style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 20d));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, System.Windows.Media.Brushes.Silver));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderThicknessProperty, new System.Windows.Thickness(0, 0, 1, 1)));
            column1.HeaderStyle = style;

            return column1;
        }

        private System.Windows.Controls.DataGridTemplateColumn CreateImageColumn(string text, int width, string binding)
        {
            System.Windows.Controls.DataGridTemplateColumn column1 = new System.Windows.Controls.DataGridTemplateColumn();
            column1.Header = text;
            column1.Width = width;
            System.Windows.DataTemplate dt1 = new System.Windows.DataTemplate();
            System.Windows.FrameworkElementFactory image = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Image));
            image.SetBinding(System.Windows.Controls.Image.SourceProperty, new System.Windows.Data.Binding(binding));
            image.SetValue(System.Windows.Controls.Image.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            image.SetValue(System.Windows.Controls.Image.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            dt1.DataType = typeof(System.Windows.Controls.Image);
            dt1.VisualTree = image;
            column1.CellTemplate = dt1;

            System.Windows.Style style = new System.Windows.Style();
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, System.Windows.FontWeights.Bold));
            style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 20d));
            column1.HeaderStyle = style;

            return column1;
        }

        private string GetNodePathFromNodeKey(string nodeKey)
        {
            string nodePath = "";
            while (true)
            {
                if (CachedMap.Instance.dicNode.ContainsKey(nodeKey))
                {
                    EmsNode node = CachedMap.Instance.dicNode[nodeKey];
                    nodePath = node.name + " / " + nodePath;
                    if (String.IsNullOrEmpty(node.parentkey))
                        break;
                    else
                        nodeKey = node.parentkey;
                }
                else
                    break;
            }

            return nodePath.Trim().Trim('/');
        }

        //use DATALOG_DP_LOG_TREND, entity_key, value and planlogtime
        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (nodeKey == null)
            {
                MessageBox.Show("请先选择节点!");
                return;
            }

            if (dpStartDate.SelectedDate.HasValue == false)
            {
                MessageBox.Show("请选择起始日期!");
                dpStartDate.Focus();
                return;
            }
            if (dpEndDate.SelectedDate.HasValue == false)
            {
                MessageBox.Show("请选择结束日期!");
                dpEndDate.Focus();
                return;
            }
            if (dpStartDate.SelectedDate.Value > dpEndDate.SelectedDate.Value)
            {
                MessageBox.Show("起始日期不能大于结束日期!");
                dpStartDate.Focus();
                return;
            }
            DebugUtil.Instance.LOG.Info("StartDate=" + dpStartDate.SelectedDate.Value.ToShortDateString() + ", EndDate=" + dpEndDate.SelectedDate.Value.ToShortDateString() + ", device=" + nodePath);
            EmsNode node = CachedMap.Instance.dicNode[nodeKey];
            if (node == null)
            {
                MessageBox.Show("找不到此节点!");
                return;
            }

            dataGrid1.Items.Clear();
            dtResult.Rows.Clear();
            List<DateTime> timeList = new List<DateTime>();
            TimeSpan timeStep = new TimeSpan(0, 15, 0);
            DateTime timeStart = dpStartDate.SelectedDate.Value.Date;
            DateTime timeEnd = dpEndDate.SelectedDate.Value.Date + new TimeSpan(1, 0, 0, 0);
            for (DateTime timeIndex = timeStart; timeIndex < timeEnd; timeIndex += timeStep)
            {
                timeList.Add(timeIndex);
            }

            if (node.equipment_code != "")
            {   //it is a leaf node
                Dictionary<DateTime, double> dicSumActive = DAIHelper.Instance.GetDataLogForEntityBetweenTime(node.SumActiveKey, timeStart, timeEnd);
                Dictionary<DateTime, double> dicSumReactive = DAIHelper.Instance.GetDataLogForEntityBetweenTime(node.SumReactiveKey, timeStart, timeEnd);
                Dictionary<DateTime, double> dicNeSumActive = DAIHelper.Instance.GetDataLogForEntityBetweenTime(node.NeSumActiveKey, timeStart, timeEnd);
                Dictionary<DateTime, double> dicNeSumReactive = DAIHelper.Instance.GetDataLogForEntityBetweenTime(node.NeSumReactiveKey, timeStart, timeEnd); 

                foreach (DateTime timeIndex in timeList)
                {
                    DateTime pre_time = timeIndex - timeStep;
                    DataRow row = dtResult.NewRow();
                    row["datetime"] = timeIndex;
                    row["asset"] = nodePath;
                    string value = GetPower(dicSumActive, timeIndex, pre_time);
                    if (value != "")
                    {
                        row["15SumActive"] = GetPower(dicSumActive, timeIndex, pre_time);
                        row["15NeSumActive"] = GetPower(dicNeSumActive, timeIndex, pre_time);
                        row["15SumReactive"] = GetPower(dicSumReactive, timeIndex, pre_time);
                        row["15NeSumReactive"] = GetPower(dicNeSumReactive, timeIndex, pre_time);
                        row["SumActive"] = dicSumActive.ContainsKey(timeIndex) ? dicSumActive[timeIndex].ToString() : "";
                        row["NeSumActive"] = dicNeSumActive.ContainsKey(timeIndex) ? dicNeSumActive[timeIndex].ToString() : "";
                        dtResult.Rows.Add(row);
                        Console.WriteLine(dtResult.Rows.Count);
                    }
                }
            }

            if(dtResult != null)
                FillDataGridWithDataTable(dataGrid1, dtResult);
        }

        private string GetPower(Dictionary<DateTime, double> dictData, DateTime endTime, DateTime startTime)
        {
            string ret = "";
            if (dictData.ContainsKey(endTime) && dictData.ContainsKey(startTime))
            {
                if (dictData[endTime] >= dictData[startTime])
                    ret = (dictData[endTime] - dictData[startTime]).ToString();
            }
            /*catch(Exception ex)
            {
                DebugUtil.Instance.LOG.Error("Cannot get power data for " + endTime.ToShortTimeString());
            }*/

            return ret;
        }

        public void SetDevice(object device)
        {
            //this.deviceName = deviceName;
            nodeKey = device.ToString();
            nodePath = GetNodePathFromNodeKey(nodeKey);
            txtDevice.Text = "当前选择设备: " + nodePath;
        }
    }
}
