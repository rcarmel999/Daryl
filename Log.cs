using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daryl
{
    internal class Log
    {
        public static bool DEBUG = false;

        internal static void L(Message message)
        {
            switch (message.Role)
            {
                case Role.User:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\b> ");
                    Console.WriteLine($"{message.Content}\n");
                    return;

                case Role.Assistant:

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\bDaryl: ");
                    Console.WriteLine($"{message.Content}");
                    Console.ForegroundColor = ConsoleColor.White;
                    return;

                case Role.System:
                    return;

                default:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"\b{System.Text.RegularExpressions.Regex.Unescape(message.Content)}");
                    Console.ForegroundColor = ConsoleColor.White;

                    return;
            }
        }

        internal static void S(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"\b{msg}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        internal static void D(string msg)
        {
            if (DEBUG)
            {
                Console.ForegroundColor= ConsoleColor.DarkYellow;
                Console.WriteLine($"\b{msg}");
                Console.ForegroundColor = ConsoleColor.White;

            }
        }

        internal static void A(string msg) 
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\b{msg}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
