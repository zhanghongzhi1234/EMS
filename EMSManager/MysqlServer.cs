using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class MysqlServer : DatabaseServer
    {
        string IP;
        string username;
        string password;
        string databaseName;
        private static MySqlConnection mySqlConn;       //数据库连接

        public MysqlServer(string name, string IP, string username, string password, string databaseName)
        {
            this.name = name;
            this.serverType = ServerType.MYSQL;
            this.IP = IP;
            this.username = username;
            this.password = password;
            this.databaseName = databaseName;

            //ConnectToDatabase(IP, username, password, databaseName);
        }

        public override void Init()
        {
            ConnectToDatabase(IP, username, password, databaseName);
        }

        public override List<RawTable> GetRawData(string name, bool exactMatch = false)
        {
            string sqlstr = "select XValue,YValue from " + name + " order by pkey asc";
            //log.Debug(sqlstr);
            List<RawTable> ret = null;
            DataTable schemaTable = ExecuteQuery(sqlstr);
            if (schemaTable != null)
            {
                ret = new List<RawTable>();
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {
                    object XValue = myField["XValue"];
                    double YValue = Convert.ToDouble(myField["YValue"]);
                    RawTable item = new RawTable(XValue, YValue);
                    ret.Add(item);
                    //For each property of the field...
                    /*foreach (DataColumn myProperty in schemaTable.Columns)
                    {
                        //Display the field name and value.
                        Console.WriteLine(myProperty.ColumnName + " = " + myField[myProperty].ToString());
                    }*/
                }
            }
            return ret;
        }

        private static void ConnectToDatabase(string server, string userid, string password, string database)
        {
            string connString = "server=" + server + ";user id=" + userid + ";password=" + password + ";database=" + database + ";charset=utf8";
            mySqlConn = new MySqlConnection(connString);
            mySqlConn.Open();
        }

        public override DataTable GetQueryData(string sqlstr)
        {
            return ExecuteQuery(sqlstr);
        }

        //query by sql
        public override DataTable ExecuteQuery(string sqlstr)
        {
            DataTable table = new DataTable();
            table.Clear();
            MySqlDataReader reader = null;
            //log.Debug(sqlstr);
            MySqlCommand cmd = new MySqlCommand(sqlstr, mySqlConn);
            try
            {
                reader = cmd.ExecuteReader();
                //DataTable schemaTable = reader.GetSchemaTable();
                //var columnNames = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetName(i);
                    //columnNames.Add(columnName);
                    table.Columns.Add(columnName);
                }
                while (reader.Read())
                {
                    DataRow newRow = table.NewRow();
                    //for (int i = 0; i < reader.FieldCount; i++)
                    int i = 0;
                    foreach (DataColumn column in table.Columns)
                    {
                        newRow[column.ColumnName] = reader.GetString(i);
                        i++;
                    }
                    table.Rows.Add(newRow);
                }
                //ret = reader.GetSchemaTable();
            }
            catch (MySqlException ex)
            {
                //log.Error(ex.ToString());
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            return table;
        }

        //执行sql语句，修改数据库, 为Insert,Update,Delete中的一种
        public override int ExecuteNonQuery(string sqlstr)
        {
            //log.Debug(sqlstr);
            MySqlCommand mySqlCmd = new MySqlCommand(sqlstr, mySqlConn);
            return mySqlCmd.ExecuteNonQuery();
        }

        public override bool SendData(string command)
        {
            return true;
        }

        public override void Close()
        {
        }
    }
}
