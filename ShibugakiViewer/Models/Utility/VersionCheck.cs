using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.ServiceModel.Syndication;

namespace ShibugakiViewer.Models.Utility
{
    public class VersionCheck
    {
        public Version LastVersion { get; private set; }
        public Version CurrentVersion { get; private set; }


        private bool TryExtractVersion(string text, out Version version, out string prereleaseText)
        {
            var startIndex = 0;
            var endIndex = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsNumber(text[i]))
                {
                    startIndex = i;
                    break;
                }
            }

            for (int i = startIndex; i < text.Length; i++)
            {
                if (text[i] != '.' && !char.IsNumber(text[i]))
                {
                    endIndex = i;
                    break;
                }
            }

            var versionText = (endIndex > startIndex)
                ? text.Substring(startIndex, endIndex - startIndex) : text.Substring(startIndex);

            prereleaseText = (endIndex > startIndex)
                ? text.Substring(endIndex) : "";


            return Version.TryParse(versionText, out version);
        }

        private async Task<Version> GetLastVersionAsync(string url)
        {
            try
            {
                var settings = new XmlReaderSettings()
                {
                    Async = true,
                };

                using (var reader = XmlReader.Create(url, settings))
                {
                    var xml = await reader.ReadAsync();

                    var feed = SyndicationFeed.Load(reader);

                    foreach (var item in feed.Items)
                    {
                        Version ver;
                        string p;
                        if (item.Title?.Text != null && this.TryExtractVersion(item.Title.Text, out ver, out p))
                        {
                            return ver;
                        }
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        public async Task<bool> CheckAsync(string url)
        {
            var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.CurrentVersion = assemblyVersion;

            var last = await this.GetLastVersionAsync(url);

            this.LastVersion = last;

            if (last == null)
            {
                return false;
            }
            return (last > assemblyVersion);
        }
    }
}
