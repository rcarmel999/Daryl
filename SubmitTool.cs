using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using OpenAI.Chat;
using static Program;

namespace Daryl
{
    internal class SubmitTool : ITool
    {
        public string Name { get { return "SubmitTool"; } }

        public Function GetToolDefinition() 
        {
            return new Function (
            "SubmitTool",
            "Submit a tool for review.",
            new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["ClassName"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "The name of the c# tool class."
                    },
                    ["SourceCode"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "The tool C# source code to be reviewed. This value should be properly escapped."
                    },
                    ["NuGetPackages"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "string"
                        },
                        ["description"] = "A list of NuGet package the source code needs."
                    }

                },
                ["required"] = new JsonArray { "ClassName", "SourceCode" }
            });

        }
                

        public Message? ProcessTool(Tool tool)
        {
            if (tool.Function.Name != "SubmitTool")
                return null;
            try
            {
                var args = System.Text.RegularExpressions.Regex.Unescape(tool.Function.Arguments.ToString());
                int startIndex = args.IndexOf("\"SourceCode\":\"");
                startIndex += "\"SourceCode\":\"".Length;
                int endIndex = args.Substring(startIndex).IndexOf("}\n\"");
                string code = string.Empty;
                string newArgs = string.Empty;
                if (endIndex == -1)
                {
                    code = args.Substring(startIndex, args.Length - (startIndex + 2));
                    newArgs = args.Remove(startIndex, args.Length - (startIndex + 2));
                }
                else
                {
                    code = args.Substring(startIndex, endIndex + 1);
                    newArgs = args.Remove(startIndex, endIndex+2);
                }
                    


                

                var functionArgs = JsonSerializer.Deserialize<SaveSourceCodeParams>(newArgs);
                functionArgs.SourceCode = code;

                //startIndex = args.IndexOf("\"ClassName\":\"");
                //startIndex += "\"ClassName\":\"".Length;
                //int lastIndex = args.Substring(startIndex).IndexOf("\"");
                //string className = args.Substring(startIndex, lastIndex );





                //var functionArgs = JsonSerializer.Deserialize<SaveSourceCodeParams>(args);

                //if (functionArgs != null)
                //{
                //functionArgs.SourceCode = functionArgs!.SourceCode!.Replace(@">", ">");
                return new Message(tool, Submit(functionArgs).ToString());
                //return new Message(tool, Submit(new SaveSourceCodeParams { ClassName = className, SourceCode = code, NuGetPackages = new() }));
                //}
            }
            catch(Exception e)
            {
                Log.D($"There was a problem with your submission.  Please review and try again: {e.ToString()}");
                return new Message(tool, $"There was a problem with your submission.  Please review and try again: {e.ToString()}");
            }
           

            return null;
        }

        public static string Submit(SaveSourceCodeParams p)
        {
            if (p == null) return "No parameters given.";
            if (p.SourceCode == null) return "Source code was null";
            if (p.ClassName == null) return "ClassName null";



            // Get the tool location
            //string jsonData = System.IO.File.ReadAllText("tools.json");
            var toolClassNames = ToolHelper.ToolClasses; //System.Text.Json.JsonSerializer.Deserialize<ToolClasses>(jsonData);
            
            // Check to see if we have have a tool location for this one.
            string toolFolder = System.IO.Path.Combine("Tools", p.ClassName);

            System.IO.Directory.CreateDirectory(toolFolder);

            string projFolder = p.ClassName;

            string codeFile = System.IO.Path.Combine(toolFolder,p.ClassName + "\\" + p.ClassName + ".cs");
            

            string slnName = $"{p.ClassName}.sln";
            string slnFile = System.IO.Path.Combine(toolFolder,slnName);

            string projName = System.IO.Path.Combine(projFolder, $"{p.ClassName}.csproj");


            if (!System.IO.File.Exists(slnFile))
            {
                VersionHelper.ExecuteShellCommand($"cd {toolFolder} && dotnet new sln -n {p.ClassName} >> out.txt", true);
                VersionHelper.ExecuteShellCommand($"cd {toolFolder} && dotnet new classlib -n {p.ClassName} >> out.txt", true);
                VersionHelper.ExecuteShellCommand($"cd {toolFolder} && dotnet sln add {projName} >> out.txt", true);
                VersionHelper.ExecuteShellCommand($"cd {toolFolder}\\{projFolder} && dotnet add package OpenAI-DotNet  -v 7.2.3 >> out.txt", true);
                System.IO.File.Copy("ITool.dll", $"{toolFolder}\\{projFolder}\\ITool.dll");

                //VersionHelper.ExecuteShellCommand($@"cd {toolFolder}\{projFolder} && dotnet add reference ITool.dll", false);

                var lines = System.IO.File.ReadAllLines($"{toolFolder}\\{projName}");

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i > lines.Length - 2)
                    {
                        sb.AppendLine("<ItemGroup><Reference Include=\"ITool.dll\" /></ItemGroup>");
                    }
                    sb.AppendLine(lines[i]);
                }
                System.IO.File.WriteAllText($"{toolFolder}\\{projName}", sb.ToString());
                System.IO.File.Delete(System.IO.Path.Combine($@"{toolFolder}\{projFolder}", "Class1.cs"));
                
                    //System.IO.File.Copy(@"C:\Files\RNC\Daryl\Version\7\ITool.cs", System.IO.Path.Combine(projFolder, "ITool.cs"));

            }
            if (p.NuGetPackages != null)
            {
                foreach (var pkg in p.NuGetPackages)
                {
                    if (pkg == "Open-AI-DotNet")
                        VersionHelper.ExecuteShellCommand($"cd {toolFolder}\\{projFolder} && dotnet add package {pkg} -v 7.2.3 >> out.txt", true);
                    else
                        VersionHelper.ExecuteShellCommand($"cd {toolFolder}\\{projFolder} && dotnet add package {pkg} >> out.txt", true);
                }
            }
            
            System.IO.File.WriteAllText(codeFile, p.SourceCode);
            VersionHelper.ExecuteShellCommand($"cd {toolFolder} && dotnet resotre >> out.txt", true);
            VersionHelper.ExecuteShellCommand($"cd {toolFolder} && dotnet build > buildResult.txt", true);

            string buildResults = System.IO.File.ReadAllText(System.IO.Path.Combine(toolFolder, "buildResult.txt"));

            if (buildResults.Contains("Build FAILED."))
            {
                Console.WriteLine("Build Error...");
                return "There was a compile error.  Can you fix it?\n\n" + buildResults;
            }
            // Check for DLL.
            string dllPath = System.IO.Path.Combine($"{toolFolder}\\{projFolder}", $@"bin\Debug\net7.0\{p.ClassName}.dll");
            if (!System.IO.File.Exists(dllPath))
            {
                Console.WriteLine($"Could not find DLL {dllPath}");
                return "false";
            }
            // public 
            VersionHelper.ExecuteShellCommand($"cd {toolFolder} && dotnet publish > publish.txt", true);

            // Update the tool config
            if (!toolClassNames.toolClassNames.Contains(p.ClassName))
            {
                toolClassNames.toolClassNames.Add(p.ClassName);
                // for debugging
                System.IO.File.WriteAllText(@"..\..\..\tools.json", System.Text.Json.JsonSerializer.Serialize(toolClassNames));
                // for runtime
                System.IO.File.WriteAllText(@"tools.json", System.Text.Json.JsonSerializer.Serialize(toolClassNames));
            }
                
            

            return ToolHelper.LoadNewTool(p.ClassName).ToString();

            
        }

        public class SaveSourceCodeParams
        {
            public string? ClassName { get; set; }
            public string? SourceCode { get; set; }

            public List<string>? NuGetPackages { get; set; }
        }
    }

    
}
