using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility;
using ImageLibrary.Core;
using ShibugakiViewer.Models;

namespace ShibugakiViewer.Backup
{
    class Compat
    {
        public const string settingsFileName = "appsettings.config";
        private const int settingVersion = 3;


        private XmlSettingManager<ApplicationSettings> SettingsXml { get; set; }
        private ApplicationSettings Settings { get; set; }

        private string GetOldLibraryDirectory()
        {
            var dir = System.Environment.GetFolderPath
                (Environment.SpecialFolder.LocalApplicationData);

            var saveDirectory =
                Path.Combine(dir, @"Packages\60037Boredbone.MikanViewer_8weh06aq8rfkj\LocalState");

            return saveDirectory;
        }

        public async Task ConvertOldLibraryAsync(string xmlSaveDirectory)
        {
            var library = LibraryOwner.GetCurrent();


            //ストレージに保存する設定
            this.SettingsXml = new XmlSettingManager<ApplicationSettings>(settingsFileName);

            this.SettingsXml.Directory = xmlSaveDirectory;

            this.Settings = SettingsXml
                .LoadXml(XmlLoadingOptions.IgnoreAllException | XmlLoadingOptions.UseBackup)
                .Value;


            using (var locking = await library.LockAsync())
            {
                var saveDirectory = this.GetOldLibraryDirectory();

                var converter = new LibraryConverter.Compat.Converter();
                await converter.Start1(saveDirectory, this.Settings);

                var data = library.GetDataForConvert();
                await converter.Start2(data.Item1, data.Item2, data.Item3);
            }


            try
            {
                this.Settings.Version = settingVersion;
                this.SettingsXml.SaveXml(this.Settings);
            }
            catch
            {

            }
        }
    }
}
