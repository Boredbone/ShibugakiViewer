using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace CreatePackage
{
    class Program
    {
        static void Main(string[] args)
        {
            //UpdateVersion("2.0.0");

            var packageRootPath = @"..\..\..\Package";
            var sourceRootPath = @"..\..\..\..\ShibugakiViewer\bin\Release\publish";
            var launcherSourceRootPath = @"..\..\..\..\ShibugakiViewer.Launcher\bin\Release\publish";

            var launcherDestPath = packageRootPath + @"\ShibugakiViewer";
            var mainDestPath = packageRootPath + @"\ShibugakiViewer\bin";

            var excludeFiles = new string[]
            {
                @"publish\System.Reactive.xml",
                @"publish\runtimes\linux-x64\native\netstandard2.0\SQLite.Interop.dll",
                @"publish\runtimes\osx-x64\native\netstandard2.0\SQLite.Interop.dll",
            }.Select(x => x.ToLower()).ToArray();




            var mainDestAbsolutePath = Path.GetFullPath(mainDestPath);
            Console.WriteLine(mainDestAbsolutePath);

            if (Directory.Exists(packageRootPath))
            {
                Directory.Delete(packageRootPath, true);
            }
            Directory.CreateDirectory(mainDestPath);

            var installFiles = new List<string>();

            {
                var sourceRootAbsolutePath = Path.GetFullPath(sourceRootPath);
                Console.WriteLine(sourceRootAbsolutePath);

                var files = Directory.GetFiles(sourceRootPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (excludeFiles.Any(x => file.ToLower().EndsWith(x)))
                    {
                        Console.WriteLine("-   " + file);
                    }
                    else
                    {
                        var absPath = Path.GetFullPath(file);
                        if (absPath.StartsWith(sourceRootAbsolutePath))
                        {
                            var relPath = absPath.Substring(sourceRootAbsolutePath.Length);
                            installFiles.Add(@"\bin" + relPath);

                            var destPath = Path.GetFullPath(mainDestPath + relPath);
                            Console.WriteLine(file + " : " + relPath + " : " + destPath);

                            var dir = Path.GetDirectoryName(destPath);
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            File.Copy(file, destPath);
                        }
                        else
                        {
                            Console.WriteLine("??? " + file);
                        }
                    }
                }
            }
            {
                var sourceRootAbsolutePath = Path.GetFullPath(launcherSourceRootPath);
                Console.WriteLine(sourceRootAbsolutePath);

                var files = Directory.GetFiles(launcherSourceRootPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var absPath = Path.GetFullPath(file);
                    if (absPath.StartsWith(sourceRootAbsolutePath))
                    {
                        var relPath = absPath.Substring(sourceRootAbsolutePath.Length);
                        installFiles.Add(relPath);

                        var destPath = Path.GetFullPath(launcherDestPath + relPath);
                        Console.WriteLine(file + " : " + relPath + " : " + destPath);

                        var dir = Path.GetDirectoryName(destPath);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.Copy(file, destPath);
                    }
                    else
                    {
                        Console.WriteLine("??? " + file);
                    }
                }
            }

            {
                using var fs = new FileStream(@"..\..\..\Installer\files.iss", FileMode.Create, FileAccess.Write);
                using var br = new StreamWriter(fs, Encoding.UTF8);

                br.WriteLine("[Files]");

                foreach (var item in installFiles)
                {
                    var destDir = "";
                    var filename = Path.GetFileName(item);
                    if (item.EndsWith(filename))
                    {
                        destDir = item.Substring(0, item.Length - filename.Length - 1);
                    }

                    var str = @"Source: ""..\Package\ShibugakiViewer" + item +
                        "\"; DestDir: \"{app}" + destDir + "\"; Flags: ignoreversion";

                    br.WriteLine(str);

                    Console.WriteLine(str);
                }
            }

            Console.WriteLine("end");
            Console.ReadLine();
        }

        static void UpdateVersion(string version)
        {
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
