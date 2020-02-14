using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Core.View;

namespace BSU.CLI
{
    public class ContStateDisplay
    {
        private readonly ViewModel _state;
        private string _buffer = "";
        private readonly object _drawLock = new object();

        public static void Show(ViewModel state)
        {
            new ContStateDisplay(state).Run();
        }

        private ContStateDisplay(ViewModel state)
        {
            _state = state;
        }

        private void Run()
        {
            Draw();

            foreach (var repo in _state.Repositories)
            {
                repo.JobHelper.OnJobStarted += Draw;
                repo.JobHelper.OnJobEnded += Draw;
                foreach (var mod in repo.Mods)
                {
                    mod.JobHelper.OnJobStarted += Draw;
                    mod.JobHelper.OnJobEnded += Draw;
                }

                repo.Mods.CollectionChanged += (sender, args) =>
                {
                    Draw();
                };
            }
            foreach (var storage in _state.Storages)
            {
                storage.JobHelper.OnJobStarted += Draw;
                storage.JobHelper.OnJobEnded += Draw;
                foreach (var mod in storage.Mods)
                {
                    mod.JobHelper.OnJobStarted += Draw;
                    mod.JobHelper.OnJobEnded += Draw;
                }
                
                storage.Mods.CollectionChanged += (sender, args) =>
                {
                    Draw();
                };
            }

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    if (_buffer == "q")
                    {
                        Console.Clear();
                        break;
                    }
                    _buffer = "";
                    continue;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    _buffer = _buffer.Substring(0, _buffer.Length - 1);
                }

                _buffer += key.KeyChar;
                Draw();
            }

            // TODO: catch keyboard interrupt?
        }

        private void Draw()
        {
            lock (_drawLock)
            {
                DoDraw();
            }
        }

        private void DoDraw()
        {
            Console.Clear();
            Console.WriteLine("Repos");
            foreach (var repository in _state.Repositories)
            {
                Console.WriteLine(" " + repository.Name + (repository.JobHelper.HasActiveJobs() ? " %" : ""));
                foreach (var repositoryMod in repository.Mods)
                {
                    Console.WriteLine("  " + repositoryMod.Name + (repositoryMod.JobHelper.HasActiveJobs() ? " %" : "") + " " + repositoryMod.DisplayName);
                    foreach (var match in repositoryMod.Matches)
                    {
                        Console.WriteLine($"   {match.StorageMod.Name} {match.Action}");
                    }
                }
            }

            Console.WriteLine("Storages");
            foreach (var storage in _state.Storages)
            {
                Console.WriteLine(" " + storage.Name + (storage.JobHelper.HasActiveJobs() ? " %" : ""));
                foreach (var storageMod in storage.Mods)
                {
                    Console.WriteLine("  " + storageMod.Name + (storageMod.JobHelper.HasActiveJobs() ? " %" : "") + " " + storageMod.DisplayName);
                }
            }

            Console.WriteLine();
            Console.Write("> " + _buffer);
        }
    }
}
