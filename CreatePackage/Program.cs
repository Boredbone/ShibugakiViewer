using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace CreatePackage
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var version = "1.4.3";
            
            ReplaceCsprojVersion("ShibugakiViewer", version);
            ReplaceCsprojVersion("ShibugakiViewer.Launcher", version);
            ReplaceCsprojVersion("ShibugakiViewer.Backup", version);
            ReplaceCsprojVersion("ShibugakiViewer.Settings", version);
            ReplaceCsprojVersion("Database", version);
            ReplaceCsprojVersion("ImageLibrary", version);
            
            ReplaceVersion("../../../../ShibugakiViewer.Launcher.Net45/Properties/AssemblyInfo.cs",
                "assembly: AssemblyVersion\\(\".+\\.0\"\\)", $"assembly: AssemblyVersion(\"{version}.0\")");
            
            ReplaceVersion($@"../../../Installer/installer.iss",
                "#define MyAppVersion \".+\"", $"#define MyAppVersion \"{version}\"");
                

            Console.WriteLine("end");
            Console.ReadLine();
        }

        static bool ReplaceCsprojVersion(string project, string version)
        {
            return ReplaceVersion($@"../../../../{project}/{project}.csproj",
                "<Version>.+</Version>", $"<Version>{version}</Version>");

        }

        static bool ReplaceVersion(string path, string pattern, string replacement)
        {
            var encoding = Encoding.ASCII;
            var fileInfo = new FileInfo(path);
            var text = "";

            using (var reader = new Hnx8.ReadJEnc.FileReader(fileInfo))
            {
                encoding = reader.Read(fileInfo).GetEncoding();
                text = reader.Text;
            }
            //Console.WriteLine($"encoding={encoding}");
            //Console.WriteLine(text);

            var regex = new Regex(pattern);

            var newText = regex.Replace(text, replacement, 1);

            {
                using var sw = new StreamWriter(path + "2", false, encoding);
                sw.Write(newText);
            }
            return true;
        }
    }
}
