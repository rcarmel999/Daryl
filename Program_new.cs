using OpenAI.Chat;
using OpenAI;
using System.Text.Json.Nodes;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading;
using Google.Apis.Util.Store;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

internal class Program
{
    private const string key = "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";
    private const string HISTORY_FILE = @"..\..\..\history.txt";
    private static OpenAIClient? api = null;
    static string[] Scopes = { GmailService.Scope.GmailReadonly };
    static string ApplicationName = "Daryl AI";

    private static async Task Main(string[] args)
    {
        Console.Clear();
        HttpClient hc = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(20) });

        hc.Timeout = new TimeSpan(0, 5, 0);

        api = new OpenAIClient(key,null, hc);
        if (api == null)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Error initing API Client");
            return;
        }
        
        var messages = new List<Message>
        {
           new Message(Role.System, "You are self-modifying AI assistant named Daryl.  You can access your own source code, make modifications as per the user request, and save the results to disk.  You must always save the entire source file."),
        };

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"\n\nDaryl V1.{VersionHelper.GetCurrentVersion()} online.\n\n");
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
            catch {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("No history found...\n");
            }
        }

        Console.WriteLine();
        
        foreach (var message in messages)
        {
            if (message.Role == Role.User)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("> ");
                Console.WriteLine($"{message.Content}");
                Console.WriteLine("\nProcessing request...\n");

            }
            else if (message.Role == Role.Assistant)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("Daryl: ");
                Console.WriteLine($"{message.Content}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
                Console.WriteLine($"{message.Content}");


        }
        
        var tools = new List<Tool>
        {
            new Function("ReadUnreadGmailMessages","Reads Unread messages from the associated Gmail Account.", new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() })
        };
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

    public class ReadUnreadGmailMessagesParams
    {
        public string? user { get; set; }
    }
    public static bool ReadUnreadGmailMessages(ReadUnreadGmailMessagesParams p)
    {
        UserCredential credential;
        using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
        {
            string credPath = "token.json";
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
        }

        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });

        UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List(p.user);
        request.Q = "is:unread";

        ListMessagesResponse response = request.Execute();
        Console.WriteLine("Unread messages: " + response.Messages.Count);
        foreach (var msg in response.Messages)
        {
            Message message = service.Users.Messages.Get(p.user, msg.Id).Execute();
            Console.WriteLine("Message: " + message.Snippet);
        }

        return true;
    }
}