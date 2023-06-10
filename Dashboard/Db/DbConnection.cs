using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Db
{
    public abstract class DbConnection
    {
        private readonly string _connectionString;

        public DbConnection()
        {
            _connectionString = "Server=(local); DataBase=NorthwindStore; Integrated Security=true;";
        }

        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
