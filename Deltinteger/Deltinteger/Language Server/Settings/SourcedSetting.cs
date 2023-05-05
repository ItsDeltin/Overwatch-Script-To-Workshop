namespace Deltin.Deltinteger.LanguageServer.Settings;
using System;

#nullable enable
public record struct SourcedSettings<T>(Uri? Uri, T Settings);