using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using log4net;

namespace TemplateProject
{
    public sealed class DebugUtil
    {
        private static volatile DebugUtil instance;        //singleton
        private static object syncRoot = new Object();
        
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ILog LOG
        {
            get { return log; }
        }

        private DebugUtil() 
        {
            FileInfo configFile = new FileInfo("log4net.config");
            log4net.Config.XmlConfigurator.ConfigureAndWatch(configFile);
            log.Info("--------------------------Start of file---------------------------");
        }

        public static DebugUtil Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new DebugUtil();
                    }
                }

                return instance;
            }
        }
    }
}
