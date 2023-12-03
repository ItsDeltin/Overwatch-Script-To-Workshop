using System;

namespace Deltin.Deltinteger.LanguageServer.Model;

public interface ILangLogger
{
    public static readonly ILangLogger Default = new Empty();

    void LogMessage(string text);

    public static ILangLogger New(Action<string> logger) => new LangLogger(logger);

    record LangLogger(Action<string> Logger) : ILangLogger
    {
        public void LogMessage(string text) => Logger(text);
    }

    record Empty() : ILangLogger
    {
        public void LogMessage(string text) { }
    }
}