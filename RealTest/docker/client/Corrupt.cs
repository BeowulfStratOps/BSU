using System;
using System.IO;
using System.Linq;

namespace Corrupt
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Corrupt <path to directory>");
                return 1;
            }

            var path = args[0];
            var dir = new DirectoryInfo(path);

            if (!dir.Exists)
            {
                Console.WriteLine("Directory doesn't exists");
                return 1;
            }
            
            CorruptDirectory(dir);

            return 0;
        }

        private static void CorruptDirectory(DirectoryInfo dir)
        {
            var random = new Random(1337);
            var files = dir.EnumerateFiles("*", SearchOption.AllDirectories).OrderBy(d => d.FullName).ToList();
            
            foreach (var file in files)
            {
                if (random.Next(0, 100) < 30)
                {
                    CorruptFile(file, random);
                    Console.Write(".");
                }
            }
            
            Console.WriteLine("Done");
        }

        private static void CorruptFile(FileInfo file, Random random)
        {
            var type = random.Next(0, 5);

            if (type == 0)
            {
                file.Delete();
                return;
            }

            if (type == 1)
            {
                var file2 = file.CopyTo(Path.Combine(file.DirectoryName, "qwerty_fqw" + file.Name));
                if (random.Next(0, 100) < 20) CorruptFileBinary(file2, random);
                return;
            }

            if (type == 2)
            {
                var file2 = file.CopyTo(Path.Combine(file.DirectoryName, file.Name + "qwerty_fqw"));
                if (random.Next(0, 100) < 20) CorruptFileBinary(file2, random);
                return;
            }

            if (type == 3)
            {
                var file2 = file.CopyTo(Path.Combine(file.DirectoryName, file.Name + ".bso"));
                if (random.Next(0, 100) < 20) CorruptFileBinary(file2, random);
                return;
            }

            CorruptFileBinary(file, random);
        }

        private static void CorruptFileBinary(FileInfo file, Random random)
        {
            var size = random.Next((int)file.Length);
            var type = random.Next(0, 3); // remove, add, corrupt
            var position = random.Next((int)file.Length);

            using var stream = file.Open(FileMode.Open, FileAccess.ReadWrite);

            if (type == 0) // Delete size bytes at position
            {
                var remainingSize = file.Length - position - size;
                if (remainingSize <= 0)
                {
                    stream.SetLength(position);
                    return;
                }

                var buffer = new MemoryStream((int)remainingSize);
                stream.Seek(position + size, SeekOrigin.Begin);
                stream.CopyTo(buffer, (int) remainingSize);
                stream.Seek(position, SeekOrigin.Begin);
                buffer.Seek(0, SeekOrigin.Begin);
                buffer.CopyTo(stream, (int) remainingSize);
                stream.SetLength(position + remainingSize);
                
                return;
            }

            if (type == 1) // Overwrite size bytes at position
            {
                size = position + size > file.Length ? (int) file.Length : position + size;
                var buffer = new byte[size];
                random.NextBytes(buffer);
                stream.Seek(position, SeekOrigin.Begin);
                stream.Write(buffer, 0, size);
                return;
            }

            if (type == 2) // Insert size bytes at position
            {
                var remainingSize = (int) file.Length - position;
                var buffer = new MemoryStream(remainingSize);
                stream.Seek(position, SeekOrigin.Begin);
                stream.CopyTo(buffer, remainingSize);
                buffer.Seek(0, SeekOrigin.Begin);
                stream.SetLength(file.Length + size);
                stream.Seek(position + size, SeekOrigin.Begin);
                buffer.CopyTo(stream, remainingSize);
                
                var filler = new byte[size];
                random.NextBytes(filler);
                stream.Seek(position + size, SeekOrigin.Begin);
                stream.Write(filler, 0, size);
            }
        }
    }
}