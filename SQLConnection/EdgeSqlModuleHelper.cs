using System;
using System.Collections.Generic;
using System.Text;
using Sql = System.Data.SqlClient;

namespace SQLConnection
{
    public class EdgeSqlModuleHelper
    {
        private string connectionString = null;

        public EdgeSqlModuleHelper(string connectionStr)
        {
            connectionString = connectionStr;
        }
        public string ExecuteSQL(string SQL)
        {
            string result = string.Empty;
            try
            {
                using (Sql.SqlConnection conn = new Sql.SqlConnection(connectionString))
                {
                    conn.Open();
                    using (Sql.SqlCommand cmd = new Sql.SqlCommand(SQL, conn))
                    {
                        //Execute the command and log the # rows affected.
                        var rows = cmd.ExecuteNonQuery();
                        result = rows + " rows were updated";
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            return result;
        }
    }
}
