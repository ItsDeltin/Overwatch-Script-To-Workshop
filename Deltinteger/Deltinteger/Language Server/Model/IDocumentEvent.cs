using System;
using PublishDiagnosticsParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.PublishDiagnosticsParams;

namespace Deltin.Deltinteger.LanguageServer.Model;

public interface IDocumentEvent
{
    void Publish(string workshopCode, int elementCount, PublishDiagnosticsParams[] diagnostics);

    void CompilationException(Exception exception);
}
