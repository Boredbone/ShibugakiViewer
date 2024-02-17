using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ShibugakiViewer.Models.Utility
{
    class FolderSelector
    {
        public List<string> SelectedItems { get; } = new List<string>();
        public string? LastSelectedPath { get; private set; }

        public bool ShowDialog(string defaultPath)
        {
            this.LastSelectedPath = null;
            this.SelectedItems.Clear();

            var window = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .SingleOrDefault(w => w.IsActive);

            var ofd = new Microsoft.Win32.OpenFolderDialog();
            ofd.Multiselect = true;

            if (!string.IsNullOrWhiteSpace(defaultPath)
                && System.IO.Directory.Exists(defaultPath))
            {
                ofd.DefaultDirectory = defaultPath;
            }

            var result = (window == null) ? ofd.ShowDialog() : ofd.ShowDialog(window);
            if (result != true)
            {
                return false;
            }

            foreach (var item in ofd.FolderNames)
            {
                string? folderPath = null;
                if ((".library-ms").Equals(Path.GetExtension(item)))
                {
                    var libraryPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        @"Microsoft\Windows\Libraries\");
                    var libraryName = Path.GetFileNameWithoutExtension
                        (item.Split(Path.DirectorySeparatorChar).Last());

                    using (var shellLibrary = WindowsShell.ShellLibrary.Load(libraryName, libraryPath, true))
                    {
                        foreach (var folder in shellLibrary)
                        {
                            folderPath = folder.Path;
                        }
                    }
                }
                else
                {
                    folderPath = item;
                }
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    this.SelectedItems.Add(item);
                    this.LastSelectedPath = item;
                }
            }
            return (this.SelectedItems.Count != 0);
        }
    }
}
