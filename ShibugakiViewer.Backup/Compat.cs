using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility;
using ImageLibrary.Core;
using ImageLibrary.Creation;
using ShibugakiViewer.Models;

namespace ShibugakiViewer.Backup
{
    class Compat
    {
        //public const string settingsFileName = "appsettings.config";
        //private const int settingVersion = 3;


        //private XmlSettingManager<ApplicationSettings> SettingsXml { get; set; }
        //private ApplicationSettings Settings { get; set; }

        //private string GetOldLibraryDirectory()
        //{
        //    var dir = System.Environment.GetFolderPath
        //        (Environment.SpecialFolder.LocalApplicationData);
        //
        //    var saveDirectory =
        //        Path.Combine(dir, @"Packages\60037Boredbone.MikanViewer_8weh06aq8rfkj\LocalState");
        //
        //    return saveDirectory;
        //}

        public async Task ConvertOldLibraryAsync
            (string saveDirectory, string settingFileName, string oldLibraryDirectory, int settingVersion)
        {
            if (saveDirectory == null || settingFileName == null || oldLibraryDirectory == null)
            {
                return;
            }

            Console.WriteLine($"Loading...");

            var config = new LibraryConfiguration(saveDirectory)
            {
                Concurrency = 512,
                FileTypeFilter = new HashSet<string>(),
                FileSystem = new FileSystem(),
            };

            LibraryOwner.SetConfig(config);

            using (var library = LibraryOwner.GetCurrent())
            {

                library.InitSettings();
                await library.LoadAsync();

                //ストレージに保存する設定
                var settingsXml = new XmlSettingManager<ApplicationSettings>
                    (Path.Combine(saveDirectory, settingFileName));

                var settings = settingsXml
                    .LoadXml(XmlLoadingOptions.IgnoreAllException | XmlLoadingOptions.UseBackup)
                    .Value;


                using (var locking = await library.LockAsync())
                {
                    //var saveDirectory = this.GetOldLibraryDirectory();

                    var converter = new LibraryConverter.Compat.Converter();
                    await converter.Start1(oldLibraryDirectory, settings);

                    var data = library.GetDataForConvert();
                    var count = 0;

                    await converter.Start2(data.Item1, data.Item2, data.Item3,
                        x => count = x, x =>
                        {
                            Console.CursorLeft = 0;
                            Console.Write($"Importing {x} / {count}");
                        });


                    Console.WriteLine("");

                    library.SaveSettings();
                }


                try
                {
                    settings.Version = settingVersion;
                    settingsXml.SaveXml(settings);
                }
                catch
                {

                }
            }
        }
    }
}
