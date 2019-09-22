using System;
using System.IO;

namespace BSU.CLI
{
    class Program
    {
        private Core.Core _core;

        static int Main(string[] args)
        {
            return new Program().Main();
        }

        int Main()
        {
            Console.WriteLine("Loading...");
            var settingsFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
            _core = new Core.Core(settingsFile);

            var commands = new Commands(this);

            Console.Clear();
            Console.WriteLine("Ready. Enter help for help.");

            while (true)
            {
                Console.Write("> ");
                var command = Console.ReadLine();
                if (command == "exit") return 0;
                try
                {
                    commands.Process(command);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.GetType().Name}\n{e.Message}");
                }
            }
        }

        [CliCommand("addrepo", "Adds a repository.", "type name url")]
        void AddRepo(string[] args)
        {
            _core.AddRepo(args[1], args[2], args[0]);
        }

        [CliCommand("addstorage", "Adds a storage.", "type name path")]
        void AddStorage(string[] args)
        {
            _core.AddStorage(args[1], new DirectoryInfo(args[2]), args[0]);
        }

        [CliCommand("printintstate", "Prints the internal state.")]
        void PrintInternalState(string[] args)
        {
            _core.PrintInternalState();
        }

        [CliCommand("getstate", "Calculate state.")]
        void GetState(string[] args)
        {
            var state = _core.GetViewState();

            foreach (var repo in state.Repositories)
            {
                Console.WriteLine(repo.Name);
                foreach (var mod in repo.Mods)
                {
                    Console.WriteLine("  " + mod.Name);
                    foreach (var candidate in mod.Candidates)
                    {
                        Console.WriteLine($"    {(candidate.Item2 ? "Match:": "No match:")} " + candidate.Item1.Name);
                    }
                }
            }
        }
    }
}
