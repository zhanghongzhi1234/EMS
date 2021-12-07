using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public abstract class DatabaseServer : Server
    {
        public abstract DataTable ExecuteQuery(string sqlstr);     //query by sql
        public abstract int ExecuteNonQuery(string sqlstr);             //执行sql语句，修改数据库, 为Insert,Update,Delete中的一种

        public virtual List<string> CreateInsertStatement(string tableName, DataTable dtContent)
        {
            List<string> ret = new List<string>();
            string sqlInsertHeader = "Insert into [" + tableName + "](";
            /*[alarmID],[sourceTime],[ackTime],[closeTime],[assetName],[alarmSeverity],[alarmDescription],"
                + "[alarmAcknowledgeBy],[state],[locationId],[parentAlarmID],[avalancheHeadID],[isHeadOfAvalanche],[isChildOfAvalanche],[mmsState],"
                + "[alarmValue],[omAlarm],[alarmType],[subsystemkey],[systemkey],[alarmComments],[strAlarmType],[subsystemType],[systemkeyType],"
                + "[locationKey]) values ";*/

            string[] columnNames = dtContent.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();

            for (int i = 0; i < columnNames.Count(); i++)
            {
                sqlInsertHeader += "[" + columnNames[i] + "],";
            }
            sqlInsertHeader = sqlInsertHeader.TrimEnd(',');
            sqlInsertHeader += ") values ";
            for (int i = 0; i < dtContent.Rows.Count; i++)
            {
                string sqlstr;
                string sqlInsertBody = "(";
                for (int j = 0; j < dtContent.Columns.Count; j++)
                {
                    sqlInsertBody += "'" + dtContent.Rows[i][j].ToString() + "',";
                }
                sqlInsertBody = sqlInsertBody.TrimEnd(',');
                sqlInsertBody += ")";
                sqlstr = sqlInsertHeader + sqlInsertBody;
                ret.Add(sqlstr);
            }
            
            return ret;
        }

        //get vector Data, 1 column * many rows
        public virtual List<string> GetVectorData(string sqlstr)
        {
            DebugUtil.Instance.LOG.Debug(sqlstr);
            List<string> ret = null;
            DataTable dataTable = ExecuteQuery(sqlstr);
            if (dataTable != null)
            {
                ret = new List<string>();
                //For each field in the table...
                foreach (DataRow row in dataTable.Rows)
                {
                    ret.Add(row[0].ToString());
                }
            }
            return ret;
        }

        //get single Data, 1 column 1 row
        public virtual string GetSingleData(string sqlstr)
        {
            DebugUtil.Instance.LOG.Debug(sqlstr);
            string ret = null;
            DataTable dataTable = ExecuteQuery(sqlstr);
            if (dataTable != null)
            {
                ret = dataTable.Rows[0][0].ToString();
            }
            return ret;
        }
    }
}
