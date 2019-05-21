using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltin.Deltinteger
{
    public class Log
    {
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

        public void Write(string text, ConsoleColor backgroundColor = ConsoleColor.Black, ConsoleColor textColor = ConsoleColor.White)
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = textColor;
            Console.WriteLine($"[{name}] {text}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }
    }
}
