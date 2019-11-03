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
            if (args.Length >= 2 && args[0] == "version")
            {
                UpdateVersion(args[1]);
                return;
            }
            if (args.Length >= 1)
            {
                if (args[0] == "file")
                {
                    ResolveFiles();
                }
                else if (args[0] == "clean")
                {
                    Clean();
                }
                else if (args[0] == "zip")
                {
                    Zip();
                }
                return;
            }

            Console.WriteLine("end");
            Console.ReadLine();
        }

        static void Zip()
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(
                @"..\..\..\Package\ShibugakiViewer", 
                @"..\..\..\Package\ShibugakiViewer.zip");
        }

        static void Clean()
        {
            var directories = Directory.GetDirectories(@"..\..\..\..\");

            var remove = new List<string>();

            foreach (var item in directories)
            {
                var dirName = Path.GetFileName(item);

                if (dirName.StartsWith("."))
                {
                    Console.WriteLine(item + " - " + dirName);
                    continue;
                }
                var hasObj = Directory.Exists(item + @"\obj");
                var hasBin = Directory.Exists(item + @"\bin");

                Console.WriteLine($"{item} - {dirName} {(hasObj ? "obj" : "-")} {(hasBin ? "bin" : "-")}");

                if (dirName == "CreatePackage")
                {
                    continue;
                }
                if (hasObj)
                {
                    remove.Add(item + @"\obj");
                }
                if (hasBin)
                {
                    if (dirName == "ShibugakiViewer")
                    {
                        if (Directory.Exists(item + @"\bin\Release"))
                        {
                            remove.Add(item + @"\bin\Release");
                        }
                    }
                    else
                    {
                        remove.Add(item + @"\bin");
                    }
                }
            }
            foreach (var item in remove.Select(x => Path.GetFullPath(x)))
            {
                Console.WriteLine(item);
                Directory.Delete(item, true);
            }
        }

        static void ResolveFiles()
        {
            var packageRootPath = @"..\..\..\Package";
            var sourceRootPath = @"..\..\..\..\ShibugakiViewer\bin\Release\netcoreapp3.0";
            var launcherSourceRootPath = @"..\..\..\..\ShibugakiViewer.Launcher\bin\Release\netcoreapp3.0";

            var launcherDestPath = packageRootPath + @"\ShibugakiViewer";
            var mainDestPath = packageRootPath + @"\ShibugakiViewer\bin";





            var mainDestAbsolutePath = Path.GetFullPath(mainDestPath);
            Console.WriteLine(mainDestAbsolutePath);

            if (Directory.Exists(packageRootPath))
            {
                Directory.Delete(packageRootPath, true);
            }
            Directory.CreateDirectory(mainDestPath);

            var installFiles = new List<string>();

            {
                var excludeFiles = new string[]
                {
                    @"\System.Reactive.xml",
                    @"\runtimes\linux-x64\native\netstandard2.0\SQLite.Interop.dll",
                    @"\runtimes\osx-x64\native\netstandard2.0\SQLite.Interop.dll",
                    @".runtimeconfig.dev.json",
                }.Select(x => x.ToLower()).ToArray();

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
                var excludeFiles = new string[]
                {
                    @".runtimeconfig.dev.json",
                }.Select(x => x.ToLower()).ToArray();

                var sourceRootAbsolutePath = Path.GetFullPath(launcherSourceRootPath);
                Console.WriteLine(sourceRootAbsolutePath);

                var files = Directory.GetFiles(launcherSourceRootPath, "*", SearchOption.AllDirectories);
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
