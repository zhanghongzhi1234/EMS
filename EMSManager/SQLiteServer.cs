using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data.Common;

namespace TemplateProject
{
    public class SQLiteServer : DatabaseServer
    {
        public string databaseName = "data.db";
        // Holds our connection with the database
        SQLiteConnection mySQLiteConn;

        public SQLiteServer(string name, string databaseName = "data.db")
        {
            this.name = name;
            this.databaseName = databaseName;
            //ConnectToDatabase(databaseName);
        }

        public override void Init()
        {
            ConnectToDatabase(databaseName);
        }

        public override List<RawTable> GetRawData(string name, bool exactMatch = false)
        {
            return null;
        }

        public void ConnectToDatabase(string databaseName)
        {
            string connString = "Data Source=" + databaseName + ";Version=3;";
            //string connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + databaseName;
            try
            {
                mySQLiteConn = new SQLiteConnection(connString);
                mySQLiteConn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Failed to create a database connection. \n{0}", ex.Message);
                return;
            }
        }

        public override void Close()
        {
            if (mySQLiteConn != null && mySQLiteConn.State == ConnectionState.Open)
            {
                try
                {
                    mySQLiteConn.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: Failed to close a database connection. \n{0}", ex.Message);
                    return;
                }

            }
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
            //DataSet myDataSet = new DataSet();
            SQLiteDataReader reader = null;
            //log.Debug(sqlstr);
            SQLiteCommand mySQLiteCommand = new SQLiteCommand(sqlstr, mySQLiteConn);
            try
            {
                reader = mySQLiteCommand.ExecuteReader();

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
                        //Type t = reader.GetFieldType(i);
                        //string temp = reader.GetDataTypeName(i);      //use Type to switch GetInt32, GetDateTime, GetDecimal, GetFloat, there are too much cases
                        //newRow[column.ColumnName] = reader.GetString(i);
                        newRow[column.ColumnName] = reader[i];
                        /*if (i == 0)         //pkey
                            newRow[column.ColumnName] = reader.GetInt32(i);
                        else if(i == 1)     //time
                            newRow[column.ColumnName] = reader.GetDateTime(i);
                        else if (i == 9 || i == 10)
                            newRow[column.ColumnName] = reader.GetDecimal(i);
                        else
                            newRow[column.ColumnName] = reader.GetFloat(i);*/
                        i++;
                    }
                    table.Rows.Add(newRow);
                }
                //ret = reader.GetSchemaTable();
            }
            catch (SQLiteException ex)
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

        //return if the specific table is 0 record
        public bool ExistRecord(string sqlstr)
        {
            bool ret = false;
            SQLiteDataReader reader = null;
            //log.Debug(sqlstr);
            SQLiteCommand mySQLiteCommand = new SQLiteCommand(sqlstr, mySQLiteConn);
            try
            {
                reader = mySQLiteCommand.ExecuteReader();
                if (reader.HasRows)
                    ret = true;
            }
            catch (SQLiteException ex)
            {
                //log.Error(ex.ToString());
                ret = false;
            }
            finally
            {
                if (reader != null) reader.Close();
            }
            return ret;
        }

        //return if the specific table is 0 record
        public bool ExistTable(string tableName)
        {
            string sqlstr = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
            return ExistRecord(sqlstr);
        }

        //执行sql语句，修改数据库, 为Insert,Update,Delete中的一种
        public override int ExecuteNonQuery(string sqlstr)
        {
            //log.Debug(sqlstr);
            int ret = -1;
            try
            {
                //SQLiteCommand command = new SQLiteCommand(sqlstr, mySQLiteConn);
                SQLiteCommand command = new SQLiteCommand(sqlstr, mySQLiteConn);
                ret = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                Console.WriteLine("Error: Failed to execute non query: {0}. \n{1}", sqlstr, ex.Message);
            }
            return ret;
        }

        //执行多条sql语句，修改数据库, 为Insert,Update,Delete中的一种, use Transaction can greatly improve insert speed, 10000 sql just take 0.1 second
        public int ExecuteNonQuery(string[] sqlstrArray)
        {
            //log.Debug(sqlstr);
            int ret = 0;
            SQLiteCommand command = new SQLiteCommand(mySQLiteConn);
            DbTransaction trans = mySQLiteConn.BeginTransaction();
            try
            {
                foreach (string sqlstr in sqlstrArray)
                {
                    command.CommandText = sqlstr;
                    command.ExecuteNonQuery();
                }
                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                //System.Windows.MessageBox.Show(ex.Message);
                Console.WriteLine("Error: Failed to execute non query: {0}. \n{1}", command.CommandText, ex.Message);
                ret = -1;
                throw (ex);
            }
            return ret;
        }

        //write DataTable to Datatable
        public int WriteDatatableToDb(string tableName, DataTable dtContent)
        {
            return ExecuteNonQuery(CreateInsertStatement(tableName, dtContent).ToArray());
        }

        public override bool SendData(string command)
        {
            return true;
        }
    }
}
