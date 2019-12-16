using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BSU.BSO.FileStructures;
using BSU.Hashes;
using Newtonsoft.Json;

namespace BSU.Server
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ./BSUServer <path to ini file>");
                return 2;
            }
            var server = ServerConfig.Load(args[0]);

            if (!new DirectoryInfo(server.TargetPath).Exists)
            {
                Console.WriteLine($"Target path {server.TargetPath} doesn't exist. Aborting.");
                return 1;
            }

            var modList = server.ModList.Split(',').Select(m => new DirectoryInfo(Path.Combine(server.SourcePath, m)))
                .ToList();
            if (modList.Any(di => !di.Exists))
            {
                Console.WriteLine("Mod folder not found: " + string.Join(", ", modList.Where(di => !di.Exists)));
                return 1;
            }

            foreach (var sourceDir in modList)
            {
                var target = new DirectoryInfo(Path.Combine(server.TargetPath, sourceDir.Name));
                DoMod(sourceDir, target);
                server.Server.ModFolders.Add(new ModFolder(sourceDir.Name));
            }

            File.WriteAllText(Path.Combine(server.TargetPath, server.ServerFileName), JsonConvert.SerializeObject(server.Server));

            return 0;
        }

        static void DoMod(DirectoryInfo source, DirectoryInfo target)
        {
            Console.WriteLine($"Working on mod {source.Name}");
            var sourceHashes = GetQuickHashes(source);
            if (!target.Exists) target.Create();
            var targetHashes = GetQuickHashes(target);
            var actions = GetActions(sourceHashes, targetHashes);
            ApplyActions(actions, source, target);
            // TODO: sync-hash files with missing sync files
            var modHashFile = new HashFile(source.Name,
                sourceHashes.Select(kv => new HashType(kv.Key, kv.Value.GetBytes(), kv.Value.Length))
                    .ToList());
            File.WriteAllText(Path.Combine(target.FullName, "hash.json"), JsonConvert.SerializeObject(modHashFile));
        }

        static void ApplyActions(Dictionary<string, FileAction> actions, DirectoryInfo sourceMod,
            DirectoryInfo targetMod)
        {
            var total = actions.Count;
            var done = 0;
            foreach (var fileAction in actions)
            {
                var sourcePath = Path.Combine(sourceMod.FullName, fileAction.Key.Substring(1));
                var targetPath = Path.Combine(targetMod.FullName, fileAction.Key.Substring(1));
                if (fileAction.Value == FileAction.Update || fileAction.Value == FileAction.New)
                {
                    new FileInfo(targetPath).Directory.Create();
                    File.Copy(sourcePath, targetPath, true);
                }
                else // Delete
                {
                    new FileInfo(targetPath).Delete();
                }

                done++;
                Console.Write($"\r Applying changes {done} / {total}");
            }
            if (total > 0) Console.WriteLine();
        }

        static Dictionary<string, FileAction> GetActions(Dictionary<string, SHA1AndPboHash> source,
            Dictionary<string, SHA1AndPboHash> target)
        {
            var actions = new Dictionary<string, FileAction>();
            foreach (var path in Combined(source.Keys, target.Keys))
            {
                var targetFile = target.GetValueOrDefault(path);
                var sourceFile = source.GetValueOrDefault(path);
                if (sourceFile != null)
                {
                    if (targetFile != null)
                    {
                        if (!sourceFile.GetBytes().SequenceEqual(targetFile.GetBytes()))
                            actions[path] = FileAction.Update;
                    }
                    else
                        actions[path] = FileAction.New;
                }
                else
                    actions[path] = FileAction.Delete;
            }

            return actions;
        }

        enum FileAction
        {
            New,
            Update,
            Delete
        }

        private static IEnumerable<string> Combined(IEnumerable<string> one, IEnumerable<string> two)
        {
            var result = new List<string>(one.Distinct());
            result.AddRange(two.Distinct());
            return result;
        }

        static Dictionary<string, SHA1AndPboHash> GetQuickHashes(DirectoryInfo modDirectory)
        {
            var hashes = new Dictionary<string, SHA1AndPboHash>();
            var files = modDirectory.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
            var total = files.Count;
            var done = 0;
            foreach (var file in files)
            {
                // TODO: remove sync-hash files that don't have a raw file
                // TODO: collect files with missing sync-hash files so we don't have to iterate again
                if (file.Name == "hash.json")
                {
                    done++;
                    continue;
                }
                var hash = new SHA1AndPboHash(file.OpenRead(), file.Extension);
                var relPath = file.FullName.Replace(modDirectory.FullName, "").Replace('\\', '/');
                hashes[relPath] = hash;
                done++;
                Console.Write($"\r Hashing {done} / {total}");
            }
            if (total > 0) Console.WriteLine();

            return hashes;
        }
    }
}
