using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace BSU.CLI
{
    static class LogWindow
    {
        public static int RunClient()
        {
            var stream = new NamedPipeClientStream(".", "bsu_cli_log", PipeDirection.InOut);
            stream.Connect();
            stream.ReadMode = PipeTransmissionMode.Message;
            var reader = new StreamReader(stream);
            while (stream.IsConnected)
            {
                if (!stream.IsMessageComplete)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var buffer = new byte[2048*10];
                var len = stream.Read(buffer, 0, buffer.Length);
                Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, len));
                if (len == buffer.Length)
                {
                    throw new NotImplementedException();
                }
            }

            return 0;
        }

        public static void NLogCall(string level, string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            _serverStream.Write(buffer, 0, buffer.Length);
        }

        private static NamedPipeServerStream _serverStream;

        public static void StartServer()
        {
            _serverStream = new NamedPipeServerStream("bsu_cli_log", PipeDirection.InOut, 1, PipeTransmissionMode.Message);
            var procInfo = new ProcessStartInfo("BSU.CLI.exe", "log_window") {CreateNoWindow = false, UseShellExecute = true};
            Process.Start(procInfo);
            _serverStream.WaitForConnection();
        }

        public static void StopServer()
        {
            _serverStream.Close();
        }
    }
}
