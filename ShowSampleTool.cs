using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Daryl
{
    internal class ShowSampleTool : ITool
    {
        public string Name { get { return "ShowSampleTool"; } }

        public Function? GetToolDefinition()
        {
            return new Function(
                "ShowSampleTool",
                "Shows a sample of how to implement a Tool",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject()
                });
        }

        public Message? ProcessTool(Tool tool)
        {
            var functionResult = ShowSample();
            return new Message(tool, functionResult);
        }

        public string ShowSample()
        {
            return System.IO.File.ReadAllText(@"SampleTool.txt");
        }
    }
}
