using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public interface IVariableResolveErrorHandler
    {
        void Error(string message, DocRange errorRange);
    }

    class VariableResolveErrorHandler : IVariableResolveErrorHandler
    {
        readonly FileDiagnostics _diagnostics;

        public VariableResolveErrorHandler(FileDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public void Error(string message, DocRange errorRange) => _diagnostics.Error(message, errorRange);
    }
}