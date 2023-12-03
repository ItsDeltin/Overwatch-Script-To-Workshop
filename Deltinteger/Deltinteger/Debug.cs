using System;

namespace Deltin.Deltinteger
{
    public class Log
    {
        public static LogLevel LogLevel = LogLevel.Normal;

        public Log(string name)
        {
            this.name = name;
        }
        readonly string name;

        /*
        public void Write(LogLevel logLevel, string text)
        {
            if ((int)logLevel <= (int)LogLevel)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"[{name}] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(text);
            }
        }
        */

        public void Write(LogLevel logLevel, params ColorMod[] colors)
        {
            if ((int)logLevel <= (int)LogLevel)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
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

        public Progress Progress(LogLevel logLevel, string text, int actions)
        {
            if ((int)logLevel <= (int)LogLevel)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"[{name}] ");
                Console.ForegroundColor = ConsoleColor.White;

                Console.Write(text + " ");
                var left = Console.CursorLeft;
                var top = Console.CursorTop;

                return new Progress(this, left, top, actions);
            }
            else return new Progress();
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

        public static implicit operator string(ColorMod cm) => cm.Text;
        public static implicit operator ColorMod(string value) => new ColorMod(value);

        public string Text { get; private set; }
        public ConsoleColor TextColor { get; private set; }
        public ConsoleColor BackgroundColor { get; private set; }
    }

    public enum LogLevel
    {
        Quiet = 0,
        Normal = 1,
        Verbose = 2
    }

    public class Progress
    {
        private Log log;
        private int left;
        private int top;
        private int actions;
        private int replace;
        private double lastWritten;

        public Progress(Log log, int left, int top, int actions)
        {
            this.log = log;
            this.left = left;
            this.top = top;
            this.actions = actions;

            Console.Write("0%");
        }

        public Progress()
        { }

        private int completed;

        public void ActionCompleted()
        {
            if (log == null) return;

            completed++;

            double percentageCompleted = ((double)completed / (double)actions) * 100;

            if (percentageCompleted - lastWritten >= 1)
                Write(percentageCompleted);
        }

        public void Finish()
        {
            if (log == null) return;
            Write(100);
            Console.WriteLine();
        }

        private void Write(double percentageCompleted)
        {
            Console.SetCursorPosition(left, top);
            string percentage = percentageCompleted.ToString("0") + "%";
            Console.Write(percentage + new string(' ', Math.Max(percentage.Length - replace, 0)));
            replace = percentage.Length;
            lastWritten = percentageCompleted;
        }
    }
}
