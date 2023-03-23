namespace DS.Analysis.Statements;
using System;

interface IStatement
{
    StatementSource? AddSourceToContext() => null;
}

interface IDisposableStatement : IStatement, IDisposable { }