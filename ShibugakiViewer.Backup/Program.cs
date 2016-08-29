using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ShibugakiViewer.Backup
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 6)
            {
                return;
            }

            //var dir = @"..\..\..\ShibugakiViewer\bin\Debug\";
            //var serverPath = @"ShibugakiViewer.exe";

            /*
            var mode = "/w";

            var serverFullPath = @"..\..\..\ShibugakiViewer\bin\Debug\ShibugakiViewer.exe";

            var saveDirectory = Path.Combine(System.Environment.GetFolderPath
                (Environment.SpecialFolder.LocalApplicationData),
                "Boredbone", "ShibugakiViewer");

            var mutexId = "79509481-1f8d-44b0-a581-d0dd4fa23710";
            var pipeId = "1af9b56b-4195-4b99-9893-1edfb2f84cbe";

            var files = new[] { "library.db", "libsettings.config", "appsettings.config" };

            var workingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            */


            var modeText = args[0]?.ToLower();
            var serverFullPath = args[1];
            var saveDirectory = args[2];
            var mutexId = args[3];
            var pipeId = args[4];
            var files = args.Skip(5).ToArray();
            var workingDirectory = Path.GetDirectoryName(serverFullPath);


            var filter = "ShibugakiViewer Library (*.svl)|*.svl";

            var fileName = $"ShibugakiViewerLibrary_{DateTime.Now.ToString("yyyyMMddHHmm")}.svl";

            if (modeText == null)
            {
                return;
            }

            Mode mode;
            switch (modeText)
            {
                case "/w":
                    mode = Mode.Export;
                    break;
                case "/r":
                    mode = Mode.Import;
                    break;
                case "/c":
                    mode = Mode.Convert;
                    break;
                default:
                    return;
            }

            string modeLabel;
            switch (mode)
            {
                case Mode.Export:
                    modeLabel = "Export";
                    break;
                case Mode.Import:
                    modeLabel = "Import";
                    break;
                case Mode.Convert:
                    modeLabel = "Converter";
                    break;
                default:
                    modeLabel = "";
                    break;
            }

            Console.WriteLine("ShibugakiViewer Library " + modeLabel);
            Console.WriteLine("Processing...");

            try
            {
                using (var mutex = new Mutex(false, mutexId))
                {
                    var hasMutex = false;
                    try
                    {

                        if (!mutex.WaitOne(0, false))
                        {
                            //ミューテックス取得失敗
                            //稼働中のアプリケーションを終了

                            using (var pipeClient =
                                new NamedPipeClientStream(".", pipeId, PipeDirection.Out))
                            {
                                pipeClient.Connect(300);

                                // Read user input and send that to the client process.
                                using (var sw = new StreamWriter(pipeClient) { AutoFlush = true })
                                {
                                    sw.WriteLine("?exit");
                                }
                            }

                            if (mutex.WaitOne(10000, false))
                            {
                                hasMutex = true;
                            }
                        }
                        else
                        {
                            hasMutex = true;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                    }

                    try
                    {
                        //ライブラリの圧縮または解凍
                        switch (mode)
                        {
                            case Mode.Export:
                                Save(files, saveDirectory, filter, fileName);
                                break;
                            case Mode.Import:
                                Load(files, saveDirectory, filter);
                                break;
                            case Mode.Convert:
                                var version = 0;
                                int.TryParse(files[2], out version);
                                new Compat()
                                    .ConvertOldLibraryAsync(saveDirectory, files[0], files[1], version)
                                    .Wait();
                                break;
                        }

                        Console.WriteLine("Done");
                    }
                    catch
                    {
                    }
                    finally
                    {
                        //ミューテックスを保持していたら解放
                        if (hasMutex)
                        {
                            mutex.ReleaseMutex();
                            mutex.Close();
                        }
                    }

                }
            }
            catch
            {

            }

            try
            {

                //var path = serverFullPath;// Path.Combine(dir, serverPath);

                //アプリ起動
                var psi = new ProcessStartInfo()
                {
                    FileName = serverFullPath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                };

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception e)
            {
                var tx = e.ToString();
            }
        }

        private static void Save(string[] files, string directory, string filter, string defaultName)
        {

            var dialog = new SaveFileDialog()
            {
                Filter = filter,
                InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                FileName = defaultName,
            };
            //dialog.FileName = $"ShibugakiViewerLibrary_{DateTime.Now.ToString("yyyyMMddHHmm")}.svl";

            if (dialog.ShowDialog() == true)
            {
                var fileName = dialog.FileName;

                using (var z = ZipFile.Open(fileName, ZipArchiveMode.Update))
                {
                    foreach (var file in files)
                    {
                        var fullPath = Path.Combine(directory, file);
                        if (System.IO.File.Exists(fullPath))
                        {
                            z.CreateEntryFromFile(fullPath, file, CompressionLevel.Optimal);
                        }
                    }
                }
            }
        }

        private static void Load(string[] files, string directory, string filter)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = filter,
                InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            };

            if (dialog.ShowDialog() == true)
            {
                var fileName = dialog.FileName;

                using (var z = ZipFile.OpenRead(fileName))
                {
                    foreach (var entry in z.Entries)
                    {
                        var name = entry.FullName.ToLower();

                        if (files.Contains(name))
                        {
                            entry.ExtractToFile(Path.Combine(directory, entry.FullName), true);
                        }
                    }
                }
            }
        }


        enum Mode
        {
            Import,
            Export,
            Convert,
        }
    }
}
