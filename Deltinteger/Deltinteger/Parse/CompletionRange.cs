using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class CompletionRange
    {
        public Scope Scope { get; }
        public DocRange Range { get; }
        public bool Priority { get; }

        public CompletionRange(Scope scope, DocRange range, bool priority = false)
        {
            Priority = priority;
            Scope = scope;
            Range = range;
        }
    }
}