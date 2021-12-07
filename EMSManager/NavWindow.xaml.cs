using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for NavWindow.xaml
    /// </summary>
    public partial class NavWindow : Window
    {
        public Dictionary<string, string> dicTemplate = new Dictionary<string, string>();
        public Dictionary<string, Window> dicWindow = new Dictionary<string, Window>();
        public Dictionary<string, IDevice> dicDevice = new Dictionary<string, IDevice>();
        public DeviceWindow deviceWindow;       //device list

        //Window currentWindow;
        public string currentWindow;
        public bool isThereStartWindow = false;
        public static IntPtr WindowHandle { get; private set; }
        public bool topmost = true;

        public NavWindow(string[] args = null)
        {
            InitializeComponent();
            DebugUtil.Instance.LOG.Debug("NavWindow Start");
            var proc = Process.GetCurrentProcess();
            if (proc.ProcessName.Contains(".vshost"))
                topmost = false;
            this.Loaded += (s, e) =>
            {
                WindowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                HwndSource.FromHwnd(WindowHandle).AddHook(new HwndSourceHook(HandleMessages));
            };
            CachedMap.Instance.LoadConfigFromDB();
            dicTemplate["实时电能趋势图"] = "TemplateProject.GZL6_page18_jinxian_realtime";
            dicTemplate["能耗数据查询"] = "TemplateProject.GZL6_page25_query_energy";
            dicTemplate["全线电能汇总"] = "TemplateProject.GZL6_page27_fullline_power";
            dicTemplate["日分类能耗统计"] = "TemplateProject.GZL6_page28_day_power";
            dicTemplate["月分类能耗统计"] = "TemplateProject.GZL6_page29_month_power";
            dicTemplate["年分类能耗统计"] = "TemplateProject.GZL6_page30_year_power";
            foreach (KeyValuePair<string, string> pair in dicTemplate)
            {
                Type t = Type.GetType(pair.Value);
                Window win = (Window)Activator.CreateInstance(t, this, pair.Value);
                win.Topmost = topmost;
                dicWindow[pair.Key] = win;
                dicDevice[pair.Key] = win as IDevice;
            }
            DebugUtil.Instance.LOG.Debug("Create all template successfully");
            if (args != null)
            {
                DebugUtil.Instance.LOG.Debug("Handle parameter passed from launch parameter");
                HandleParameter(args);
            }

            if (isThereStartWindow == false)
            {
                DebugUtil.Instance.LOG.Debug("Start the first template");
                KeyValuePair<string, Window> startPair = dicWindow.First();
                startPair.Value.Show();
                currentWindow = startPair.Key;
                this.Hide();
                isThereStartWindow = true;
            }

            this.Left = -this.Width;
            int i = 0;
            foreach (KeyValuePair<string, string> pair in dicTemplate)
            {
                ListViewItem item = new ListViewItem();// { Text = entry.Value };
                item.Content = pair.Key;
                item.Tag = pair.Key;
                if (i % 2 == 0)
                    item.Background = Brushes.Gray;
                else
                    item.Background = Brushes.DarkGray;
                item.Height = 40;
                list1.Items.Add(item);
                i++;
            }
            this.Topmost = true;
            deviceWindow = new DeviceWindow(this);

            DebugUtil.Instance.LOG.Debug("NavWindow End");
        }

        internal void HandleParameter(string[] args)
        {
            if (Application.Current.MainWindow is NavWindow)
            {
                NavWindow mainWindow = Application.Current.MainWindow as NavWindow;
                if (args != null && args.Length > 0 && args[0].IndexOf("EntityName") >= 0)
                {
                    string[] temp = args[0].Split('=');
                    if (temp.Count() > 0)
                    {
                        string deviceName = temp[1];
                        //ActivateAndSetDevice("GZL6_page18_jinxian_realtime", deviceName);
                        Activate("GZL6_page18_jinxian_realtime");
                        isThereStartWindow = true;
                    }
                }
            }
        }

        private IntPtr HandleMessages(IntPtr handle, int message, IntPtr wParameter, IntPtr lParameter, ref Boolean handled)
        {
            if (handle != WindowHandle)
                return IntPtr.Zero;

            var data = UnsafeNative.GetMessage(message, lParameter);

            if (data != null)
            {
                if (Application.Current.MainWindow == null)
                    return IntPtr.Zero;

                if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                    Application.Current.MainWindow.WindowState = WindowState.Normal;

                UnsafeNative.SetForegroundWindow(new WindowInteropHelper(Application.Current.MainWindow).Handle);

                var args = data.Split(' ');
                HandleParameter(args);
                handled = true;
            }

            return IntPtr.Zero;
        }

        public Window Activate(string windowName)
        {
            if (windowName == "")
                return null;
            Window win = dicWindow[windowName];
            if (currentWindow != windowName)
            {
                win.Show();
                if (currentWindow != null)
                    dicWindow[currentWindow].Hide();
                currentWindow = windowName;
            }
            return win;
        }

        public void SetDevice(string windowName, object device)
        {
            if (windowName == "")
                return;
            dicDevice[windowName].SetDevice(device);
        }

        public void ActivateAndSetDevice(string targetWindow, object device)
        {
            if (targetWindow == "")
                return;
            Activate(targetWindow);
            SetDevice(targetWindow, device);
        }

        //The reason not to handle the PreviewMouseLeftButtonDown event is that, by the time when you handle the event, the ListView's SelectedItem may still be null.
        private void stationList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //selectedStation = stationList.SelectedValue.ToString();
            ListViewItem item = (ListViewItem)list1.SelectedItem;
            string winName = item.Tag.ToString();
            Activate(winName);
            //stationWindow.SetStationName(selectedStation);
            //Type t = Type.GetType(fileName);
            //Window win = (Window)Activator.CreateInstance(t);
            /*Window win = dicWindow[winName];
            win.Show();
            this.Hide();*/
        }

        public void AnimationShow()
        {
            DoubleAnimation animation = new DoubleAnimation(-this.Width, 0, TimeSpan.FromSeconds(0.3));
            //animation.RepeatBehavior = 1;
            //animation.AutoReverse = true;
            Storyboard.SetTargetName(animation, "Nav");
            Storyboard.SetTargetProperty(animation, new PropertyPath(Window.LeftProperty));
            // Create a storyboard to contain the animation.
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            this.Show();
            this.Activate();
            storyboard.Begin(this);
        }

        public void AnimationHide()
        {
            //this.Show();
            DoubleAnimation animation = new DoubleAnimation(0, -this.Width, TimeSpan.FromSeconds(0.3));
            Storyboard.SetTargetName(animation, "Nav");
            Storyboard.SetTargetProperty(animation, new PropertyPath(Window.LeftProperty));
            // Create a storyboard to contain the animation.
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            this.Show();
            this.Activate();
            storyboard.Begin(this);
        }

        private void Nav_Deactivated(object sender, EventArgs e)
        {
            this.AnimationHide();
            this.Hide();
        }

    }
}
