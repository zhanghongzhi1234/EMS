using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Visifire.Charts;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System.Windows.Media.Animation;


namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for Template.xaml
    /// </summary>
    public partial class GZL6_page25_query_energy : Window, IDevice
    {
        public static readonly log4net.ILog log = DebugUtil.Instance.LOG;

        private Dictionary<string, Server> serverMap = new Dictionary<string, Server>();
        private Dictionary<string, DataSource> dataSourceMap = new Dictionary<string, DataSource>();
        private Dictionary<string, ChartData> chartDataMap = new Dictionary<string, ChartData>();
        //private List<TextData> textDataList = new List<TextData>();         //TextBox binding
        private Dictionary<string, string> variableMap = new Dictionary<string, string>();      //Variable mapping
        private Dictionary<string, string> mappingMap = new Dictionary<string, string>();
        private Dictionary<string, TriggerData> triggerMap = new Dictionary<string, TriggerData>();

        DispatcherTimer timerRefresh;
        private int refreshInterval = 3000;

        DispatcherTimer timerAutoSwitch;
        private int autoSwtichInterval = 6000;

        private TextBox currentEditableTB = null;
        private NavWindow navWindow = null;
        private string currentFileName;
        public GZL6_page25_query_energy(NavWindow navWindow, string currentFileName)
        {
            InitializeComponent();
            this.navWindow = navWindow;
            this.currentFileName = currentFileName.Split('.')[1];

            try
            {
                ParseTemplate(this.currentFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                System.Environment.Exit(0);
            }

            this.Loaded += new RoutedEventHandler(Window1_Loaded);
            //too complex to use it now
            //Start the timerPoll
            /*timerRefresh = new DispatcherTimer();
            timerRefresh.Interval = TimeSpan.FromMilliseconds(refreshInterval);
            timerRefresh.Tick += new EventHandler(timerRefresh_Tick);
            timerRefresh.Start();

            //Start the timerAutoSwitch
            timerAutoSwitch = new DispatcherTimer();
            timerAutoSwitch.Interval = TimeSpan.FromMilliseconds(autoSwtichInterval);
            timerAutoSwitch.Tick += new EventHandler(timerAutoSwitch_Tick);
            timerAutoSwitch.Start();*/
            foreach (Button child in FindVisualChildren<Button>(Canvas))
            {
                if (child.Name == "btnExport")
                {
                    //child.Click += new RoutedEventHandler(btnExport_Click);
                    child.Click += btnExport_Click;
                    child.MouseEnter += menuButton_MouseEnter;
                    child.MouseLeave += menuButton_MouseLeave;
                }
                else if (child.Name == "btnExit")
                {
                    child.Click += btnExit_Click;
                    child.MouseEnter += menuButton_MouseEnter;
                    child.MouseLeave += menuButton_MouseLeave;
                }
                else if (child.Name == "btnNavigation")
                    child.Click += btnNavigation_Click;
            }
            foreach (var item in triggerMap)
            {
                object UIElement = this.FindName(item.Key);
                if (UIElement is Button)
                {
                    Button btnQuery = UIElement as Button;
                    btnQuery.Click += btnQuery_Click;
                }
            }
            InitScript();
            this.Closed += Window_Closed_1;
        }

        private bool ParseTemplate(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            //string thisFile = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
            //string defFile = thisFile.Replace(".xaml.cs", ".xml.def");
            //string defFile = fileName.Replace(".xaml.cs", ".xml.def");
            string defFile = fileName + ".xml.def";
            string variableResourceName = "TemplateProject.Template." + System.IO.Path.GetFileName(defFile);     //Namespace + folder + filename

            //var variableResourceName = "TemplateProject.Template.xml";          //use embedded resource
            Stream streamVariable = assembly.GetManifestResourceStream(variableResourceName);
            if (streamVariable == null)
                return false;
            XmlDocument xmlDocVariable = new XmlDocument();
            xmlDocVariable.Load(streamVariable);

            /*var schemaResourceName = "TemplateProject.Template.xaml";          //use embedded resource
            Stream streamSchema = assembly.GetManifestResourceStream(schemaResourceName);
            XmlDocument xmlDocSchema = new XmlDocument();
            xmlDocSchema.Load(streamSchema);*/

            //Parse Server
            XmlNodeList nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/Server");
            foreach (XmlNode childNode in nodeList)
            {
                Server server = null;
                string name = "";
                if (childNode.Attributes["Type"].Value == "MYSQL")
                {
                    name = childNode.Attributes["Name"].Value;
                    string IP = childNode.Attributes["IP"].Value;
                    string username = childNode.Attributes["UserName"].Value;
                    string password = childNode.Attributes["Password"] != null ? childNode.Attributes["Password"].Value : "";
                    string databaseName = childNode.Attributes["DatabaseName"].Value;
                    server = new MysqlServer(name, IP, username, password, databaseName);
                    serverMap[name] = server;
                }
                else if (childNode.Attributes["Type"].Value == "TCPIP")
                {
                    name = childNode.Attributes["Name"].Value;
                    string IP = childNode.Attributes["IP"].Value;
                    string port = childNode.Attributes["Port"].Value;
                    server = new TCPIPServer(name, IP, port);

                }
                else if (childNode.Attributes["Type"].Value == "SCADA")
                {
                    name = childNode.Attributes["Name"].Value;
                    server = new ScadaServer(name);
                }
                else if (childNode.Attributes["Type"].Value == "SQLITE")
                {
                    name = childNode.Attributes["Name"].Value;
                    string databaseName = childNode.Attributes["DatabaseName"].Value;
                    server = new SQLiteServer(name, databaseName);
                    serverMap[name] = server;
                }

                if (server != null)
                {
                    server.Init();
                    serverMap[name] = server;
                }
            }

            //Parse DataSource
            nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/DataSource");
            foreach (XmlNode childNode in nodeList)
            {
                string name = childNode.Attributes["Name"].Value;
                string serverName = childNode.Attributes["Server"] != null ? childNode.Attributes["Server"].Value : null;
                Server server = null;
                if (serverName != null)
                {
                    if (serverMap.ContainsKey(serverName) == false)
                    {
                        log.Info("Error: Cannot find Server:" + serverName + " for DataSource:" + name);
                        continue;
                    }
                    server = serverMap[serverName];
                }
                string Channel = childNode.Attributes["Channel"] != null ? childNode.Attributes["Channel"].Value : null;
                string DataType = childNode.Attributes["DataType"] != null ? childNode.Attributes["DataType"].Value : "Static";
                string ChartType = childNode.Attributes["ChartType"] != null ? childNode.Attributes["ChartType"].Value : null;
                string LegendText = childNode.Attributes["LegendText"] != null ? childNode.Attributes["LegendText"].Value : null;
                string color = childNode.Attributes["Color"] != null ? childNode.Attributes["Color"].Value : null;
                string LineThickness = childNode.Attributes["LineThickness"] != null ? childNode.Attributes["LineThickness"].Value : null;
                string LabelEnabled = childNode.Attributes["LabelEnabled"] != null ? childNode.Attributes["LabelEnabled"].Value : null;
                string LabelFormat = childNode.Attributes["LabelFormat"] != null ? childNode.Attributes["LabelFormat"].Value : null;
                string LabelStyle = childNode.Attributes["LabelStyle"] != null ? childNode.Attributes["LabelStyle"].Value : null;
                string LabelSuffix = childNode.Attributes["LabelSuffix"] != null ? childNode.Attributes["LabelSuffix"].Value : null;
                string Exploded = childNode.Attributes["Exploded"] != null ? childNode.Attributes["Exploded"].Value : null;
                string AxisYType = childNode.Attributes["AxisYType"] != null ? childNode.Attributes["AxisYType"].Value : null;
                string MarkerType = childNode.Attributes["MarkerType"] != null ? childNode.Attributes["MarkerType"].Value : null;
                string MarkerEnabled = childNode.Attributes["MarkerEnabled"] != null ? childNode.Attributes["MarkerEnabled"].Value : null;
                string XValueType = childNode.Attributes["XValueType"] != null ? childNode.Attributes["XValueType"].Value : null;
                string ShowInLegend = childNode.Attributes["ShowInLegend"] != null ? childNode.Attributes["ShowInLegend"].Value : null;
                DataSource dataSource = new DataSource(name, serverName, Channel, DataType, ChartType, LegendText, color, LineThickness, LabelEnabled, LabelFormat, LabelStyle, LabelSuffix, Exploded, AxisYType, MarkerType, MarkerEnabled, XValueType, ShowInLegend);
                dataSource.SetServer(server);
                //if (ChartType != null)
                //    dataSource.Reload(data);        //Load Data for Datasource, only for Chart
                dataSourceMap[name] = dataSource;
            }

            //Parse Variable
            nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/Variable");
            foreach (XmlNode childNode in nodeList)
            {
                variableMap[childNode.Attributes["Name"].Value] = childNode.Attributes["Source"].Value;
            }

            //Parse Mapping
            nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/Mapping");
            foreach (XmlNode childNode in nodeList)
            {
                mappingMap[childNode.Attributes["UIElement"].Value] = childNode.Attributes["DataSource"].Value;
            }

            //Parse Trigger
            nodeList = xmlDocVariable.SelectNodes("/Root/ItemGroup/Trigger");
            foreach (XmlNode childNode in nodeList)
            {
                //TriggerData triggerData = new TriggerData(childNode.Attributes["Button"].Value, childNode.Attributes["DataSource"].Value, childNode.Attributes["Target"].Value);
                TriggerData triggerData = new TriggerData(childNode.Attributes["Button"].Value, childNode.Attributes["DataSource"].Value);
                triggerMap[childNode.Attributes["Button"].Value] = triggerData;
            }

            //Parse Chart from schema
            /** Chart1 will autoswitch bewteen 2 datasource, but Chart2 will show several data source together
             *  <Mapping UIElement="Chart1" DataSource="Data1"/>
             *  <Mapping UIElement="Chart1" DataSource="Data2"/>
                <Mapping UIElement="Chart2" DataSource="Rule3,Rule4,Rule5,Rule6,Rule7"/>
             **/
            //XmlNodeList nodeList = xmlDocVariable.SelectNodes("/Canvas/Chart");
            //nodeList = xmlDocSchema.GetElementsByTagName("charts:Chart");
            //foreach (XmlNode childNode in nodeList)
            //foreach (Chart chart in FindVisualChildren<Chart>(Canvas))
            foreach (Chart chart in FindLogicalChildren<Chart>(Canvas))
            {
                if (mappingMap.ContainsKey(chart.Name))     //Check Chart DataSource
                {
                    ChartData chartData = new ChartData();
                    chartData.name = chart.Name;
                    //chartData.FreeText = childNode.Attributes["FreeText"] != null ? childNode.Attributes["FreeText"].Value : null;
                    string dataSourceArray = mappingMap[chartData.name];
                    if (dataSourceArray != null && dataSourceArray.Length > 0)
                    {
                        chartData.dataSourceList.AddRange(dataSourceArray.Split(','));
                        chartDataMap[chartData.name] = chartData;
                        SetChart(chartData.name, chartData);
                    }
                }
            }

            //Parse TextBlock
            //XmlNodeList nodeList = xmlDocVariable.SelectNodes("/Canvas/Chart");
            /*nodeList = xmlDocVariable.GetElementsByTagName("TextBlock");
            foreach (XmlNode childNode in nodeList)
            {
                if (childNode.Attributes["DataSource"] != null)
                {
                    string name = childNode.Attributes["Name"].Value;
                    string dataSourceName = childNode.Attributes["DataSource"].Value;
                    TextData textData = new TextData(name, dataSourceName);
                    textDataList.Add(textData);
                }
            }*/

            return true;
        }

        void SetChart(string chartName, ChartData chartData)
        {
            Chart chart = (Chart)this.FindName(chartName);

            //test code
            /*            DataSeries dataSeries = new DataSeries();
                        DataPoint dataPoint = new DataPoint();
                        dataPoint.XValue = 1;
                        dataPoint.AxisXLabel = "照明";
                        dataPoint.YValue = 50;
                        dataSeries.DataPoints.Add(dataPoint);
                        dataPoint = new DataPoint();
                        dataPoint.XValue = 2;
                        dataPoint.AxisXLabel = "电力";
                        dataPoint.YValue = 60;
                        dataSeries.DataPoints.Add(dataPoint);
                        dataPoint = new DataPoint();
                        dataPoint.XValue = 3;
                        dataPoint.AxisXLabel = "动力";
                        dataPoint.YValue = 40;
                        dataSeries.DataPoints.Add(dataPoint);
                        dataSeries.RenderAs = RenderAs.Pie;
                        Chart1.Series.Add(dataSeries);
                        return;*/
            // test code end

            chart.IsHitTestVisible = false;     //Don't accept mouse event when set chart data
            chart.Series.Clear();
            try
            {
                foreach (string dataSourceName in chartData.dataSourceList)
                {
                    if (dataSourceName == "Data_SCADA")
                    {
                        DataSource dataSource = dataSourceMap[dataSourceName];
                        ScadaServer scadaServer = dataSource.server as ScadaServer;
                        if (scadaServer != null)
                        {
                            scadaServer.SetObserver(chart, dataSource.Channel);
                            chart.Series.Add(dataSource.dataSeries);
                        }
                    }
                    else
                    {
                        DataSource dataSource = dataSourceMap[dataSourceName];
                        chart.Series.Add(dataSource.dataSeries);
                    }
                }
                /*if (chartData.FreeText == "true")
                {
                    chart.MouseLeftButtonDown -= new MouseButtonEventHandler(mouseLeftBtnDown);
                    chart.MouseLeftButtonDown += new MouseButtonEventHandler(mouseLeftBtnDown);
                }*/
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
            chart.IsHitTestVisible = true;          //resume mouse event
        }

        //loadAll = true, reload all data, loadAll = false, reload dynamic data only
        private void Reload(bool loadAll = false)
        {
            foreach (KeyValuePair<string, DataSource> entry in dataSourceMap)
            {
                if (loadAll == true || (loadAll == false && entry.Value.DataType == "Dynamic"))
                {   //for chart
                    entry.Value.ReloadChartData();
                }
            }
            //for other control
            foreach (var item in mappingMap)
            {
                object UIElement = this.FindName(item.Key);
                if (!(UIElement is Chart))      //Chart maybe have many dataSource in item.value, it will crash use dataSourceMap[item.Value
                {
                    DataSource dataSource = dataSourceMap[item.Value];
                    if (UIElement is TextBlock)
                    {   //TextBlock will always be dynamic
                        TextBlock textBlock = UIElement as TextBlock;
                        textBlock.Text = dataSource.GetSingleData();
                    }
                    else if (UIElement is ListView && loadAll == true)
                    {
                        ListView listView1 = UIElement as ListView;
                        DataTable dt = dataSource.GetData();
                        FillListViewWithDataTable(listView1, dt);
                    }
                    else if (UIElement is DataGrid && loadAll == true)
                    {
                        DataGrid dataGrid1 = UIElement as DataGrid;
                        DataTable dt = dataSource.GetData();
                        FillDataGridWithDataTable(dataGrid1, dt);
                    }
                    else if (UIElement is TreeView && loadAll == true)
                    {
                        TreeView treeView1 = UIElement as TreeView;
                        //DataTable dt = dataSource.GetData();              //to be done
                        //FillTreeViewWithDataTable(treeView1, dt);
                    }
                    else if (UIElement is ComboBox && loadAll == true)
                    {
                        ComboBox comboBox = UIElement as ComboBox;
                        List<string> list = dataSource.GetSingularData();
                        foreach (string str in list)
                        {
                            comboBox.Items.Add(str);
                        }
                        comboBox.SelectedIndex = 0;
                    }
                }

            }
        }

        void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            Reload(true);
            LoadScript();
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            Reload(false);
        }

        private void timerAutoSwitch_Tick(object sender, EventArgs e)
        {
            /*foreach (KeyValuePair<string, ChartDataCollection> entry in chartDataMap)
            {
                ChartDataCollection chartDataCollection = entry.Value;
                if (chartDataCollection.chartDataList.Count() > 1)
                {
                    ChartData chartData = chartDataCollection.GetNextChartDataCycle();
                    SetChart(entry.Key, chartData);
                }
            }*/
        }

        private void mouseLeftBtnDown(object sender, MouseButtonEventArgs e)
        {
            if (currentEditableTB == null)
            {
                Point p = e.GetPosition(Canvas);
                currentEditableTB = new TextBox();
                currentEditableTB.Margin = new Thickness(p.X, p.Y, Canvas.ActualWidth - 100 - p.X, Canvas.ActualHeight - p.Y - 20);
                Canvas.Children.Add(currentEditableTB);
            }
            else
            {
                var textBlock = new TextBlock();
                textBlock.Text = currentEditableTB.Text;
                textBlock.Margin = currentEditableTB.Margin;
                Canvas.Children.Remove(currentEditableTB);
                currentEditableTB = null;
                Canvas.Children.Add(textBlock);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void menuButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Button btn = (Button)sender;
            btn.Foreground = Brushes.Black;
        }

        private void menuButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Button btn = (Button)sender;
            btn.Foreground = Brushes.White;
        }

        private double GetRandom(double minimum, double maximum, Random rnd = null)
        {
            double ret = minimum;
            double range = maximum - minimum;
            int rangeMin = 1000;
            if (rnd == null)
                rnd = new Random();
            if (range == 0d)
            {
                ret = minimum;
            }
            else if (range < rangeMin)
            {
                double zoom = rangeMin / range;
                int temp = rnd.Next(0, rangeMin);
                ret = minimum + temp / zoom;
            }
            if (range <= 1 && range > 0)
                ret = Math.Round(ret, 2);
            else
                ret = Math.Round(ret, 0);
            return ret;
        }

        public static IEnumerable<T> FindLogicalChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                if (obj is T)
                    yield return obj as T;

                foreach (DependencyObject child in LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>())
                    foreach (T c in FindLogicalChildren<T>(child))
                        yield return c;
            }
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj, string tag) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is FrameworkElement)
                    {
                        FrameworkElement el = child as FrameworkElement;
                        if (el.Tag != null && el.Tag.ToString() == tag)
                            yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child, tag))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            string fileName = @".\new.xlsx";
            if (this.Title != "")
                fileName = this.Title + ".xlsx";
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = fileName; // Default file name
            dlg.DefaultExt = ".xlsx"; // Default file extension
            dlg.Filter = "Excel documents (.xlsx)|*.xlsx"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {   // Save document
                fileName = dlg.FileName;
            }

            FileInfo newFile = new FileInfo(fileName);
            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(fileName);
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Export");
                ws.Cells.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                Tuple<int, int> blankRowRange = new Tuple<int, int>(1, 100);
                Tuple<int, int> blankColRange = new Tuple<int, int>(1, 100);

                var TransformToPixelX = new Func<double, int>(unitX =>
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                    {
                        return (int)((g.DpiX / 96) * unitX);
                    }
                });

                var TransformToPixelY = new Func<double, int>(unitY =>
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                    {
                        return (int)((g.DpiY / 96) * unitY);
                    }
                });

                string testString = "0000";
                FormattedText formattedText = new FormattedText(testString,
                    System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    10,
                    Brushes.Black);

                double perCellPointWidthInInch = (formattedText.Width / testString.Length) / 96;

                foreach (DataGrid child in FindVisualChildren<DataGrid>(Canvas))
                {
                    Point pos = child.PointToScreen(new Point(0, 0));
                    /*GridView gView = child.View as GridView;
                    if (gView == null)
                        continue;*/

                    double top = 0, left = 0;
                    int beginRow = 1, beginCol = 1;
                    while (top / 72 < pos.Y / 96)
                    {
                        top += ws.Row(beginRow).Height;
                        ++beginRow;
                    }

                    ws.Row(beginRow - 1).Height -= (top / 72 - pos.Y / 96) * 72;
                    while (left * perCellPointWidthInInch < pos.X / 96)
                    {
                        left += ws.Column(beginCol).Width;
                        ++beginCol;
                    }
                    ws.Column(beginCol - 1).Width -= (left * perCellPointWidthInInch - pos.X / 96) / perCellPointWidthInInch;
                    int i = beginRow, j = beginCol;
                    if (i - 1 < blankRowRange.Item2)
                        blankRowRange = new Tuple<int, int>(blankRowRange.Item1, i - 1);

                    if (j - 1 < blankColRange.Item2)
                        blankColRange = new Tuple<int, int>(blankColRange.Item1, j - 1);

                    //foreach (GridViewColumn col in gView.Columns)
                    foreach (DataGridColumn col in child.Columns)
                    {
                        ws.Cells[i, j].Value = col.Header as string;
                        ws.Column(j).Width = col.ActualWidth / 96 / perCellPointWidthInInch;
                        ++j;
                    }
                    foreach (object obj in child.Items)
                    {
                        ++i;
                        j = beginCol;
                        var dictionary = (IDictionary<string, object>)obj;
                        object[] temp = dictionary.Values.ToArray();
                        int k = 0;
                        //foreach (GridViewColumn col in gView.Columns)
                        foreach (DataGridColumn col in child.Columns)
                        {
                            //Binding bd = col.DisplayMemberBinding as Binding;
                            //object val = obj.GetType().GetProperty(bd.Path.Path).GetValue(obj, null);         //Cannot use for ExpandoObject
                            //object val = (obj as IDictionary<string, object>)[bd.Path.Path];
                            object val = temp[k];
                            k++;
                            if (val != null)
                            {
                                ws.Cells[i, j++].Value = val;
                            }
                        }
                    }
                    ws.Cells[beginRow, beginCol, i, j].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                const int loadRowIndex = 200;
                int loadColIndex = 200;
                foreach (Chart child in FindVisualChildren<Chart>(Canvas))
                {
                    if (child.Series.Count == 0)
                        continue;

                    Point pos = child.PointToScreen(new Point(0, 0));
                    Vector szVec = child.PointToScreen(new Point(child.ActualWidth, child.ActualHeight)) - child.PointToScreen(new Point(0, 0));
                    Size sz = new Size(szVec.X, szVec.Y);

                    OfficeOpenXml.Drawing.Chart.eChartType chartType = 0;
                    if (child.Series[0].RenderAs == RenderAs.Pie)
                        chartType = OfficeOpenXml.Drawing.Chart.eChartType.Pie;
                    else if (child.Series[0].RenderAs == RenderAs.StackedColumn)
                        chartType = OfficeOpenXml.Drawing.Chart.eChartType.ColumnStacked;
                    else if (child.Series[0].RenderAs == RenderAs.Area)
                        chartType = OfficeOpenXml.Drawing.Chart.eChartType.Area;

                    var chartObj = ws.Drawings.AddChart(child.Name, chartType);
                    var pieChar = chartObj as ExcelPieChart;
                    if (pieChar != null)
                    {
                        pieChar.DataLabel.ShowPercent = true;
                    }

                    chartObj.SetPosition(TransformToPixelY(pos.Y), TransformToPixelX(pos.X));
                    chartObj.SetSize(TransformToPixelX(sz.Width), TransformToPixelY(sz.Height));
                    chartObj.Legend.Position = eLegendPosition.Bottom;
                    if (chartObj.From.Column - 1 < blankColRange.Item2)
                        blankColRange = new Tuple<int, int>(blankColRange.Item1, chartObj.From.Column - 1);
                    if (chartObj.From.Row - 1 < blankRowRange.Item2)
                        blankRowRange = new Tuple<int, int>(blankRowRange.Item1, chartObj.From.Row - 1);

                    var chartAxesX = child.AxesX[0];
                    foreach (DataSeries ds in child.Series)
                    {
                        List<double> values = new List<double>();
                        List<string> xvalues = new List<string>();
                        foreach (DataPoint dp in ds.DataPoints)
                        {
                            values.Add(dp.YValue);
                            xvalues.Add(dp.XValue.ToString() + chartAxesX.Suffix);
                        }

                        if (values.Count == 0)
                            continue;

                        ws.Cells[loadRowIndex, loadColIndex].LoadFromCollection(values);
                        ws.Cells[loadRowIndex, loadColIndex + 1].LoadFromCollection(xvalues);
                        var s = chartObj.Series.Add(ExcelRange.GetAddress(loadRowIndex, loadColIndex, values.Count() + loadRowIndex - 1, loadColIndex), ExcelRange.GetAddress(loadRowIndex, loadColIndex + 1, values.Count() + loadRowIndex - 1, loadColIndex + 1));
                        loadColIndex += 2;

                        s.Header = ds.LegendText;
                        if (ds.Color == null)
                            continue;

                        byte r = ((SolidColorBrush)ds.Color).Color.R;
                        byte g = ((SolidColorBrush)ds.Color).Color.G;
                        byte b = ((SolidColorBrush)ds.Color).Color.B;
                        s.Fill.Color = System.Drawing.Color.FromArgb((int)r, (int)g, (int)b);
                    }
                }

                foreach (int i in Enumerable.Range(blankRowRange.Item1, blankRowRange.Item2))
                    ws.Row(i).Hidden = true;

                foreach (int i in Enumerable.Range(blankColRange.Item1, blankColRange.Item2))
                    ws.Column(i).Hidden = true;

                package.SaveAs(newFile);
            }
        }

        private List<string> ParseRule(string rule)
        {   //get all variable name from rule
            List<string> ret = new List<string>();
            string pattern = @"\?([^)]*?)\?";
            foreach (Match match in Regex.Matches(rule, pattern))
            {
                ret.Add(match.Value);
            }
            return ret;
        }

        private void FillListViewWithDataTable(ListView lstView, DataTable dt)
        {
            lstView.Items.Clear();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dynamic item = new ExpandoObject();
                var dictionary = (IDictionary<string, object>)item;
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    string columnName = dt.Columns[j].ColumnName;
                    dictionary[columnName] = dt.Rows[i][j];
                }
                lstView.Items.Add(item);
            }
        }

        private void FillDataGridWithDataTable(DataGrid dataGrid, DataTable dt)
        {
            /*dataGrid.Items.Clear();
            foreach (DataColumn column in dt.Columns)
            {
                int width = (int)dataGrid1.ActualWidth / dt.Columns.Count;
                dataGrid1.Columns.Add(CreateLabelColumn(column.ColumnName, width, column.ColumnName));
            }*/
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dynamic item = new ExpandoObject();
                var dictionary = (IDictionary<string, object>)item;
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    string columnName = dt.Columns[j].ColumnName;
                    dictionary[columnName] = dt.Rows[i][j];
                }
                dataGrid.Items.Add(item);
            }
        }

        private void btnNavigation_Click(object sender, RoutedEventArgs e)
        {
            navWindow.AnimationShow();
        }

        private void item_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            {
                Cursor cursor = CursorHelper.CreateCursor(e.Source as UIElement);
                e.UseDefaultCursors = false;
                Mouse.SetCursor(cursor);
            }

            e.Handled = true;
        }

        private Chart GetNearestChart(FrameworkElement fe)
        {
            Chart ret = null;
            FrameworkElement currentNode = fe;
            bool bFound = false;
            while (bFound == false)
            {
                DependencyObject dep = currentNode.Parent;
                if (dep != null && dep is FrameworkElement)
                {
                    FrameworkElement parentNode = dep as FrameworkElement;
                    IEnumerable<Chart> charts = FindVisualChildren<Chart>(parentNode);
                    if (charts.Count() >= 1)
                    {
                        ret = charts.FirstOrDefault();
                        bFound = true;
                    }
                    else
                        currentNode = parentNode;
                }
                else
                    break;
            }
            return ret;
        }

        private void textBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock txt = (TextBlock)sender;
            txt.Foreground = Brushes.Lime;
        }

        private void textBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock txt = (TextBlock)sender;
            txt.Foreground = Brushes.White;
        }

        private void textBlockLink_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBlock txt = (TextBlock)sender;
            txt.TextDecorations = TextDecorations.Underline;
            txt.Foreground = Brushes.Green;
            this.Cursor = Cursors.Hand;
        }

        private void textBlockLink_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBlock txt = (TextBlock)sender;
            txt.TextDecorations = null;
            txt.Foreground = Brushes.White;
            this.Cursor = Cursors.Arrow;
        }

        private void Window_Closed_1(object sender, EventArgs e)
        {
            foreach (KeyValuePair<string, Server> pair in this.serverMap)
            {
                pair.Value.Close();
            }
        }

    }
}
