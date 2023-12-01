using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daryl
{
    
    internal class VersionHelper
    {
        public const string VERSION_PATH = @"C:\files\RNC\Daryl\Version";

        //public static int GetCurrentVersion()
        //{
        //    Console.WriteLine("Getting current version");

        //    if (!System.IO.Directory.Exists(VERSION_PATH))
        //    {
        //        return 0;
        //    }
        //    else
        //    {
        //        var dir = System.IO.Directory.GetCurrentDirectory();
        //        Console.WriteLine(dir);
        //        DirectoryInfo? d = new DirectoryInfo(dir);
        //        string? name = d?.Parent?.Parent?.Parent?.Name;

        //        if (name != null)
        //        {
        //            Console.WriteLine($"{name}");
        //            int v = 0;
        //            if (int.TryParse(name, out v))
        //            {
        //                return v;
        //            }

        //        }
        //        else { Console.WriteLine("unknown"); }
        //    }
        //    return 0; 
            
        //}
        //public static int GetNextVersion()
        //{
        //    if (!System.IO.Directory.Exists(VERSION_PATH))
        //    {
        //        System.IO.Directory.CreateDirectory(VERSION_PATH);
        //    }

        //    var folders = System.IO.Directory.GetDirectories(VERSION_PATH);
        //    int nextVersion = 0;
        //    foreach ( var folder in folders )
        //    {
        //        int version = -1;
        //        if (int.TryParse(System.IO.Path.GetFileName(folder), out version))
        //        {
        //            nextVersion = System.Math.Max(nextVersion, version);
        //        }

        //    }

        //    return nextVersion + 1;
        //}

        //public static bool CopyBaseVersion(int version)
        //{
        //    string basePath = @"..\..\..\";
        //    if (version < 2)
        //    {
                
        //    }
        //    else
        //    {
        //        basePath = System.IO.Path.Combine(VERSION_PATH, (version - 1).ToString());

        //    }

        //    if (!System.IO.File.Exists(System.IO.Path.Combine( basePath,"Program_new.cs" )))
        //    {
        //        Console.WriteLine("No source code changes detected.");
        //        return false;
        //    }
        //    string newPath = System.IO.Path.Combine(VERSION_PATH, version.ToString());

        //    System.IO.Directory.CreateDirectory(newPath);

        //    List<string> files = new List<string> { "Daryl.csproj", "Daryl.sln", "history.txt" };
        //    foreach ( var file in files )
        //    {
        //        string oldFile = System.IO.Path.Combine(basePath, file);
        //        string newFile = System.IO.Path.Combine(newPath, file);
                
        //        Console.WriteLine($"Copying {oldFile} to {newFile}");

        //        System.IO.File.Copy(oldFile ,newFile );


        //    }

        //    var sourceFiles = System.IO.Directory.GetFiles(basePath, "*.cs");
        //    foreach ( var sourceFile in sourceFiles )
        //    {
        //        if (!sourceFile.EndsWith("Program.cs"))
        //        {
        //            string newFile = System.IO.Path.Combine(newPath, System.IO.Path.GetFileName(sourceFile));

        //            Console.WriteLine($"Copying {sourceFile} to {newFile}");

        //            System.IO.File.Copy(sourceFile ,newFile );
        //        }

        //    }

        //    string odFile = System.IO.Path.Combine(newPath, "Program_new.cs");
        //    string nwFile = System.IO.Path.Combine(newPath, "Program.cs");
        //    Console.WriteLine($"Renaming {odFile} to {nwFile}");
        //    System.IO.File.Move(odFile, nwFile);

        //    return true;
        //}

        //public static void CompileVersion(int version)
        //{
        //    string path = System.IO.Path.Combine(VERSION_PATH, version.ToString());

        //    Console.WriteLine("Compliing version: " + path);

        //    ExecuteShellCommand($"cd {path} && dotnet resotre", true);
        //    ExecuteShellCommand($"cd {path} && dotnet build", true);

        //}

        //public static void LaunchVersion(int version)
        //{
        //    string path = System.IO.Path.Combine(VERSION_PATH, version.ToString());
        //    path = System.IO.Path.Combine(path, @"bin\Debug\net7.0\");
        //    System.IO.Directory.SetCurrentDirectory(path);

        //    path = System.IO.Path.Combine(path, "Daryl.exe");
        //    Console.WriteLine($"Launching  V1.{version}");
        //    Console.WriteLine(path);


        //    Process.Start(path);
        //    Environment.Exit(0);

        //}

        public static void ExecuteShellCommand(string command, bool silent)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe", // Use "bash" for Linux/Mac
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(processStartInfo))
                {
                    using (StreamWriter writer = process!.StandardInput)
                    {
                        if (writer.BaseStream.CanWrite)
                        {
                           
                                writer.WriteLine(command);
                                writer.WriteLine("exit");
                            
                            
                        }
                    }

                    string result = process.StandardOutput.ReadToEnd();
                    if (!silent)
                    {
                        Console.WriteLine(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

    }
}
