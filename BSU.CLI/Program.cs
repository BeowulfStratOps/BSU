using System;
using System.IO;
using System.Linq;
using BSU.Core;

namespace BSU.CLI
{
    class Program
    {
        private Core.Core _core;
        private ViewState _viewState = null;

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

        [CliCommand("calcstate", "Calculate state.")]
        void CalcState(string[] args)
        {
            _viewState = _core.GetViewState();
        }

        [CliCommand("showstate", "Show state.")]
        void ShowState(string[] args)
        {
            if (_viewState == null)
            {
                Console.WriteLine("None active. Creating...");
                _viewState = _core.GetViewState();
            }

            foreach (var repo in _viewState.Repos)
            {
                Console.WriteLine(repo.Name);
                foreach (var mod in repo.Mods)
                {
                    Console.WriteLine("  " + mod.Name);
                    for (int i = 0; i < mod.Actions.Count; i++)
                    {
                        var action = mod.Actions[i];
                        var actionText = "    ";
                        actionText += action == mod.Selected ? "* " : $"{i + 1} ";
                        actionText += action.ToString();
                        Console.WriteLine(actionText);
                    }
                }
            }
        }

        [CliCommand("select", "Select an action.", "repo_name mod_name action_number")]
        void Select(string[] args)
        {
            if (_viewState == null)
                throw new InvalidOperationException("No active state. use calcstate (or showstate) to create one.");

            string repoName = args[0], modName = args[1];
            var action = int.Parse(args[2]);

            var mod = _viewState.Repos.Single(r => r.Name.Equals(repoName, StringComparison.InvariantCultureIgnoreCase))
                .Mods.Single(m => m.Name.Equals(modName, StringComparison.InvariantCultureIgnoreCase));

            mod.Selected = mod.Actions[action - 1];
        }
    }
}
