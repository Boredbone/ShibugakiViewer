using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShibugakiViewer.Models
{
    static class SharePathOperation
    {
        /// <summary>
        /// パスをコピー
        /// </summary>
        /// <param name="path"></param>
        public static void CopyPath(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    Clipboard.SetText(path);
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// エクスプローラで開く
        /// </summary>
        /// <param name="path"></param>
        public static void OpenExplorer(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    System.Diagnostics.Process.Start
                        ("EXPLORER.EXE", $@"/select,""{path}""");
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// 他のアプリで開く
        /// </summary>
        /// <param name="path"></param>
        public static void OpenAnotheApp(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    System.Diagnostics.Process.Start
                        ("rundll32.exe", $"shell32.dll, OpenAs_RunDLL {path}");
                }
            }
            catch
            {

            }
        }

    }
}
