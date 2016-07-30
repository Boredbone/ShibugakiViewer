using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.Creation;
using ImageLibrary.File;

namespace ImageLibrary.Core
{
    /// <summary>
    /// 現在有効なライブラリを保持するシングルトン
    /// </summary>
    public class LibraryOwner
    {
        private static Library Instance { get; set; }

        private static ILibraryConfiguration defaultConfig;

        public static Library GetCurrent()
        {
            if (Instance == null)
            {
                Instance = new Library(defaultConfig);
            }
            return Instance;
        }

        public static void SetConfig(ILibraryConfiguration config)
        {
            defaultConfig = config;
        }

        public static void Reset()
        {
            Instance?.Dispose();
            Instance = null;
        }
    }

}
