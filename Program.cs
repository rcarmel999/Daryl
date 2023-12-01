using OpenAI.Chat;
using OpenAI;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;
using OpenAI.Models;
using Daryl;
using System.Reflection;
using Message = OpenAI.Chat.Message;
using System.Speech.Synthesis;
using System.Runtime.Intrinsics;
using System.Security;
using System.Xml.Linq;
using System.Net.Http.Headers;
using System;

internal class Program
{
    private const string VERSION = "V1.7";

    private const string key = "";
    private const string HISTORY_FILE = @"..\..\..\history.txt";
    private static OpenAIClient? api = null;
    private static Model model = new Model("gpt-4-1106-preview", "openai");

    public static List<ITool> ToolDefinitions = new();
    public static List<Tool> tools = new();

    public static List<ToolPermission> ToolPermissions = new();

    private static async Task Main(string[] args)
    {
        Log.DEBUG = true;

        Console.Clear();
        ToolDefinitions = ToolHelper.LoadToolDefinitions();

        tools = ToolHelper.InitTools(ToolDefinitions);
        
        InitToolPermissions();

        HttpClient hc = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(20) });

        hc.Timeout = new TimeSpan(0, 5, 0);

        api = new OpenAIClient(key, null, hc);
        if (api == null)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Error initing API Client");
            return;
        }

        List<Message> messages = InitMessages();

        using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
        {
            InitSpeech(synthesizer);

            synthesizer.Speak(DisplayVersion().Replace("V", "Version "));
            synthesizer.Rate = 2;

            messages = LoadHistory(messages);

            DisplayHistory(messages);

            string? text = PromptUser();

            while (text != null)
            {
                
                if (string.IsNullOrEmpty(text))
                {
                    text = PromptUser();
                    continue;

                }

                if (HandleConsoleCommand(text, messages, synthesizer) == true)
                {
                    text = PromptUser();
                    continue;
                }

                Console.WriteLine();

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                Task spinnerTask = StartSpinner(cancellationTokenSource.Token);

                messages.Add(new Message(Role.User, text));
                var chatRequest = new ChatRequest(messages, tools: tools, toolChoice: "auto", model: model);
                var result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

                await ProcessResult(result, messages, tools, ToolDefinitions, synthesizer, cancellationTokenSource);

                cancellationTokenSource.Cancel();
                spinnerTask.Wait();
                Console.Write("\b");

                text = PromptUser();
            }



        }


    }

    private static void InitToolPermissions()
    {
        try
        {
            var permissions = System.Text.Json.JsonSerializer.Deserialize<List<ToolPermission>>(System.IO.File.ReadAllText("toolPermissions.json"));
            if (permissions != null)
            {
                ToolPermissions = permissions;
            }
        }
        catch (Exception)
        {

            throw;
        }
    }

    static async Task StartSpinner(CancellationToken token)
    {
        string[] spinner = new string[] { "|", "/", "-", "\\" };
        int counter = 0;
        Console.ForegroundColor = ConsoleColor.Yellow;
        while (!token.IsCancellationRequested)
        {
            Console.Write(spinner[counter % spinner.Length]);
            await Task.Delay(100);  // Adjust the delay to control the speed of the spinner
            Console.Write("\b");    // Backspace to remove the previous spinner character
            counter++;
        }
        Console.ForegroundColor = ConsoleColor.White;
    }
    private static void InitSpeech(SpeechSynthesizer synthesizer)
    {
        synthesizer.Volume = 100;  // 0...100
        synthesizer.Rate = 1;      // -10...10
        synthesizer.SelectVoice("Microsoft David Desktop");
    }

    private static string? PromptUser()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("> ");
        string? text = Console.ReadLine();
        return text;
    }

    private static void DisplayHistory(List<Message> messages)
    {
        foreach (var message in messages)
        {
            if (message.Role == Role.User)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("> ");
                Console.WriteLine($"{message.Content}\n");
                //Console.WriteLine("\nProcessing request...\n");
                

            }
            else if (message.Role == Role.Assistant)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("Daryl: ");
                Console.WriteLine($"{message.Content}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (message.Role == Role.System)
                continue;
            else
                Console.WriteLine($"{message.Content}");


        }
    }

    private static List<Message> InitMessages()
    {
        return new List<Message>
        {
           new Message(Role.System, "You are an AI assistant named Daryl.  You help me write tools.  When I ask you for a new tool, you must first review the sample tool (use ShowSampleTool), next write the requested tool following the sample, and lastly submit your tool (SubmitTool). When asked to modify and resubmit a tool, you must never change the name of the tool from its original name.  When dealing with build errors, only try 3 times before asking me for help.  Also please do not constantly remind the user that you are an AI and that you don't have feelings.  Since you are my personal assistant you should address me often in your responses.  My name is Richard.  Keep your answers short."),
        };
    }

    private static List<Message> LoadHistory(List<Message> messages)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Load history? (y/n)");

        var c = Console.ReadKey();

        if (c.Key == ConsoleKey.Y || c.Key == ConsoleKey.Enter)
        {
            try
            {
                var m = System.Text.Json.JsonSerializer.Deserialize<List<Message>>(System.IO.File.ReadAllText(HISTORY_FILE));
                if (m != null)
                    messages = m;
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("No history found...\n");
            }
        }

        Console.WriteLine();
        return messages;
    }

    private static string  DisplayVersion()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        string v = $"\n\nDaryl {VERSION} online.\n\n";
        Log.S(v);
        
        return v;
        
    }

    private static bool HandleConsoleCommand(string text, List<Message> messages, SpeechSynthesizer synthesizer)
    {
        if (!text.StartsWith("/"))
            return false;

        var args = text.Split(' ');
        string cmd = args[0];

        switch (cmd)
        {
            case "/q":
                synthesizer.SpeakAsyncCancelAll();
                break;

            case "/s":
                System.IO.File.WriteAllText(HISTORY_FILE, System.Text.Json.JsonSerializer.Serialize(messages));
                Log.S("History Saved...\n");

                break;
            
            case "/u":
                ToolHelper.UnloadTool(text.Replace("/u ", ""));
                break;

            case "/grant":
                GrantToolPermission(text);
                break;
            case "/deny":
                DenyToolPermission(text);
                break;

            case "/list":
                foreach (var t in ToolDefinitions)
                {
                    string perm = "None";
                    var tp = ToolPermissions.Find(x => x.ToolName == t.Name);
                    if (tp != null)
                        perm = tp.Permission.ToString();

                    Log.S($"{t.Name.PadRight(30)}[ {perm} ]");

                }
                break;
            case "/unregister":
                
                break;

            case "/truncate":
                if (ArgsChecker(1, args))
                {
                    var msgs = InitMessages();
                    messages.Clear();
                    messages.AddRange(msgs);
                }
                break;
            default:
                break;
        }
       


        //if (text == "/c")
        //{
        //    int v = VersionHelper.GetNextVersion();
        //    if (!VersionHelper.CopyBaseVersion(v))
        //        return true;
        //    VersionHelper.CompileVersion(v);
        //    VersionHelper.LaunchVersion(v);
        //    return true;
        //}

        return true;
    }
    private static bool ArgsChecker(int num, string[] args)
    {
        if (args.Length < num)
        {
            Log.S("Not enough arguments.");
            return false;
        }
        else return true;
    }
    private static void DenyToolPermission(string text)
    {
        var args = text.Split(' ');
        if (args.Length > 0)
        {
            string toolName = args[1];
            PermissionType pt = PermissionType.Denied;
            var t = ToolPermissions.Find(x => x.ToolName == toolName);
            if (t != null)
            {
                t.Permission = pt;

            }
            else
                ToolPermissions.Add(new ToolPermission { ToolName = toolName, Permission = pt });

            System.IO.File.WriteAllText("toolPermissions.json", System.Text.Json.JsonSerializer.Serialize(ToolPermissions));
            Log.S("Permission set.");
        }
        else
            Log.S("Not enough arguments.");
    }

    private static void GrantToolPermission(string text)
    {
        var args = text.Split(' ');
        if (args.Length > 2)
        {
            string toolName = args[1];
            PermissionType pt = (PermissionType)Enum.Parse(typeof(PermissionType), args[2]);
            var t = ToolPermissions.Find(x => x.ToolName == toolName);
            if (t != null)
            {
                t.Permission = pt;

            }
            else
                ToolPermissions.Add(new ToolPermission { ToolName = toolName, Permission = pt });

            System.IO.File.WriteAllText("toolPermissions.json", System.Text.Json.JsonSerializer.Serialize(ToolPermissions));
            Log.S("Permission set.");
        }
        else
            Log.S("Not enough arguments.");
    }

    static async Task ProcessResult(ChatResponse result, List<Message> messages, List<Tool> tools, List<ITool> toolDefs, SpeechSynthesizer synthesizer, CancellationTokenSource cancellationTokenSource)
    {


        messages.Add(result.FirstChoice.Message);

        //Console.ForegroundColor = ConsoleColor.Blue;

        //Console.WriteLine($"\b{result.FirstChoice.Message.Role}: {result.FirstChoice.Message.Content} | Finish Reason: {result.FirstChoice.FinishReason}");
        Log.L(result.FirstChoice.Message);
        Log.D(result.FirstChoice.FinishReason);
        
        Speak(result, synthesizer);

        if (result.FirstChoice.Message.ToolCalls != null)
        {
            foreach (var toolCall in result.FirstChoice.Message.ToolCalls)
            {
                ITool t = toolDefs.Find(x => x.Name == toolCall.Function.Name);
                if (t != null)
                {
                    Log.S(t.Name);

                    if (RequestToolExecutePermission(toolCall, cancellationTokenSource))
                    {
                        Message? m = t.ProcessTool(toolCall);
                        if (m != null)
                        {
                            messages.Add(m);
                            Log.L(m);
                        }
                    }
                    else
                    {
                        messages.Add(new Message(toolCall, "Richard denied access to this tool."));
                    }
                }


            }
            ChatRequest newChatRequest = new ChatRequest(messages, tools: tools, toolChoice: "auto", model: model);

            var newResult = await api!.ChatEndpoint.GetCompletionAsync(newChatRequest);

            await ProcessResult(newResult, messages, tools, toolDefs, synthesizer, cancellationTokenSource); // Recursively process the new result
        }


    }

    private static void Speak(ChatResponse result, SpeechSynthesizer synthesizer)
    {
        try
        {
            synthesizer.SpeakAsyncCancelAll();

            string msg = result.FirstChoice.Message.Content.ToString();

            synthesizer.SpeakAsync(msg);

        }
        catch (Exception) { }
    }

    public static bool RequestToolExecutePermission(Tool tool, CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            
            string args = "none";

            if (ToolPermissions == null)
            {
                Log.S("Tool permissions are not init.  Using Prompt Permissions");
                cancellationTokenSource.Cancel();
                return PromptPermission(tool, args);
            }    

            var permission = ToolPermissions.Find(x => x.ToolName == tool.Function.Name);
            if (permission == null)
            {
                Log.S("Could not find permission for this tool.  Using Prompt Permissions");
                cancellationTokenSource.Cancel();
                return PromptPermission(tool, args);
            }

            switch (permission.Permission)
            {
                case PermissionType.Denied:
                    return false;
                case PermissionType.Granted:
                    return true;
                case PermissionType.GrantedVerbose:
                    if (tool.Function.Arguments != null)
                        args = tool.Function.Arguments.ToString();

                    Log.S($"{tool.Function.Name}\n\n{args}");
                    return true;

                case PermissionType.Prompt:
                    cancellationTokenSource.Cancel();
                    return PromptPermission(tool,  args);
            }
        }
        catch (Exception ex)
        {
            Log.S($"\nError checking tool permissions: {ex.ToString()}.  Permission Denied.\n");
            return false;
        }
        return false;
    }

    private static bool PromptPermission(Tool tool, string args)
    {
        if (tool.Function.Arguments != null)
            args = tool.Function.Arguments.ToString();

        Log.A($"{tool.Function.Name}\nArgs:\n{args}\n\n");
        Log.A("Grant Permission ([y]/n)");
        Log.A("> ");
        var key = Console.ReadKey();
        if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Y)
        {
            return true;
        }
        else
            return false;
    }

    public class ToolClasses
    {
        public string? toolFolder { get; set; }
        
        public List<string>? toolClassNames { get; set; }
    }
}

public class Tools
{
    public static string ReadSourceCode()
    {

        return System.IO.File.ReadAllText(@"..\..\..\Program.cs");
    }

    public class SaveSourceCodeParams
    {
        public string? SourceCode { get; set; }
    }
    public static bool SaveSourceCode(SaveSourceCodeParams p)
    {
        System.IO.File.WriteAllText(@"..\..\..\Program_new.cs", p.SourceCode);
        return true;
    }



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
