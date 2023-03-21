using System;
using System.Reactive.Disposables;
using Deltin.Deltinteger.Compiler;

namespace DS.Analysis.Diagnostics
{
    /// <summary>Manages diagnostics for a written identifier that may or may not exist.</summary>
    struct NamedDiagnosticToken
    {
        readonly FileDiagnostics diagnostics;
        readonly Token token;

        public NamedDiagnosticToken(FileDiagnostics diagnostics, Token token)
        {
            this.diagnostics = diagnostics;
            this.token = token;
        }

        /// <summary>Creates an error if the identifier exists.</summary>
        /// <param name="messageFactory">A function used to create the error message. The provided argument is the identifier's text.</param>
        /// <returns>An IDisposable used to delete the error. Will return Disposable.Empty if the identifier does not exist.</returns>
        public IDisposable Error(Func<string, string> messageFactory) => token ? diagnostics.Error(messageFactory(token), token) : Disposable.Empty;

        /// <summary>Creates an error if the identifier exists.</summary>
        /// <param name="message">The error's message.</param>
        /// <returns>An IDisposable used to delete the error. Will return Disposable.Empty if the identifier does not exist.</returns>
        public IDisposable Error(string message) => token ? diagnostics.Error(message, token) : Disposable.Empty;

        /// <summary>Gets the name of the token.</summary>
        /// <param name="name">The name is copied to this parameter. Will be `string.Empty` if the token is not valid.</param>
        /// <returns>Whether the token is valid.</returns>
        public bool GetName(out string name)
        {
            if (token)
            {
                name = token;
                return true;
            }
            name = string.Empty;
            return false;
        }
    }
}