using System;
using System.Collections.Generic;

namespace AscentMigration
{
    public class MigrationResult
    {
        public string Entity               { get; set; }
        public int    Total               { get; set; }
        public int    Migrated            { get; set; }
        public int    Skipped             { get; set; }
        public bool   CompletedSuccessfully { get; set; } = true;  // false only on fatal exception
        public List<MigrationError> Errors { get; set; } = new List<MigrationError>();

        public int ErrorCount => Errors.Count;

        public void Print()
        {
            var color = ErrorCount > 0 ? ConsoleColor.Yellow : ConsoleColor.Green;
            Console.ForegroundColor = color;
            Console.WriteLine($"  [{Entity}] Total: {Total} | Migrated: {Migrated} | Skipped: {Skipped} | Errors: {ErrorCount}");
            Console.ResetColor();

            foreach (var err in Errors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    ERROR [{err.RowIdentifier}]: {err.Reason}");
                Console.ResetColor();
            }
        }
    }

    public class MigrationError
    {
        public string RowIdentifier { get; set; }   // e.g. admission_no or legacy PK
        public string Reason        { get; set; }
    }
}
