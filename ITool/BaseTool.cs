using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ITool
{
    public abstract class BaseTool
    {
        private ToolPermission _permission = new ToolPermission { Permission = PermissionType.Denied };

        public BaseTool(string name)
        {
            try
            {
                var permissions = System.Text.Json.JsonSerializer.Deserialize<List<ToolPermission>>(System.IO.File.ReadAllText("toolPermissions.json"));
                if (permissions != null )
                {
                    var toolPermission = permissions.Find(x => x.ToolName == name);
                    if (toolPermission != null)
                        _permission = toolPermission;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        public bool RequestToolExecutePermission(Tool tool)
        {
            try
            {
                string args = "none";
                switch (_permission.Permission)
                {
                    case PermissionType.Denied:
                        return false;
                    case PermissionType.Granted:
                        return true;
                    case PermissionType.GrantedVerbose:
                        if (tool.Function.Arguments != null)
                            args = tool.Function.Arguments.ToString();

                        Console.WriteLine($"{tool.Function.Name}\n\n{args}");
                        return true;

                    case PermissionType.Prompt:

                        if (tool.Function.Arguments != null)
                            args = tool.Function.Arguments.ToString();

                        Console.WriteLine($"{tool.Function.Name}\n\n{args}\n\n");
                        Console.WriteLine("Grant Permission ([y]/n)> ");
                        var key = Console.ReadKey();
                        if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Y)
                        {
                            return true;
                        }
                        else
                            return false;
                }
            }
            catch (Exception)
            {

                return false;
            }
            return false;
        }


        public abstract Function? GetToolDefinition();


        public abstract Message? ProcessTool(Tool tool);

    }


    internal class ToolPermission
    {
        public string? ToolName { get; set; }
        public PermissionType Permission { get; set; }

    }

    internal enum PermissionType
    {
        Denied = 0,
        Prompt = 1,
        GrantedVerbose = 2,
        Granted = 3
        
    }
}
