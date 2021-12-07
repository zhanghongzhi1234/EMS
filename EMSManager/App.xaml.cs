using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //string libPath = "C:\\Program Files\\DashboardEditor";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var proc = Process.GetCurrentProcess();
            var processName = proc.ProcessName.Replace(".vshost", "");
            //Process[] runningProcessAll = Process.GetProcesses()
                //.Where(x => (x.ProcessName == processName || x.ProcessName == proc.ProcessName || x.ProcessName == proc.ProcessName + ".vshost") && x.Id != proc.Id).ToArray();
            //var runningProcess = runningProcessAll[0];
            var runningProcess = Process.GetProcesses()
                .FirstOrDefault(x => (x.ProcessName == processName || x.ProcessName == proc.ProcessName || x.ProcessName == proc.ProcessName + ".vshost") && x.Id != proc.Id);
                //.FirstOrDefault(x => (x.ProcessName == processName && x.Id != proc.Id));

            if (runningProcess == null)
            {
                //var app = new App();
                //app.InitializeComponent();
                var window = new NavWindow(e.Args);
                this.MainWindow = window;
                //window.Show();      //don't call this function, it will cause start window lose focus. make sure NavWindow.Load is empty because it is not executed
                //window.HandleParameter(e.Args);

                //this.Run(window);
                return; // In this case we just proceed on loading the program
            }

            if (e.Args.Length > 0)
            {
                //IntPtr hWndPtr = runningProcess.MainWindowHandle;         //because MainWindow is the focused window, so cannot use it
                IntPtr hWndPtr = FindWindow(null, "NavWindow");
                UnsafeNative.SendMessage(hWndPtr, string.Join(" ", e.Args));
                // 还原窗口  
                ShowWindow(hWndPtr, SW_SHOWMAXIMIZED);
                // 激活窗口  
                SetForegroundWindow(hWndPtr);
                Application.Current.Shutdown();
            }
        }

        /* Windows API */

        //ShowWindow 参数    
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_NORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11;
        public const int SW_MAX = 11;
        /// <summary>  
        /// 在桌面窗口列表中寻找与指定条件相符的第一个窗口。  
        /// </summary>  
        /// <param name="lpClassName">指向指定窗口的类名。如果 lpClassName 是 NULL，所有类名匹配。</param>  
        /// <param name="lpWindowName">指向指定窗口名称(窗口的标题）。如果 lpWindowName 是 NULL，所有windows命名匹配。</param>  
        /// <returns>返回指定窗口句柄</returns>  
        [DllImport("USER32.DLL", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>  
        /// Restore window  
        /// </summary>  
        /// <param name="hWnd"></param>  
        /// <param name="nCmdShow"></param>  
        /// <returns></returns>  
        [DllImport("USER32.DLL")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>  
        /// Activate the specified window  
        /// </summary>  
        /// <param name="hWnd">Specified window handle</param>  
        /// <returns></returns>  
        [DllImport("USER32.DLL")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /*protected override void OnStartup(StartupEventArgs e)
        {
            string libPath = "C:\\Program Files\\DashboardEditor";
            try
            {
                RegistryKey hklm;
                if(Environment.Is64BitProcess == true)
                    hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                else
                    hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                RegistryKey rsg = hklm.OpenSubKey("SOFTWARE\\DashboardEditor", false);
                libPath = rsg.GetValue("Path").ToString();
                rsg.Close();

                //AppDomain.CurrentDomain.AppendPrivatePath("Lib");
                if (AppDomain.CurrentDomain.FriendlyName != "MyApp")
                {
                    AppDomainSetup domainInfo = new AppDomainSetup();
                    //domainInfo.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
                    domainInfo.ApplicationBase = libPath;
                    domainInfo.PrivateBinPath = "Lib";
                    var domain = AppDomain.CreateDomain("MyApp", null, domainInfo);
                    domain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location);
                    AppDomain.Unload(domain);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                //MessageBox.Show("Please Install Dashboard Framework Before Run The Application!");
            }

            
        }*/

        /*[STAThread]
        public static void Main()
        {
            string libPath = "C:\\Program Files\\DashboardEditor";
            try
            {
                RegistryKey hklm;
                if (Environment.Is64BitProcess == true)
                    hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                else
                    hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                RegistryKey rsg = hklm.OpenSubKey("SOFTWARE\\DashboardEditor", false);
                libPath = rsg.GetValue("Path").ToString();
                rsg.Close();

                //AppDomain.CurrentDomain.AppendPrivatePath("Lib");
                if (AppDomain.CurrentDomain.FriendlyName != "MyApp")
                {
                    AppDomainSetup domainInfo = new AppDomainSetup();
                    //domainInfo.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
                    domainInfo.ApplicationBase = libPath;
                    domainInfo.PrivateBinPath = "Lib";
                    var domain = AppDomain.CreateDomain("MyApp", null, domainInfo);
                    domain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location);
                    AppDomain.Unload(domain);
                    return;
                }

                //Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var application = new App();
                application.InitializeComponent();
                application.Run();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                //MessageBox.Show("Please Install Dashboard Framework Before Run The Application!");
            }
        }
        */
    }
}
