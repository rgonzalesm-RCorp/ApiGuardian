using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ApiGuardian.Infrastructure.Persistence
{
    public class DapperContextSqlServer64
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContextSqlServer64(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnectionSqlServer64")!;
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
