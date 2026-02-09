using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ApiGuardian.Infrastructure.Persistence
{
    public class DapperContextSqlServer
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContextSqlServer(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnectionSqlServer")!;
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
