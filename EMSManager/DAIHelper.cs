using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using System.Data;

namespace TemplateProject
{
    public sealed class DAIHelper
    {
        private static volatile DAIHelper instance;        //singleton
        private static object syncRoot = new Object();

        private DatabaseServer dbServer = null;

        private DAIHelper()
        {
            DebugUtil.Instance.LOG.Info("Init DAIHelper");
            if (CachedMap.Instance.isSetRunParam("DbType"))
            {
                string DbType = CachedMap.Instance.GetRunParam("DbType");
                DebugUtil.Instance.LOG.Info("DbType=" + DbType);
                try
                {
                    if (DbType == "MYSQL")
                    {
                        string name = CachedMap.Instance.GetRunParam("Name");
                        string IP = CachedMap.Instance.GetRunParam("IP");
                        string UserName = CachedMap.Instance.GetRunParam("UserName");
                        string Password = CachedMap.Instance.GetRunParam("Password");
                        string DatabaseName = CachedMap.Instance.GetRunParam("DatabaseName");
                        dbServer = new MysqlServer(name, IP, UserName, Password, DatabaseName);
                    }
                    else if (DbType == "ORACLE")
                    {
                        string TNSName = CachedMap.Instance.GetRunParam("TNSName");
                        string UserName = CachedMap.Instance.GetRunParam("UserName");
                        string Password = CachedMap.Instance.GetRunParam("Password");
                        DebugUtil.Instance.LOG.Info("Connect to oracle server, TNSName=" + TNSName + ", Username=" + UserName + ", Password=" + Password);
                        dbServer = new OracleServer(TNSName, UserName, Password);
                        DebugUtil.Instance.LOG.Info("Connect to oracle server successfully");
                    }
                }
                catch (Exception ex)
                {
                    DebugUtil.Instance.LOG.Error("Cannot connect to database, application will exit, error=" + ex.ToString());
                    //throw ex;
                }
                //dbServer = serverMap["TRANSACT"] as DatabaseServer;
                //string sqlstr = "select name from location where pkey>=3 order by pkey asc";
                //DataTable schemaTable = dbServer.ExecuteQuery(sqlstr);
            }
            else
            {
                DebugUtil.Instance.LOG.Error("Cannot find database setting, application will exit");
                System.Environment.Exit(0);
            }
        }

        public static DAIHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new DAIHelper();
                    }
                }

                return instance;
            }
        }

        public DatabaseServer DbServer
        {
            get
            {
                return dbServer;
            }
        }

        public Dictionary<DateTime, double> GetDataLogForEntityOnDate(int entitykey, DateTime selectedDate)
        {
            Dictionary<DateTime, double> dictRet = new Dictionary<DateTime, double>();
            string sqlstr = "select * from DATALOG_DP_LOG_TREND where entity_key=" + entitykey 
                + " and to_char(planlogtime,'dd/mm/yyyy')=" + selectedDate.ToString("dd/mm/yyyy") 
                + " order by planlogtime asc";
            DataTable dt1 = dbServer.GetQueryData(sqlstr);
            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                DateTime datetime = Convert.ToDateTime(dt1.Rows[i]["planlogtime"]);
                double value = Convert.ToDouble(dt1.Rows[i]["value"]);
                dictRet[datetime] = value;
            }

            return dictRet;
        }

        public Dictionary<DateTime, double> GetDataLogForEntityBetweenTime(int entitykey, DateTime startTime, DateTime endTime)
        {
            string strStartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss");
            string strEndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss");
            Dictionary<DateTime, double> dictRet = new Dictionary<DateTime, double>();
            string sqlstr = "select * from DATALOG_DP_LOG_TREND where entity_key=" + entitykey 
                + " and planlogtime>=to_date('" + strStartTime + "', 'YYYY-MM-DD HH24:MI:SS')" 
                + " and planlogtime<=to_date('" + strEndTime + "', 'YYYY-MM-DD HH24:MI:SS')" 
                + " order by planlogtime asc";
            DataTable dt1 = dbServer.GetQueryData(sqlstr);
            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                DateTime datetime = Convert.ToDateTime(dt1.Rows[i]["planlogtime"]);
                double value = Convert.ToDouble(dt1.Rows[i]["value"]);
                dictRet[datetime] = value;
            }

            return dictRet;
        }

        //return -Double.MaxValue, it mean invalid data
        public double GetDataLogForEntityOnTime(int entitykey, DateTime dateTime)
        {
            double ret = -Double.MaxValue;
            string strDateTime = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            string sqlstr = "select value from DATALOG_DP_LOG_TREND where entity_key=" + entitykey
                + " and planlogtime=to_date('" + strDateTime + "', 'YYYY-MM-DD HH24:MI:SS')";
            try
            {
                string value = dbServer.GetSingleData(sqlstr);
                if(value != null)
                    ret = Convert.ToDouble(value);
            }
            catch (Exception) { }

            return ret;
        }

    }
}
