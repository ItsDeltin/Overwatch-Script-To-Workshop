#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Deltin.Deltinteger;

static class ErrorReport
{
    static readonly Queue<string> messages = new();
    static IErrorReporter? errorReporter;

    public static void Add(string message)
    {
        Debug.WriteLine(message);

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

    public static T TryOr<T>(T or, Func<T> func)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            Add(ex.ToString());
            return or;
        }
    }

    public static async Task<T> TryOrDefaultAsync<T>(Func<Task<T>> func) where T : new()
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            Add(ex.ToString());
            return new();
        }
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