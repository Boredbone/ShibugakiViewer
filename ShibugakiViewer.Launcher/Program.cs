using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;

namespace ShibugakiViewer.Launcher
{
    class Program
    {

        private const string mutexId = "79509481-1f8d-44b0-a581-d0dd4fa23710";
        private const string pipeId = "1af9b56b-4195-4b99-9893-1edfb2f84cbe";
        private const string serverPath = @"ShibugakiViewer.exe";
        private static readonly string[] folderCandidates
            = new[] { "bin", "Debug\\netcoreapp3.1", "Release\\netcoreapp3.1", "Debug", "Release" };
        private const string endMark = "?";

        static void Main(string[] args)
        {
            bool exitMode = false;
            bool retryExitMode = false;
            if (args != null && args.Length == 1 && args[0] != null)
            {
                var argLow = args[0].ToLower();
                exitMode = (argLow.Equals("/q"));
                retryExitMode = (argLow.Equals("/qq"));
            }


            try
            {
                if (retryExitMode)
                {
                    RetryExitServer();
                    return;
                }

                var isServerRunning = true;
                bool createdNew = false;

                using (var mutex = new Mutex(true, mutexId, out createdNew))
                {
                    if (createdNew)
                    {
                        // Got mutex
                        isServerRunning = false;
                        mutex.ReleaseMutex();
                    }
                    mutex.Close();
                }
                if (isServerRunning)
                {
                    if (exitMode)
                    {
                        ExitServer(5000);

                        using (var mutex = new Mutex(false, mutexId))
                        {
                            if (mutex.WaitOne(5000, false))
                            {
                                // Got mutex
                                mutex.ReleaseMutex();
                                mutex.Close();
                            }
                        }
                    }
                    else
                    {
                        SendMessages(5000, args, endMark);
                    }
                }
                else
                {
                    if (!exitMode)
                    {
                        RunServer(args);
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        static void RunServer(string[] args)
        {
            var dir = System.AppDomain.CurrentDomain.BaseDirectory;

            foreach (var folder in folderCandidates)
            {
                try
                {
                    var workingDirectory = Path.Combine(dir, folder);
                    var path = Path.Combine(workingDirectory, serverPath);

                    var psi = new ProcessStartInfo()
                    {
                        FileName = path,
                        WorkingDirectory = workingDirectory,
                        Arguments = (args != null) ? string.Join(" ", args.Select(x => $"\"{x}\"")) : "",
                    };

                    var p = System.Diagnostics.Process.Start(psi);

                    return;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
            }
        }

        static void SendMessages(int timeout, string[] args, string endText)
        {
            using (var pipeClient =
                new NamedPipeClientStream(".", pipeId, PipeDirection.Out))
            {
                pipeClient.Connect(timeout);

                using (var sw = new StreamWriter(pipeClient) { AutoFlush = true })
                {
                    if (args != null)
                    {
                        foreach (var line in args)
                        {
                            sw.WriteLine(line);
                        }
                    }
                    sw.WriteLine(endText);
                }
            }
        }
        static void ExitServer(int timeout) => SendMessages(timeout, null, "?exit");

        static void RetryExitServer()
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    ExitServer(300);
                }
                catch (Exception)
                {
                    break;
                }
                Thread.Sleep(200);
            }
        }
    }
}
