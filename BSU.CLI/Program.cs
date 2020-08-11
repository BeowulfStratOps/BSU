using System;
using System.IO;
using System.Linq;
using NLog;

namespace BSU.CLI
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Core.Core _core;
        static int Main(string[] args)
        {
            var ret = new Program().Main();
            return ret;
        }

        private int Main()
        {
            Console.WriteLine("Loading...");
            var settingsFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
            _core = new Core.Core(settingsFile, a => a());

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
                    _core.Dispose(true);
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

        [CliCommand("addrepo", "Adds a repository.", "type url name")]
        void AddRepo(string[] args)
        {
            _core.ViewState.AddRepository(args[0], args[1], args[2]);
        }

        [CliCommand("delrepo", "Removes a repository.", "name")]
        void DelRepo(string[] args)
        {
            throw new NotImplementedException();
            //_state.Repos.Single(r => r.Name == args[0]).Remove();
        }

        [CliCommand("addstorage", "Adds a storage.", "type path name")]
        void AddStorage(string[] args)
        {
            _core.ViewState.AddStorage(args[0], args[1], args[2]);
        }

        [CliCommand("delstorage", "Removes a repository.", "name")]
        void DelStorage(string[] args)
        {
            throw new NotImplementedException();
            //_state.Storages.Single(s => s.Name == args[0]).Remove();
        }

        [CliCommand("show", "Show state.")]
        void ShowState(string[] args)
        {
            foreach (var repo in _core.ViewState.Repositories)
            {
                Console.WriteLine(repo.Name);
                foreach (var mod in repo.Mods)
                {
                    Console.WriteLine("  " + mod.Name);
                    foreach (var match in mod.Matches)
                    {
                        Console.WriteLine("    " + match.Action);
                        /* TODO: conflicts
                        Console.WriteLine("      Conflicts");
                        foreach (var conflict in action.GetConflicts())
                        {
                            Console.WriteLine("        " + conflict);
                        }
                        */
                    }
                }
            }
        }

        /*[CliCommand("select", "Select an action.", "repo_name mod_name action_number")]
        void Select(string[] args)
        {
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
            CheckState();

            var repo = _state.Repos.Single(r =>
                r.Name.Equals(args[0], StringComparison.InvariantCultureIgnoreCase));

            using var packet = repo.PrepareUpdate();

            foreach (var packetJob in packet.GetJobs())
            {
                Console.WriteLine(
                    $"{packetJob.GetStorageModDisplayName()} -> {packetJob.GetRepositoryModDisplayName()}");
                Console.WriteLine(
                    $" Download: {packetJob.GetTotalNewFilesCount()} Files, {Utils.BytesToHuman(packetJob.GetTotalBytesToDownload())}");
                Console.WriteLine(
                    $" Update: {packetJob.GetTotalChangedFilesCount()} Files, {Utils.BytesToHuman(packetJob.GetTotalBytesToUpdate())}");
                Console.WriteLine($" Delete: {packetJob.GetTotalDeletedFilesCount()} Files");
            }

            if (!packet.GetJobs().Any())
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
        }*/

        [CliCommand("jobs", "Shows job states")]
        void Jobs(string[] args)
        {
            foreach (var job in _core.ViewState.Jobs)
            {
                Console.WriteLine($"{job.Title}: {job.Progress}%");
                /*if (job.IsDone())
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
                }*/
            }
        }

        [CliCommand("types", "Shows available repo/storage types")]
        void Types(string[] args)
        {
            Console.WriteLine("Repo Types:");
            foreach (var repoType in _core.Types.GetRepoTypes())
            {
                Console.WriteLine(" " + repoType);
            }

            Console.WriteLine("Storage Types:");
            foreach (var storageType in _core.Types.GetStorageTypes())
            {
                Console.WriteLine(" " + storageType);
            }
        }
    }
}
