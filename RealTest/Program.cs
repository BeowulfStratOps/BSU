using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using BSU.Core;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace RealTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            SetupLogging();
            
            var settingsFile = new FileInfo("/client/settings.json");
            using var core = new Core(settingsFile, a => a());
            var state = core.Model;

            state.AddRepository("BSO", "http://server/repo1.json", "main");
            state.AddRepository("BSO", "http://server/repo2.json", "ww2");
            state.AddRepository("BSO", "http://server/repo3.json", "joint");
            state.AddStorage("DIRECTORY", new DirectoryInfo("/storage/main"), "main");
            state.AddStorage("DIRECTORY", new DirectoryInfo("/storage/side"), "ww2");
            state.AddStorage("STEAM", new DirectoryInfo("/storage/steam"), "steam");
            
            state.AddStorage("DIRECTORY", new DirectoryInfo("/cant/possibly/exist"), "error_pls");
            
            // TODO: check for errors

            while (state.Repositories.Single(r => r.Identifier == "main").Loading.IsActive()) Thread.Sleep(1);
            Thread.Sleep(10000);
            
            var executor = new ModelExecuter(core);

            var aceUpdate = executor.Update("main/@ace_v1", "main/@ace");
            
            while (!aceUpdate.IsDone()) Thread.Sleep(1);
            
            Console.WriteLine("Done");
        }

        private static void SetupLogging()
        {
            var config = new LoggingConfiguration();
            var logconsole = new ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Error, LogLevel.Fatal, logconsole);
            LogManager.Configuration = config;
        }


        /*var ace = state.Repositories.Single(r => r.Name == "main").Mods.Single(m => m.Name == "ace_v1");
        var action = ace.Matches.Single(m => m.Action.ToLowerInvariant().Contains("update"));
        
        action.DoUpdate();
        
        while (state.Jobs.Any())
        {
            Thread.Sleep(500);
        }

        Assert(CheckEqual(Path.Combine(baseDir.FullName, "source", "ace_v1"),
            Path.Combine(baseDir.FullName, "storage", "main", "@ace")));

        var acex = state.Repositories.Single(r => r.Name == "joint").Mods.Single(m => m.Name == "acex_v1");
        acex.Downloads.Single(d => d.StorageName == "main").DoDownload("acex_test");
        
        var cba = state.Repositories.Single(r => r.Name == "ww2").Mods.Single(m => m.Name == "cba_v1");
        acex.Downloads.Single(d => d.StorageName == "main").DoDownload("cba_test");

        cba = state.Repos.Single(r => r.Name == "ww2").Mods.Single(m => m.Name == "cba_v1");
        cba.Selected = cba.Actions.OfType<DownloadAction>().Single(d => d.Storage.Name == "main");
        (cba.Selected as DownloadAction).FolderName = "cba_test";

        state.Repos.Single(r => r.Name == "ww2").PrepareUpdate().DoUpdate();

        ResumeJob(oldJob);

        while (core.GetActiveJobs().Any())
        {
            Thread.Sleep(500);
        }

        Assert(CheckEqual(Path.Combine(baseDir.FullName, "source", "acex_v1"),
            Path.Combine(baseDir.FullName, "storage", "main", "@acex_test")));

        Assert(CheckEqual(Path.Combine(baseDir.FullName, "source", "cba_v1"),
            Path.Combine(baseDir.FullName, "storage", "main", "@cba_test")));

        state = core.GetState();

        var acre = state.Repos.Single(r => r.Name == "main").Mods.Single(m => m.Name == "acre2_v1");
        acre.Selected = acre.Actions.OfType<UpdateAction>().Single();

        state.Repos.Single(r => r.Name == "main").PrepareUpdate().DoUpdate();
        core.Dispose();
        core = new Core(settingsFile);
        state = core.GetState();

        acre = state.Repos.Single(r => r.Name == "ww2").Mods.Single(m => m.Name == "acre2_v2");
        acre.Selected = acre.Actions.OfType<UpdateAction>().Single();

        state.Repos.Single(r => r.Name == "main").PrepareUpdate().DoUpdate();

        core.GetActiveJobs().Single().Abort();

        while (core.GetActiveJobs().Any())
        {
            Thread.Sleep(500);
        }

        Assert(!CheckEqual(Path.Combine(baseDir.FullName, "source", "acre2_v2"),
            Path.Combine(baseDir.FullName, "storage", "ww2", "@acre2")));

        state = core.GetState();

        acre = state.Repos.Single(r => r.Name == "main").Mods.Single(m => m.Name == "acre2_v1");
        acre.Selected = acre.Actions.OfType<UpdateAction>().Single();

        state.Repos.Single(r => r.Name == "main").PrepareUpdate().DoUpdate();

        while (core.GetActiveJobs().Any())
        {
            Thread.Sleep(500);
        }

        Assert(!CheckEqual(Path.Combine(baseDir.FullName, "source", "acre2_v1"),
            Path.Combine(baseDir.FullName, "storage", "ww2", "@acre2")));

        state = core.GetState();

        var settings = Settings.Load(new FileInfo(Path.Combine(baseDir.FullName, "settings.json")));
        Assert(settings.Storages.All(s => !s.Updating.Any()));
    }

    private static void StallJob(RepoSync job)
    {
        var field = job.GetType().GetField("_actionsTodo", BindingFlags.Instance | BindingFlags.NonPublic);
        var value = field.GetValue(job);
        var jobs = value as List<WorkUnit>;
        for (int i = 0; i < 120 * 5; i++)
        {
            jobs.Insert(0, new WaitJob(job.StorageMod, "/", job));
        }
    }

    private static void ResumeJob(RepoSync job)
    {
        var field = job.GetType().GetField("_actionsTodo", BindingFlags.Instance | BindingFlags.NonPublic);
        var value = field.GetValue(job);
        var jobs = value as List<WorkUnit>;
        while (jobs.OfType<WaitJob>().Any())
        {
            jobs.Remove(jobs.OfType<WaitJob>().First());
        }
    }

    private class WaitJob : SyncWorkUnit
    {
        public WaitJob(IStorageMod storage, string path, RepoSync sync) : base(storage, path, sync)
        {
        }

        protected override void DoWork(CancellationToken token)
        {
            Thread.Sleep(1000);
        }
    }*/

        private static bool CheckEqual(string pathA, string pathB)
        {
            var dirA = new DirectoryInfo(pathA);
            var dirB = new DirectoryInfo(pathB);

            var checkedPaths = new HashSet<string>();

            foreach (var file in dirA.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var other = file.FullName.Replace(dirA.FullName, dirB.FullName);
                if (!File.ReadAllBytes(file.FullName).SequenceEqual(File.ReadAllBytes(other)))
                    return false;
                checkedPaths.Add(other.ToLowerInvariant());
            }

            foreach (var file in dirB.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (!checkedPaths.Contains(file.FullName.ToLowerInvariant()))
                    return false;
            }

            return true;
        }

        private static void Assert(bool b)
        {
            if (!b) throw new InvalidOperationException("Assertion failed!");
        }
    }
}
