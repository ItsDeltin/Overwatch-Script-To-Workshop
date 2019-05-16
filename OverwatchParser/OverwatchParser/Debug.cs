using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchParser
{
    public class Log
    {
        public Log(string name)
        {
            this.name = name;
        }
        readonly string name;

        public void Write(string text)
        {
            Console.WriteLine($"[{name}] {text}");
        }
    }
}
