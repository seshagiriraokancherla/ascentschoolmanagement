using AscentMigration.Config;
using System;

namespace AscentMigration
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("============================================");
            Console.WriteLine("       Ascent Schools — Migration Tool      ");
            Console.WriteLine("============================================");
            Console.ResetColor();
            Console.WriteLine();

            MigrationConfig config;
            try
            {
                config = MigrationConfig.Load();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Config error: {ex.Message}");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            config.Print();
            Console.WriteLine();

            if (config.DryRun)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  *** DRY RUN mode — nothing will be written ***");
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.Write("Press Enter to start migration (Ctrl+C to cancel)...");
            Console.ReadLine();
            Console.WriteLine();

            var runner = new MigrationRunner(config);
            runner.RunAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
