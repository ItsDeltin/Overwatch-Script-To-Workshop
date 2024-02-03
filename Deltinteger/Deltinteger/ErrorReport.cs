#nullable enable
using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger;

static class ErrorReport
{
    static readonly Queue<string> messages = new();
    static IErrorReporter? errorReporter;

    public static void Add(string message)
    {
        if (errorReporter is null)
            messages.Enqueue(message);
        else
            errorReporter.OnError(message);
    }

    public static void FlushQueuedMessages(IErrorReporter onError)
    {
        while (messages.Count > 0)
        {
            onError.OnError(messages.Dequeue());
        }
        errorReporter = onError;
    }
}

interface IErrorReporter
{
    void OnError(string message);

    public static IErrorReporter New(Action<string> onError) => new ErrorReporter(onError);

    record ErrorReporter(Action<string> OnErrorFunc) : IErrorReporter
    {
        public void OnError(string message) => OnErrorFunc(message);
    }
}