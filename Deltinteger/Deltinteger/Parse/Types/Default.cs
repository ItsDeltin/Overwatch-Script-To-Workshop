using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class DefaultType : CodeType
    {
        public static DefaultType Instance { get; } = new DefaultType();

        private DefaultType() : base("define")
        {
            CanBeDeleted = false;
            CanBeExtended = false;
            Default = true;
        }
        
        public override Scope GetObjectScope(DeltinScript translateInfo) => translateInfo.PlayerVariableScope;
        public override CompletionItem GetCompletion() => throw new NotImplementedException();
    }
}