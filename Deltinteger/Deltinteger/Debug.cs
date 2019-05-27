using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltin.Deltinteger
{
    public class Log
    {
        public static LogLevel LogLevel = LogLevel.Normal;

        public static object Passer(object obj)
        {
            Console.WriteLine(obj);
            return obj;
        }

        public Log(string name)
        {
            this.name = name;
        }
        readonly string name;

        public void Write(LogLevel logLevel, string text)
        {
            if ((int)logLevel <= (int)LogLevel)
                Console.WriteLine($"[{name}] {text}");
        }

        public void Write(LogLevel logLevel, params ColorMod[] colors)
        {
            if ((int)logLevel <= (int)LogLevel)
            {
                Console.Write($"[{name}] ");
                foreach (ColorMod color in colors)
                {
                    Console.BackgroundColor = color.BackgroundColor;
                    Console.ForegroundColor = color.TextColor;
                    Console.Write(color.Text);
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine();
            }
        }
    }

    public class ColorMod
    {
        public ColorMod(string text, ConsoleColor textColor = ConsoleColor.White, ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Text = text;
            TextColor = textColor;
            BackgroundColor = backgroundColor;
        }
        public string Text { get; private set; }
        public ConsoleColor TextColor { get; private set; }
        public ConsoleColor BackgroundColor { get; private set; }
    }

    public enum LogLevel
    {
        Normal = 0,
        Verbose = 1
    }
}
