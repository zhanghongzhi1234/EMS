using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Visifire.Charts;

namespace TemplateProject
{
    public class DataSource
    {
        public string name;
        public string serverName;
        public string Channel;
        public string DataType;         //Static or Dynamic, currently refresh timer in .xaml.cs not start, so dynamic have no use now, this parameter is not used
        public string ChartType;    //Pie, Line, Bar, Column
        public string LegendText;
        public string color;
        public string LineThickness;
        public string LabelEnabled; //true or false
        public string LabelFormat; //self defined property, include XValue and YValue, such as XValue-YVAlue%
        public string LabelStyle;
        public string LabelSuffix;
        public string Exploded;     //true or false
        public string AxisYType;    //Primary or Secondary
        public string MarkerType; //Circle, Square, Triangle, Cross, Diamond, Line = 5
        public string MarkerEnabled;
        public string XValueType;   //Auto, Numeric, Date, DateTime, Time
        public string ShowInLegend; //if show legend
        public Server server;
        public DataSeries dataSeries;

        public DataSource()
        {
        }

        public DataSource(string name, string serverName, string Channel, string DataType, string ChartType,
            string LegendText, string color, string LineThickness, string LabelEnabled, string LabelFormat, string LabelStyle, string LabelSuffix,
            string Exploded, string AxisYType, string MarkerType, string MarkerEnabled, string XValueType, string ShowInLegend)
        {
            this.name = name;
            this.serverName = serverName;
            this.Channel = Channel;
            this.DataType = DataType;
            this.ChartType = ChartType;
            this.LegendText = LegendText;
            this.color = color;
            this.LineThickness = LineThickness;
            this.LabelEnabled = LabelEnabled;
            this.LabelFormat = LabelFormat;
            this.LabelStyle = LabelStyle;
            this.LabelSuffix = LabelSuffix;
            this.Exploded = Exploded;
            this.AxisYType = AxisYType;
            this.MarkerType = MarkerType;
            this.MarkerEnabled = MarkerEnabled;
            this.XValueType = XValueType;
            this.ShowInLegend = ShowInLegend;

            CreateDataSeries();
        }

        public void CreateDataSeries()
        {
            dataSeries = new DataSeries();
            if (LineThickness != null && LineThickness != "")
                dataSeries.LineThickness = Convert.ToDouble(LineThickness);
            if (color != null && color != "")
                dataSeries.Color = new BrushConverter().ConvertFromString(color) as SolidColorBrush;     //new SolidBrush(Color.FromName(color));        not sure it is ok for hex
            if (ChartType != null && ChartType != "")
                dataSeries.RenderAs = (RenderAs)Enum.Parse(typeof(RenderAs), ChartType);
            if (LegendText != null && LegendText != "")
                dataSeries.LegendText = LegendText;
            if (AxisYType == "Secondary")
                dataSeries.AxisYType = AxisTypes.Secondary;
            else
                dataSeries.AxisYType = AxisTypes.Primary;
        }

        public void SetServer(Server server)
        {
            this.server = server;
        }

        public DataPoint AddDataPoint(RawTable rawTable)
        {
            return AddDataPoint(rawTable.XValue, rawTable.YValue, rawTable.LabelText, rawTable.Color, rawTable.Tag);
        }

        public DataPoint AddDataPoint(object XValue, double YValue, string LabelText, Brush color = null, object Tag = null)
        {
            DataPoint dataPoint = new DataPoint();
            try
            {
                dataPoint.XValue = XValue;
            }
            catch (Exception)
            {
                dataPoint.AxisXLabel = XValue.ToString();
            }
            if (!string.IsNullOrEmpty(LabelText))
                dataPoint.LabelText = LabelText;
            if (color != null)
                dataPoint.Color = color;

            dataPoint.YValue = YValue;
            dataPoint.Tag = Tag;
            if (LabelEnabled != null)
            {
                dataPoint.LabelEnabled = Convert.ToBoolean(LabelEnabled);
                if (LabelStyle != null)
                    dataPoint.LabelStyle = (LabelStyles)Enum.Parse(typeof(LabelStyles), LabelStyle);
                else
                    dataPoint.LabelStyle = LabelStyles.OutSide;
                /*if (LabelFormat != null)      //if set LabelText here, it won't update when datapoint changed. become static
                    dataPoint.LabelText = LabelFormat.Replace("XValue", dataPoint.XValue.ToString()).Replace("YValue", dataPoint.YValue.ToString());
                else
                    dataPoint.LabelText = dataPoint.YValue.ToString() + LabelSuffix;*/
            }
            if (Exploded != null)
                dataPoint.Exploded = Convert.ToBoolean(Exploded);
            if (MarkerEnabled != null)
                dataPoint.MarkerEnabled = Convert.ToBoolean(MarkerEnabled);
            if (MarkerType != null)
                dataPoint.MarkerType = (Visifire.Commons.MarkerTypes)Enum.Parse(typeof(Visifire.Commons.MarkerTypes), MarkerType);

            //dataPoint.MarkerEnabled = false;
            dataSeries.DataPoints.Add(dataPoint);
            return dataPoint;
        }

