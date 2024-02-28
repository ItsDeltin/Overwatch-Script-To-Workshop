#nullable enable

using System;

namespace Deltin.Deltinteger.Emulator;

public interface IEmulateLogger
{
    void Log(string text);

    public static IEmulateLogger New(Action<string> loggerFunc) => new EmulateLogger(loggerFunc);
    class EmulateLogger(Action<string> loggerFunc) : IEmulateLogger
    {
        public void Log(string text) => loggerFunc(text);
    }
}