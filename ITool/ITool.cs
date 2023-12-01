using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daryl
{
    public interface ITool
    {
        string Name { get; }

        Function? GetToolDefinition();

        Message? ProcessTool(Tool tool);
    }
}