        public void RemoveDataPoint(int index)
        {
            if (index < dataSeries.DataPoints.Count)
            {
                dataSeries.DataPoints.RemoveAt(index);
            }
        }

        //if data is not null, just use the data as datasource, or load from DataSource, this function will clear all current dataSeries
        public void ReloadChartData(List<RawTable> data = null)
        {
            if (ChartType != null)
            {
                if (data == null && Channel != null && isRule(Channel) == false)
                {
                    data = GetRawData(Channel);
                }
                
                if(data != null)
                {
                    dataSeries.DataPoints.Clear();
                    foreach (RawTable item in data)
                    {
                        AddDataPoint(item);
                    }
                    if(ShowInLegend != null)
                        dataSeries.ShowInLegend = Convert.ToBoolean(ShowInLegend);
                }
                /*else if(server != null && server.serverType == ServerType.SCADA)
                {
                    ScadaServer scadaServer = server as ScadaServer;
                    string[] subsystems = {"ECS", "Lighting", "Rolling", "Traction", "MMS", "Rail"};
                    for (int i = 0; i < scadaServer.dpNameList.Count(); i++)
                    {
                        DataPoint datapoint = new DataPoint();
                        datapoint.AxisXLabel = subsystems[i];
                        datapoint.YValue = 10;
                        dataSeries.DataPoints.Add(datapoint);
                    }
                }*/

                if (XValueType != null)
                    dataSeries.XValueType = (Visifire.Charts.ChartValueTypes)Enum.Parse(typeof(Visifire.Charts.ChartValueTypes), XValueType);
            }
        }

        private List<RawTable> GetRawData(string table)
        {
            return server != null ? server.GetRawData(table) : null;
        }

        public List<RawTable> GetRawData()
        {
            return GetRawData(Channel);
        }

        public string GetSingleData()
        {
            string ret = null;
            List<RawTable> rawTable = server.GetRawData(Channel);
            if (rawTable != null && rawTable.Count() >= 1)
                ret = rawTable[0].YValue.ToString();
            return ret;
        }

        //if table is sql, no problem, if table is rule, need replace variable first
        public DataTable GetQueryData(string sqlstr)
        {
            DataTable ret = server.GetQueryData(sqlstr);
            return ret;
        }

        //if table is sql, no problem, if table is rule, need replace variable first
        public DataTable GetData()
        {
            string sqlstr = Channel;
            if (!Channel.ToLower().Contains("select"))
            {
                sqlstr = "select * from " + Channel;
            }
            DataTable ret = server.GetQueryData(sqlstr);
            return ret;
        }

        public List<string> GetSingularData()
        {
            List<string> ret = new List<string>();
            string sqlstr = Channel;
            if (!Channel.ToLower().Contains("select"))
            {
                sqlstr = "select * from " + Channel;
            }
            DataTable dt = server.GetQueryData(sqlstr);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //DataRow dr = dt.Rows[i][0].ToString();
                //dr[0].ToString();
                ret.Add(dt.Rows[i][0].ToString());
            }
            return ret;
        }

        public bool isRule(string table)
        {
            if (table == null)
                return false;
            return table.Contains('?');
        }

        //if data is not null, just use the data as datasource, or load from DataSource
        public void ReloadChartData(DataTable dt)
        {
            if (dt == null && dt.Columns.Count < 2)
                return;
            try
            {
                List<RawTable> data = new List<RawTable>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    object XValue = dt.Rows[i][0].ToString();
                    double YValue = Convert.ToDouble(dt.Rows[i][1].ToString());
                    RawTable rawTable = new RawTable(XValue, YValue);
                    data.Add(rawTable);
                }
                ReloadChartData(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
