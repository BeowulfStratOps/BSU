using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using BSU.Core;
using BSU.Core.Sync;
using BSU.CoreCommon;
using DownloadAction = BSU.Core.State.DownloadAction;
using UpdateAction = BSU.Core.State.UpdateAction;

namespace RealTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var baseDir = new DirectoryInfo("E:/bsu_real_test");
            var cachePath = "E:/bsu_cache";
            Setup(baseDir, cachePath);
            Test(baseDir);
        }

        private static void Test(DirectoryInfo baseDir)
        {
            var settingsFile = new FileInfo(Path.Combine(baseDir.FullName, "settings.json"));
            if (settingsFile.Exists) settingsFile.Delete();
            var core = new Core(settingsFile);
            core.AddRepo("main", "http://127.0.0.1/bsu_real_test/server/repo1.json", "BSO");
            core.AddRepo("ww2", "http://127.0.0.1/bsu_real_test/server/repo2.json", "BSO");
            core.AddRepo("joint", "http://127.0.0.1/bsu_real_test/server/repo2.json", "BSO");
            var storage = Path.Combine(baseDir.FullName, "storage");
            core.AddStorage("main", new DirectoryInfo(Path.Combine(storage,"main")), "DIRECTORY");
            core.AddStorage("ww2", new DirectoryInfo(Path.Combine(storage,"ww2")), "DIRECTORY");
            core.AddStorage("steam", new DirectoryInfo(Path.Combine(storage,"steam")), "STEAM");

            var state = core.GetState();

            var ace = state.Repos.Single(r => r.Name == "main").Mods.Single(m => m.Name == "ace_v1");
            ace.Selected = ace.Actions.OfType<UpdateAction>().Single();

            state.Repos.Single(r => r.Name == "main").PrepareUpdate().DoUpdate();

            while (core.GetActiveJobs().Any())
            {
                Thread.Sleep(500);
            }

            Assert(!state.IsValid);

            Assert(CheckEqual(Path.Combine(baseDir.FullName, "source", "ace_v1"),
                Path.Combine(baseDir.FullName, "storage", "main", "@ace")));

            state = core.GetState();

            var acex = state.Repos.Single(r => r.Name == "joint").Mods.Single(m => m.Name == "acex_v1");
            acex.Selected = acex.Actions.OfType<DownloadAction>().Single(d => d.Storage.Name == "main");
            (acex.Selected as DownloadAction).FolderName = "acex_test";

            var cba = state.Repos.Single(r => r.Name == "ww2").Mods.Single(m => m.Name == "cba_v1");
            cba.Selected = cba.Actions.OfType<DownloadAction>().Single(d => d.Storage.Name == "main");
            (cba.Selected as DownloadAction).FolderName = "cba_test";

            state.Repos.Single(r => r.Name == "joint").PrepareUpdate().DoUpdate();
            Assert(!state.IsValid);

            var oldJob = core.GetActiveJobs().Single() as RepoSync;
            StallJob(oldJob);

            try
            {
                state.Repos.Single(r => r.Name == "ww2").PrepareUpdate().DoUpdate();
                Assert(false);
            }
            catch (InvalidOperationException)
            {
                Assert(true);
            }

            state = core.GetState();

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
            core.Shutdown();
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
            for (int i = 0; i < 120*5; i++)
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

        private class WaitJob : WorkUnit
        {
            public WaitJob(IStorageMod storage, string path, RepoSync sync) : base(storage, path, sync)
            {
            }

            protected override void DoWork(CancellationToken token)
            {
                Thread.Sleep(1000);
            }
        }

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

        private static void Setup(DirectoryInfo baseDir, string cachePath)
        {
            if (baseDir.Exists) baseDir.Delete(true);
            baseDir.Create();
            var sourceDir = baseDir.CreateSubdirectory("source");
            SetupSource(sourceDir, cachePath);
            var targetDir = baseDir.CreateSubdirectory("server");
            SetupServer(baseDir, sourceDir, targetDir);
            // TODO start webserver
            SetupStorage(sourceDir, baseDir.CreateSubdirectory("storage"));
        }

        private static void SetupSource(DirectoryInfo sourceDir, string cachePath)
        {
            DownloadUnpack(sourceDir.CreateSubdirectory("cba_v1"), "https://github.com/CBATeam/CBA_A3/releases/download/v3.13.0.191116/CBA_A3_v3.13.0.zip", cachePath);
            DownloadUnpack(sourceDir.CreateSubdirectory("cba_v2"), "https://github.com/CBATeam/CBA_A3/releases/download/v3.9.1.181229/CBA_A3_v3.9.1.zip", cachePath);
            DownloadUnpack(sourceDir.CreateSubdirectory("ace_v1"), "https://github.com/acemod/ACE3/releases/download/v3.12.3/ace3_3.12.3.zip", cachePath);
            DownloadUnpack(sourceDir.CreateSubdirectory("ace_v2"), "https://github.com/acemod/ACE3/releases/download/v3.13.0-rc1/ace3_3.13.0-rc1.zip", cachePath);
            DownloadUnpack(sourceDir.CreateSubdirectory("acre2_v1"), "https://github.com/IDI-Systems/acre2/releases/download/v2.7.2.1022/acre2_2.7.2.1022.zip", cachePath);
            DownloadUnpack(sourceDir.CreateSubdirectory("acre2_v2"), "https://github.com/IDI-Systems/acre2/releases/download/v2.5.1.980/acre2_2.5.1.980.zip", cachePath);
            DownloadUnpack(sourceDir.CreateSubdirectory("acex_v1"), "https://github.com/acemod/ACEX/releases/download/v3.5.0-rc1/acex_3.5.0-rc1.zip", cachePath);
        }

        private static void DownloadUnpack(DirectoryInfo dir, string uri, string cachePath)
        {
            byte[] data;
            var cacheFile = Path.Combine(cachePath, dir.Name + ".zip");
            if (File.Exists(cacheFile))
            {
                data = File.ReadAllBytes(cacheFile);
            }
            else
            {
                Console.WriteLine("Downloading " + uri);
                using var client = new WebClient();
                data = client.DownloadData(uri);
                File.WriteAllBytes(cacheFile, data);
            }
            Console.WriteLine("Unpacking " + uri);
            using var stream = new MemoryStream(data);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (var entry in zip.Entries)
            {
                if (entry.Name == "") continue; // directory
                var path = Path.Combine(entry.FullName.Split("/").Skip(1).ToArray());
                path = Path.Combine(dir.FullName, path);
                var fi = new FileInfo(path);
                if (!fi.Directory.Exists) fi.Directory.Create();
                entry.ExtractToFile(path);
            }
        }

        private static void SetupStorage(DirectoryInfo source, DirectoryInfo storage)
        {
            // TODO: mess with symlinks

            var main = storage.CreateSubdirectory("main");
            CopyDirectory(source, "cba_v1", main.CreateSubdirectory("@cba"));
            Corrupt(CopyDirectory(source, "ace_v2", main.CreateSubdirectory("@ace")));

            var ww2 = storage.CreateSubdirectory("ww2");
            CopyDirectory(source, "acex_v1", ww2.CreateSubdirectory("@acex"));
            Corrupt(CopyDirectory(source, "acre2_v1", ww2.CreateSubdirectory("@acre2")));

            var steam = storage.CreateSubdirectory("steam").CreateSubdirectory("steamapps")
                .CreateSubdirectory("workshop").CreateSubdirectory("content").CreateSubdirectory("107410");
            CopyDirectory(source, "cba_v2", ww2.CreateSubdirectory("450814997"));
        }

        private static void SetupServer(DirectoryInfo baseDir, DirectoryInfo source, DirectoryInfo target)
        {
            BSU.Server.Program.Main(new[]
                {MakeRepoIni(baseDir, "repo1", source, target, new[] {"cba_v1", "ace_v1", "acre2_v1"})});
            BSU.Server.Program.Main(new[]
                {MakeRepoIni(baseDir, "repo2", source, target, new[] {"cba_v1", "acex_v1", "acre2_v2"})});
            BSU.Server.Program.Main(new[]
                {MakeRepoIni(baseDir, "repo3", source, target, new[] {"cba_v2", "ace_v2", "acre2_v2"})});
        }

        private static void Assert(bool b)
        {
            if (!b) throw new InvalidOperationException("Assertion failed!");
        }

        private static string MakeRepoIni(DirectoryInfo baseDir, string name, DirectoryInfo source,
            DirectoryInfo target, string[] mods)
        {
            var path = Path.Combine(baseDir.FullName, $"{name}.ini");
            var strMods = string.Join(",", mods);
            File.WriteAllText(path, @$"[Preset]
SourcePath={source.FullName}
TargetPath={target.FullName}
ModList={strMods}

[Server]
Name=main
Address=http://127.0.0.1/
Password=
Port=0
LastUpdate=2019-12-06T08:18:56.343563+00:00
Guid={Guid.NewGuid()}
SyncUris=http://127.0.0.1/
CreationDate=2017-01-19T16:29:35.294546+00:00

[Misc]
ServerFileName={name}.json");
            return path;
        }

        private static DirectoryInfo CopyDirectory(DirectoryInfo source, string sourceName, DirectoryInfo target)
        {
            source = new DirectoryInfo(Path.Combine(source.FullName, sourceName));
            foreach (var file in source.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var newPath = file.FullName.Replace(source.FullName, target.FullName);
                new FileInfo(newPath).Directory.Create();
                file.CopyTo(newPath);
            }

            return target;
        }

        private static void Corrupt(DirectoryInfo dir)
        {
            var random = new Random(1337);
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (random.Next(100) < 10)
                {
                    file.Delete();
                    continue;
                }

                if (random.Next(100) < 20)
                {
                    using var stream = file.OpenWrite();
                    var buffer = new byte[random.Next(1024 * 16)];
                    random.NextBytes(buffer);
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
