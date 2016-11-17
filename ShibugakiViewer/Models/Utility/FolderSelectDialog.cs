using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ShibugakiViewer.Models.Utility
{

    public class FolderSelectDialog : IDisposable
    {

        private CommonOpenFileDialog Dialog { get; }
        public string DefaultDirectory
        {
            get { return this.Dialog.DefaultDirectory; }
            set { this.Dialog.DefaultDirectory = value; }
        }

        public string SelectedPath => this.Dialog.FileName;


        public FolderSelectDialog()
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                AllowNonFileSystemItems = true,
                EnsurePathExists = true,
                EnsureFileExists = false,
            };
            this.Dialog = dialog;
        }

        public bool? ShowDialog()
        {
            var window = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .SingleOrDefault(w => w.IsActive);

            this.Dialog.InitialDirectory = this.DefaultDirectory;

            var result = (window == null) ? this.Dialog.ShowDialog() : this.Dialog.ShowDialog(window);

            switch (result)
            {
                case CommonFileDialogResult.Ok:
                    return true;
                case CommonFileDialogResult.Cancel:
                    return false;
                default:
                    return null;
            }
        }

        public void Dispose()
        {
            this.Dialog.Dispose();
        }
    }
}
