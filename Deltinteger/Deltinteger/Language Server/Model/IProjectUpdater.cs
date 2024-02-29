namespace Deltin.Deltinteger.LanguageServer;

using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using System;

public interface IProjectUpdater
{
    void UpdateProject(Document activeModel);

    Task<DeltinScript> GetProjectCompilationAsync();

    void Lock(Action action);
}

class TimedProjectUpdater : IProjectUpdater
{
    const int TYPE_DELAY_MILLISECONDS = 50;
    readonly Task compileProjectTask;
    readonly object locker = new();
    TaskCompletionSource<object> resetTimer = new();
    TaskCompletionSource<object> requestScriptNow = new();
    TaskCompletionSource<DeltinScript> currentCompilation = new();
    bool doExit;
    Document activeDocument;

    public TimedProjectUpdater(IScriptCompiler compiler)
    {
        compileProjectTask = Task.Run(async () =>
        {
            while (!doExit)
            {
                // Wait for initial trigger.
                await resetTimer.Task;
                resetTimer = new();

                // Check if exit was requested while waiting.
                if (doExit)
                {
                    return;
                }

                // Reset current compilation if it is already set.
                if (currentCompilation.Task.IsCompleted)
                {
                    currentCompilation = new TaskCompletionSource<DeltinScript>();
                }

                Task timeOut = Task.Delay(TYPE_DELAY_MILLISECONDS);
                Task completedTask;
                while ((completedTask = await Task.WhenAny(timeOut, resetTimer.Task, requestScriptNow.Task)) == resetTimer.Task)
                {
                    resetTimer = new();
                }

                // If the delay was stopped early via requestScriptNow, reset its TaskCompletionSource.
                if (requestScriptNow.Task.IsCompleted)
                {
                    requestScriptNow = new();
                }

                Lock(() =>
                {
                    compiler.Compile(activeDocument);
                    currentCompilation.SetResult(compiler.Current());
                });
            }
        });
    }

    public async Task<DeltinScript> GetProjectCompilationAsync()
    {
        requestScriptNow.TrySetResult(null);
        return await currentCompilation.Task;
    }

    public void UpdateProject(Document activeModel)
    {
        activeDocument = activeModel;
        resetTimer.TrySetResult(null);
    }

    public void Lock(Action action)
    {
        lock (locker) action();
    }
}

class ProjectUpdater : IProjectUpdater
{
    readonly IScriptCompiler compiler;

    public ProjectUpdater(IScriptCompiler compiler)
    {
        this.compiler = compiler;
    }

    public Task<DeltinScript> GetProjectCompilationAsync() => Task.FromResult(compiler.Current());
    public void UpdateProject(Document activeModel) => compiler.Compile(activeModel);
    public void Lock(Action action) => action();
}