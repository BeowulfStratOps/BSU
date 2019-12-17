using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BSU.Core;

namespace RealTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Setup();
            Test();
        }

        static void Test()
        {
            var settingsFile = new FileInfo("settings.json");
            if (settingsFile.Exists) settingsFile.Delete();
            var core = new Core(settingsFile);
            core.AddRepo("main", "http://127.0.0.1/bsu_real_test/bsu_target/repo1.json", "BSO");
            core.AddRepo("ww2", "http://127.0.0.1/bsu_real_test/bsu_target/repo2.json", "BSO");
            core.AddRepo("joint", "http://127.0.0.1/bsu_real_test/bsu_target/repo2.json", "BSO");
            core.AddStorage("main", new DirectoryInfo("G:/bsu_real_test/storage/main"), "DIRECTORY");
            core.AddStorage("ww2", new DirectoryInfo("G:/bsu_real_test/storage/ww2"), "DIRECTORY");
            core.AddStorage("steam", new DirectoryInfo("G:/bsu_real_test/storage/steam"), "STEAM");
            
            var state = core.GetState();
        }

        static void Setup()
        {
            var baseDir = new DirectoryInfo("G:\\bsu_real_test");
            var raw = new DirectoryInfo("G:\\bsu_real_test\\bsu_source");
            var storage = baseDir.CreateSubdirectory("storage");
            storage.Delete(true);

            // TODO: mess with symlinks

            var main = storage.CreateSubdirectory("main");
            CopyDirectoy(new DirectoryInfo(Path.Combine(raw.FullName, "@cba_v1")), main.CreateSubdirectory("@cba"));
            var corruptAce = main.CreateSubdirectory("@ace");
            CopyDirectoy(new DirectoryInfo(Path.Combine(raw.FullName, "@ace_v2")), corruptAce);
            Corrupt(corruptAce);

            var ww2 = storage.CreateSubdirectory("ww2");
            CopyDirectoy(new DirectoryInfo(Path.Combine(raw.FullName, "@acex_v1")), ww2.CreateSubdirectory("@acex"));
            var corruptAcre = ww2.CreateSubdirectory("@acre2");
            CopyDirectoy(new DirectoryInfo(Path.Combine(raw.FullName, "@acre2_v1")), corruptAcre);
            Corrupt(corruptAcre);

            var steam = storage.CreateSubdirectory("steam").CreateSubdirectory("steamapps").CreateSubdirectory("workshop").CreateSubdirectory("content").CreateSubdirectory("107410");
            CopyDirectoy(new DirectoryInfo(Path.Combine(raw.FullName, "@cba_v2")), steam.CreateSubdirectory("450814997"));

            baseDir.CreateSubdirectory("bsu_target");
            
            BSU.Server.Program.Main(new[] { "G:/bsu_real_test/repo1.ini" });
            BSU.Server.Program.Main(new[] { "G:/bsu_real_test/repo2.ini" });
            BSU.Server.Program.Main(new[] { "G:/bsu_real_test/repo3.ini" });
        }

        static void CopyDirectoy(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var file in source.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var newPath = file.FullName.Replace(source.FullName, target.FullName);
                new FileInfo(newPath).Directory.Create();
                file.CopyTo(newPath);
            }
        }

        static void Corrupt(DirectoryInfo dir)
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
