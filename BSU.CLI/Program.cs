using System;
using System.IO;
using System.Linq;
using BSU.Core;
using BSU.Core.State;
using NLog;
using NLog.Fluent;

namespace BSU.CLI
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Core.Core _core;
        private State _state = null;

        static int Main(string[] args)
        {
            return new Program().Main();
        }

        private int Main()
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
                Logger.Info("Issued command {0}", command);
                if (command == "exit")
                {
                    _core.Shutdown();
                    return 0;
                }
                try
                {
                    commands.Process(command);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Console.WriteLine($"Error: {e.GetType().Name}\n{e.Message}");
#if DEBUG
                    Console.WriteLine(e.StackTrace);
#endif
                }
            }
        }

        [CliCommand("addrepo", "Adds a repository.", "type name url")]
        void AddRepo(string[] args)
        {
            _core.AddRepo(args[1], args[2], args[0]);
        }

        [CliCommand("delrepo", "Removes a repository.", "name")]
        void DelRepo(string[] args)
        {
            _core.RemoveRepo(args[0]);
        }

        [CliCommand("addstorage", "Adds a storage.", "type name path")]
        void AddStorage(string[] args)
        {
            _core.AddStorage(args[1], new DirectoryInfo(args[2]), args[0]);
        }

        [CliCommand("delstorage", "Removes a repository.", "name")]
        void DelStorage(string[] args)
        {
            _core.RemoveStorage(args[0]);
        }

        [CliCommand("printintstate", "Prints the internal state.")]
        void PrintInternalState(string[] args)
        {
            _core.PrintInternalState();
        }

        [CliCommand("calcstate", "Calculate state.")]
        void CalcState(string[] args)
        {
            _state = _core.GetState();
            _state.Invalidated += StateOnInvalidated;
        }

        private void StateOnInvalidated()
        {
            Console.WriteLine("State invalidated.");
        }

        [CliCommand("showstate", "Show state.")]
        void ShowState(string[] args)
        {
            if (_state == null)
            {
                Console.WriteLine("None active. Creating...");
                _state = _core.GetState();
            }

            if (!_state.IsValid) Console.WriteLine("State got invalidated!");

            foreach (var repo in _state.Repos)
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
                        if (!action.GetConflicts().Any()) continue;
                        Console.WriteLine("      Conflicts");
                        foreach (var conflict in action.GetConflicts())
                        {
                            Console.WriteLine("        " + conflict);
                        }
                    }
                }
            }
        }

        [CliCommand("select", "Select an action.", "repo_name mod_name action_number")]
        void Select(string[] args)
        {
            if (_state == null)
                throw new InvalidOperationException("No active state. use calcstate (or showstate) to create one.");

            string repoName = args[0], modName = args[1];
            var action = int.Parse(args[2]);

            var mod = _state.Repos.Single(r => r.Name.Equals(repoName, StringComparison.InvariantCultureIgnoreCase))
                .Mods.Single(m => m.Name.Equals(modName, StringComparison.InvariantCultureIgnoreCase));

            var selected = mod.Actions[action - 1];

            if (selected is DownloadAction downloadAction)
            {
                while (string.IsNullOrWhiteSpace(downloadAction.FolderName))
                {
                    Console.Write("Enter new folder name: ");
                    downloadAction.FolderName = Console.ReadLine();
                }
            }

            mod.Selected = mod.Actions[action - 1];
        }

        [CliCommand("update", "Update for a repository mod.", "repo_name")]
        void Update(string[] args)
        {
            if (_state == null)
                throw new InvalidOperationException("No active state. use calcstate (or showstate) to create one.");

            var repo = _state.Repos.Single(r =>
                r.Name.Equals(args[0], StringComparison.InvariantCultureIgnoreCase));

            var packet = repo.PrepareUpdate();

            foreach (var packetJob in packet.GetJobsViews())
            {
                Console.WriteLine($"{packetJob.GetStorageModDisplayName()} -> {packetJob.GetRepositoryModDisplayName()}");
                Console.WriteLine($" Download: {packetJob.GetTotalNewFilesCount()} Files, {Utils.BytesToHuman(packetJob.GetTotalBytesToDownload())}");
                Console.WriteLine($" Update: {packetJob.GetTotalChangedFilesCount()} Files, {Utils.BytesToHuman(packetJob.GetTotalBytesToUpdate())}");
                Console.WriteLine($" Delete: {packetJob.GetTotalDeletedFilesCount()} Files");
            }

            if (!packet.GetJobsViews().Any())
            {
                Console.WriteLine("Nothing to do.");
                return;
            }

            Console.Write("Proceed? (y/Y = yes. Anything else = no): ");
            if (Console.ReadLine().ToLowerInvariant() != "y")
            {
                Console.WriteLine("Aborting");
                return;
            }

            packet.DoUpdate();
        }

        [CliCommand("jobs", "Shows job states")]
        void Jobs(string[] args)
        {
            if (_state == null)
                throw new InvalidOperationException("No active state. use calcstate (or showstate) to create one.");

            var jobs = _core.GetAllJobs();

            foreach (var job in jobs)
            {
                Console.WriteLine($"{job.GetStorageModDisplayName()} -> {job.GetRepositoryModDisplayName()}");
                if (job.IsDone())
                {
                    var error = job.GetError();
                    Console.WriteLine(error == null ? " Done" : $" Error: {error}");
                }
                else
                {
                    Console.WriteLine(
                        $" Download: {job.GetRemainingNewFilesCount()} Files, {Utils.BytesToHuman(job.GetRemainingBytesToDownload())}");
                    Console.WriteLine(
                        $" Update: {job.GetRemainingChangedFilesCount()} Files, {Utils.BytesToHuman(job.GetRemainingBytesToUpdate())}");
                    Console.WriteLine($" Delete: {job.GetRemainingDeletedFilesCount()} Files");
                }
            }
        }

        [CliCommand("types", "Shows available repo/storage types")]
        void Types(string[] args)
        {
            Console.WriteLine("Repo Types:");
            foreach (var repoType in _core.GetRepoTypes())
            {
                Console.WriteLine(" " + repoType);
            }
            Console.WriteLine("Storage Types:");
            foreach (var storageType in _core.GetStorageTypes())
            {
                Console.WriteLine(" " + storageType);
            }
        }
    }
}
