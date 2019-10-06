using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShibugakiViewer.Launcher
{
    class Program
    {

        private const string mutexId = "79509481-1f8d-44b0-a581-d0dd4fa23710";
        private const string pipeId = "1af9b56b-4195-4b99-9893-1edfb2f84cbe";
        private const string serverPath = @"ShibugakiViewer.exe";
        private static readonly string[] folderCandidates
            = new[] { "bin", "Debug\\netcoreapp3.0", "Release\\netcoreapp3.0", "Debug", "Release" };
        private const string endMark = "?";

        static void Main(string[] args)
        {
            var exitMode = args != null
                && args.Length == 1
                && args[0] != null
                && args[0].ToLower().Equals("/q");
            

            try
            {
                var isServerRunning = true;
                bool createdNew = false;

                using (var mutex = new Mutex(true, mutexId, out createdNew))
                {
                    if (createdNew)
                    {
                        //取得できた
                        isServerRunning = false;
                        mutex.ReleaseMutex();
                    }
                    mutex.Close();

                    //try
                    //{
                    //    //ミューテックスの所有権を要求する
                    //    if (mutex.WaitOne(0, false))
                    //    {
                    //        //取得できた
                    //        isServerRunning = false;
                    //        mutex.ReleaseMutex();
                    //        mutex.Close();
                    //    }
                    //}
                    //catch (AbandonedMutexException)
                    //{
                    //    isServerRunning = false;
                    //}
                }

                if (!isServerRunning)
                {
                    if (exitMode)
                    {
                        return;
                    }

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
                            //var tx = e.ToString();
                        }
                    }
                    return;
                }

                using (var pipeClient =
                    new NamedPipeClientStream(".", pipeId, PipeDirection.Out))
                {
                    pipeClient.Connect(5000);

                    // Read user input and send that to the client process.
                    using (var sw = new StreamWriter(pipeClient) { AutoFlush = true })
                    {
                        if (exitMode)
                        {
                            sw.WriteLine("?exit");
                        }
                        else
                        {
                            if (args != null)
                            {
                                foreach (var line in args)
                                {
                                    sw.WriteLine(line);
                                }
                            }
                            sw.WriteLine(endMark);
                        }
                    }
                }


                if (exitMode)
                {
                    using (var mutex = new Mutex(false, mutexId))
                    {
                        //ミューテックスの所有権を要求する
                        if (mutex.WaitOne(5000, false))
                        {
                            //取得できた
                            mutex.ReleaseMutex();
                            mutex.Close();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
