using System.Data;
using System.Threading.Tasks;

namespace IoTMonitoring.Api.Data.Connection
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
    }
}