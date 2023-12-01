using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenAI.Chat;
using static Program;

namespace Daryl
{
    internal class ToolHelper
    {
        internal static List<ToolLoadContext> ToolContexts = new List<ToolLoadContext>();
        internal static ToolClasses ToolClasses = new ToolClasses();


        public static List<Tool> InitTools(List<ITool> toolDefinitions)
        {
            var tools = new List<Tool>();

            foreach (var toolDefinition in toolDefinitions)
            {
                tools.Add(toolDefinition.GetToolDefinition());
            }

            return tools;

        }

        public static bool LoadNewTool(string className)
        {

            ToolHelper.UnloadTool(className);

            //Thread.Sleep(10000);

            //string jsonData = System.IO.File.ReadAllText("tools.json");
            var toolClassNames = ToolClasses; //System.Text.Json.JsonSerializer.Deserialize<ToolClasses>(jsonData);
            string toolFolder = "Tools";
            if (!string.IsNullOrEmpty(toolClassNames.toolFolder ))
                toolFolder = toolClassNames.toolFolder;

            string toolPath = System.IO.Path.Combine("Tools", className);
            string binDll = System.IO.Path.Combine(toolFolder, "Bin", $"{className}.dll");
            string binPath = System.IO.Path.Combine(toolFolder, "Bin");
            if (System.IO.Directory.Exists(toolPath))
            {


                // Check to see if this tool already exists in the bin directory.
                if (System.IO.File.Exists(binDll))
                {
                    // Create a version directory
                    int i = 0;
                    binPath = System.IO.Path.Combine(toolFolder, "Bin", $"{className}_v{i}");
                    while (System.IO.Directory.Exists(binPath))
                    {
                        binPath = System.IO.Path.Combine(toolFolder, "Bin", $"{className}_v{i++}");
                    }
                    System.IO.Directory.CreateDirectory(binPath);


                }
                else
                {
                    binPath = System.IO.Path.Combine(toolFolder, "Bin");
                }

                // copy files to bin directory
                string folderPath = System.IO.Path.Combine(System.IO.Path.Combine(toolPath, className), $@"bin\Debug\net7.0\publish");
                var files = System.IO.Directory.GetFiles(folderPath);
                foreach (var file in files)
                {
                    if (!file.Contains("ITool.dll"))
                    {
                        System.IO.File.Copy(file, System.IO.Path.Combine(binPath, System.IO.Path.GetFileName(file)), true);
                    }
                }

                string currentDir = System.IO.Directory.GetCurrentDirectory();
                string dllPath = System.IO.Path.Combine(System.IO.Path.Combine(currentDir, binPath, $"{className}.dll"));

                if (System.IO.File.Exists(dllPath))
                {
                    //var ass = Assembly.LoadFrom(dllPath);
                    string name = $"Daryl.{className}";

                    var tcx = new ToolLoadContext(System.IO.Path.Combine(currentDir, dllPath), className);
                    ToolContexts.Add(tcx);

                    //var ass = tcx.LoadFromAssemblyName(new AssemblyName(className));
                    var ass = tcx.LoadFromAssemblyPath(dllPath);
                    //t = ass.GetType(name);

                    Type? t = ass.GetType(name);

                    if (t != null)
                    {
                        ITool? tool = Activator.CreateInstance(t) as ITool;

                        if (tool != null)
                        {
                            Program.ToolDefinitions.Add(tool);
                            Program.tools.Add(tool.GetToolDefinition());

                            return true;
                        }
                    }

                }
            }
            return false;
        }

