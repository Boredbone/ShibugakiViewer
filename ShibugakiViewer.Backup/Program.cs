using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
                //return;
            }

            //var dir = @"..\..\..\ShibugakiViewer\bin\Debug\";
            //var serverPath = @"ShibugakiViewer.exe";

            var mode = "/w";

            var serverFullPath = @"..\..\..\ShibugakiViewer\bin\Debug\ShibugakiViewer.exe";
            
            var saveDirectory = Path.Combine(System.Environment.GetFolderPath
                (Environment.SpecialFolder.LocalApplicationData),
                "Boredbone", "ShibugakiViewer");
            

            var mutexId = "79509481-1f8d-44b0-a581-d0dd4fa23710";
            var pipeId = "1af9b56b-4195-4b99-9893-1edfb2f84cbe";


            var files = new[] { "library.db", "libsettings.config", "appsettings.config" };



            var workingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            //mode = args[0];
            //serverFullPath = args[1];
            //saveDirectory = args[2];
            //files = args.Skip(3).ToArray();
            //workingDirectory = Path.GetDirectoryName(serverFullPath);


            var filter = "ShibugakiViewer Library (*.svl)|*.svl";

            var fileName = $"ShibugakiViewerLibrary_{DateTime.Now.ToString("yyyyMMddHHmm")}.svl";


            switch (mode)
            {
                case "/w":
                    Save(files, saveDirectory, filter, fileName);
                    break;
                case "/r":
                    Load(files, saveDirectory, filter);
                    break;
            }

            try
            {

                //var path = serverFullPath;// Path.Combine(dir, serverPath);

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

            try
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
            catch
            {

            }
        }

        private static void Load(string[] files, string directory, string filter)
        {

            try
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
            catch
            {

            }
        }
    }
}
