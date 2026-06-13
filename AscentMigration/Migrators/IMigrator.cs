using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public interface IMigrator
    {
        string Name { get; }
        Task<MigrationResult> RunAsync();
    }
}
