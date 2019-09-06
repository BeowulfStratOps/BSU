using System;
using System.IO;

namespace BSU.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            new Program().Main();
            return 0;
        }

        void Main()
        {
            var settingsFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
            var core = new Core.Core(settingsFile);

            var commands = new Commands(this);
            
            
        }

        [CliCommand("addrepo", "Adds a repository.", "url name")]
        void AddRepo(string[] args)
        {
            
        }
    }
}
