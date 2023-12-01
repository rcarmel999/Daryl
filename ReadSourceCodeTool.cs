using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Daryl
{
    internal class ReadSourceCodeTool : ITool
    {
        public string Name { get { return nameof(ReadSourceCodeTool); } }
        
        public Function? GetToolDefinition()
        {
            return new Function (
                 "ReadSourceCodeTool",
                "Reads the source code for a tool.",
                new JsonObject {
                    ["type"] = "object", 
                    ["properties"] = new JsonObject 
                    {
                        ["ToolName"] = new JsonObject 
                        {
                            ["type"] = "string",
                            ["description"] = "The name of the tool who's source code will be returned."
                        }

                    } 
                });
        }

        public Message? ProcessTool(Tool tool)
        {
            var functionArgs = JsonSerializer.Deserialize<ReadSourceCodeParameters>(tool.Function.Arguments.ToString());

            if (functionArgs != null)
            {

                var functionResult = ReadSourceCode(functionArgs);

                return new Message(tool, functionResult);
            }

            return null;
        }

        public static string ReadSourceCode(ReadSourceCodeParameters p)
        {
            try
            {
                if (p != null)
                    if (p.ToolName != null)
                    {


                        string toolFiles = System.IO.Path.Combine("Tools", p.ToolName, p.ToolName);
                        if (Directory.Exists(toolFiles))
                        {
                            string? codePath = System.IO.Directory.GetFiles(toolFiles).ToList().Find(x => x.EndsWith($"{p.ToolName}.cs"));
                            if (codePath != null)
                                return System.IO.File.ReadAllText(codePath);
                        }
                        else
                            return $"Could not locate a directory for tool name: {p.ToolName}";

                    }

                return "No source code found.";
            }
            catch (Exception e)
            {

                return $"There was a problem finding the source code for the tool.";
            }

           
           
        }

        public class ReadSourceCodeParameters
        {
            public string? ToolName { get; set; }
        }
    }
}
