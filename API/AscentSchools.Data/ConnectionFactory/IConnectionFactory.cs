using System.Data;

namespace AscentSchools.Data.ConnectionFactory
{
    public interface IConnectionFactory
    {
        IDbConnection GetMasterConnection();
        IDbConnection GetTenantConnection(string tenantDbName);
    }
}
