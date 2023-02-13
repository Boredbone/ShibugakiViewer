using System;
using System.Collections.Generic;
using System.Text;

namespace ShibugakiViewer.Models.Utility
{

    class FolderSelector
    {
        public List<string> SelectedItems { get; } = new List<string>();
        public string LastSelectedPath { get; private set; }

        public bool ShowDialog(string defaultPath)
        {
            this.LastSelectedPath = null;
            this.SelectedItems.Clear();

            using (var fbd = new WindowsShell.FolderSelectDialog())
            {
                if (System.IO.Directory.Exists(defaultPath))
                {
                    fbd.DefaultDirectory = defaultPath;
                }

                if (fbd.ShowDialog() == true)
                {
                    foreach (var item in fbd.SelectedItems)
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                        {
                            this.SelectedItems.Add(item);
                            this.LastSelectedPath = item;
                        }
                    }
                }
            }

            return (this.SelectedItems.Count != 0);
        }
#if false
        public bool ShowDialog2(string defaultPath)
        {
            this.LastSelectedPath = null;
            this.SelectedItems.Clear();


            string folderPath = null;
            using (var fbd = new FolderSelectDialog2())
            {
                if (System.IO.Directory.Exists(defaultPath))
                {
                    fbd.DefaultDirectory = defaultPath;
                }

                if (fbd.ShowDialog() == true)
                {
                    folderPath = fbd.SelectedPath;
                }
            }

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            try
            {
                if ((".library-ms").Equals(Path.GetExtension(folderPath)))
                {
                    //Windowsライブラリの場合

                    var libraryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        @"Microsoft\Windows\Libraries\");
                    var libraryName = Path.GetFileNameWithoutExtension
                        (folderPath.Split(Path.DirectorySeparatorChar).Last());

                    using (var shellLibrary
                        = Microsoft.WindowsAPICodePack.Shell.ShellLibrary.Load(libraryName, libraryPath, true))
                    {
                        foreach (var folder in shellLibrary)
                        {
                            this.SelectedItems.Add(folder.Path);
                            this.LastSelectedPath = folder.Path;
                        }
                    }
                }
                else
                {
                    //通常フォルダ
                    this.SelectedItems.Add(folderPath);
                    this.LastSelectedPath = folderPath;
                }
            }
            catch
            {

            }
            return true;
        }
#endif
    }
}
