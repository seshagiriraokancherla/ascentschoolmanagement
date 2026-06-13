namespace AscentMigration.Config
{
    public enum MigrationMode
    {
        Truncate,   // DELETE all rows for this school, then insert fresh
        Skip        // Insert only rows not already present (resume-safe)
    }
}
