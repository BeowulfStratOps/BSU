using System;
using System.IO;

namespace BSU.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            var settingsFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
            var core = new Core.Core(settingsFile);

            return 0;
        }
    }
}
