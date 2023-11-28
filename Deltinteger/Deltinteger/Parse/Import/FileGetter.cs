using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.LanguageServer.Settings;
using Deltin.Deltinteger.Compiler;
using System.IO;

#nullable enable

namespace Deltin.Deltinteger.Parse;

public interface IFileGetter
{
    /// <summary>Gets an OSTW document from the specified uri.</summary>
    Document? GetScript(Uri uri);

    /// <summary>Gets the content of a file via its uri.</summary>
    string? GetContent(Uri uri);

    bool Exists(Uri uri);
}


class LsFileGetter : IFileGetter
{
    readonly DocumentHandler _documentHandler;
    readonly IParserSettingsResolver _settingsResolver;
    // Importing scripts not being edited.
    readonly List<ImportedScript> _importedFiles = new List<ImportedScript>();

    public LsFileGetter(DocumentHandler documentHandler, IParserSettingsResolver settingsResolver)
    {
        _documentHandler = documentHandler;
        _settingsResolver = settingsResolver;
    }

    public Document? GetScript(Uri uri)
    {
        // Get the content of the script being obtained.
        var doc = _documentHandler?.GetDocuments().FirstOrDefault(td => td.Uri.Compare(uri));
        if (doc != null) return doc;

        ImportedScript importedFile = GetImportedFile(uri);
        importedFile.Update();
        return importedFile.Document;
    }

    public string? GetContent(Uri uri)
    {
        var file = GetImportedFile(uri);
        file.Update();
        return file.Content;
    }

    public bool Exists(Uri uri)
    {
        return _importedFiles.Any(f => f.Uri.Compare(uri)) || File.Exists(uri.LocalPath);
    }

    private ImportedScript GetImportedFile(Uri uri)
    {
        foreach (ImportedScript importedFile in _importedFiles)
            if (importedFile.Uri == uri)
                return importedFile;
        var newImportedFile = new ImportedScript(uri, _settingsResolver);
        _importedFiles.Add(newImportedFile);
        return newImportedFile;
    }
}

class WebFileGetter : IFileGetter
{
    // Unlike LsFileGetter, WebFileGetter has all of the scripts available
    // right away. Since no scripts need to be created here we do not need
    // an IParserSettingsResolver
    readonly DocumentHandler _documentHandler;

    public WebFileGetter(DocumentHandler documentHandler)
    {
        _documentHandler = documentHandler;
    }

    public string? GetContent(Uri uri) => _documentHandler.TextDocumentFromUri(uri).Content;

    public Document? GetScript(Uri uri) => _documentHandler.TextDocumentFromUri(uri);

    public bool Exists(Uri uri)
    {
        bool result = _documentHandler.TextDocumentFromUri(uri) is not null;

        if (!result)
        {
            OstwJavascript.ConsoleLog($"Uri '{uri}' doesn't exists (available: {string.Join(", ", _documentHandler.GetDocuments().Select(d => d.Uri.ToString()))})");
        }

        return result;
    }
}