        public static List<ITool> LoadToolDefinitions()
        {
            string jsonData = System.IO.File.ReadAllText("tools.json");
            
            var toolClassNames = System.Text.Json.JsonSerializer.Deserialize<ToolClasses>(jsonData);
            

            List<ITool> toolDefinitions = new List<ITool>();
            if (toolClassNames == null)
            {
                toolClassNames = new ToolClasses { toolClassNames = new List<string>(), toolFolder = "Tools" };
            }

            if (string.IsNullOrEmpty(toolClassNames.toolFolder))
                toolClassNames.toolFolder = "Tools";
            if (toolClassNames.toolClassNames == null)
                toolClassNames.toolClassNames = new();

            ToolClasses = toolClassNames;

            Console.WriteLine($"Loading tools:\n");

            foreach (var className in toolClassNames.toolClassNames)
            {
                string name = $"Daryl.{className}";
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;

                //Console.Write($"{name}...".PadRight(30));

                string currentDir = System.IO.Directory.GetCurrentDirectory();

                Type? t = null;
                string toolPath = System.IO.Path.Combine(toolClassNames.toolFolder, className);

                string binPath = Path.Combine(toolClassNames.toolFolder, "Bin");
                string foundPath = binPath;
                //bool foundVersion = false;
                if (System.IO.Directory.Exists(toolPath))
                {
                   
                    int i = 0;
                    binPath = System.IO.Path.Combine(toolClassNames.toolFolder, "Bin", $"{className}_v{i}");
                    
                    while (System.IO.Directory.Exists(binPath))
                    {
                        //foundVersion = true;
                        foundPath = binPath;
                        binPath = System.IO.Path.Combine(toolClassNames.toolFolder, "Bin", $"{className}_v{i++}");
                    }
                    
                    if (i > 0)
                        Console.Write($"{name} V{i}...".PadRight(30));
                    else
                        Console.Write($"{name}...".PadRight(30));

                    if (Log.DEBUG)
                        Console.WriteLine();

                    string dllPath = System.IO.Path.Combine(foundPath, $"{className}.dll"); //System.IO.Path.Combine(System.IO.Path.Combine(toolPath, className), $@"bin\Debug\net7.0\{className}.dll");
                    if (System.IO.File.Exists(dllPath)) 
                    {
                        // Check to see if there are any newer versions to be loaded

                        //if (foundVersion)

                        var tcx = new ToolLoadContext(System.IO.Path.Combine(currentDir, dllPath), className);
                        ToolContexts.Add(tcx);

                        //var ass = tcx.LoadFromAssemblyName(new AssemblyName(className));
                        var ass = tcx.LoadFromAssemblyPath(System.IO.Path.Combine(currentDir, dllPath));
                        t = ass.GetType(name);
                        
                        //var ass = Assembly.LoadFrom(dllPath);

                        //t = ass.GetType(name);
                    }
                }
                else
                {
                    Console.Write($"{name}...".PadRight(30));
                    t = Type.GetType(name);
                }
                
                if (t != null)
                {
                    ITool? tool = Activator.CreateInstance(t) as ITool;

                    if (tool != null)
                    {
                        toolDefinitions.Add(tool);
                        Console.Write("\t\t\t\t[ ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("OK");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" ]");

                    }
                    else
                    {
                        WriteFail();
                    }
                }
                else
                {
                    WriteFail();
                }

            }

            return toolDefinitions;
        }

        internal static void UnloadTool(string toolName)
        {
            var tcx = ToolContexts.Find(x=>x.ToolName ==  toolName);
            if (tcx != null)
            {
                ToolContexts.Remove(tcx);

                var tn = Program.tools.Find(x=>x.Function.Name == toolName);
                if (tn != null)
                    Program.tools.Remove(tn);

                tn = null;

                var it = Program.ToolDefinitions.Find(x => x.Name == toolName);
                if (it != null)
                    Program.ToolDefinitions.Remove(it);

                it = null;

                tcx.Unload();
                tcx = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();

            }
        }
        private static void WriteFail()
        {
            Console.Write("\t\t\t[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("FAIL");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");
        }

        internal class ToolLoadContext : AssemblyLoadContext
        {
            private string assemblyPath;

            public string ToolName { get; set; }

            public ToolLoadContext(string assemblyPath, string toolName) : base(isCollectible: true)
            {
                this.assemblyPath = assemblyPath;
                ToolName = toolName;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                Log.D($"Resolving: {assemblyName}");
                

                //string nugetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
                //string packagePath = Path.Combine(nugetPath, assemblyName.Name.ToLowerInvariant(), $"{assemblyName.Version.Major.ToString()}.{assemblyName.Version.Minor.ToString()}.{assemblyName.Version.Revision.ToString()}");
                //string assemblyFilePath = Path.Combine(packagePath, "lib", "netstandard2.0", $"{assemblyName.Name}.dll");

                //if (File.Exists(assemblyFilePath))
                //{
                //    return LoadFromAssemblyPath(assemblyFilePath);
                //}

                //if (assemblyName.Name != "ITool")
                //{
                //    string dllPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assemblyPath), $"{assemblyName.Name}.dll");
                //    if (System.IO.File.Exists(dllPath))
                //        return LoadFromAssemblyPath(dllPath);

                //}
                
                if (!System.IO.File.Exists(System.IO.Path.Combine("Tools","Bin", $"{assemblyName.Name}.dll")))
                {
                    string dllPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assemblyPath), $"{assemblyName.Name}.dll");
                        if (System.IO.File.Exists(dllPath))
                    {
                       Log.D("Load " + dllPath);
                        return LoadFromAssemblyPath(dllPath);
                        
                    }
                            
                }
                else
                {
                    try
                    {
                        var ass = Default.LoadFromAssemblyName(assemblyName);
                        return ass;
                    }
                    catch (Exception)
                    {

                        string dllPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assemblyPath), $"{assemblyName.Name}.dll");
                        if (System.IO.File.Exists(dllPath))
                        {
                            Log.D("Load " + dllPath);
                            return LoadFromAssemblyPath(dllPath);

                        }
                    }
                    
                }

                return Default.LoadFromAssemblyName(assemblyName);
            }
        }
    }
}
