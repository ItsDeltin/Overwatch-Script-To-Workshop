using System;
using System.Linq;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse
{
    public class PipeType : CodeType
    {
        public CodeType[] IncludedTypes { get; }
        private readonly Scope _scope = new Scope() { TagPlayerVariables = true };

        public PipeType(params CodeType[] types) : base(GetName(types))
        {
            IncludedTypes = types;

            // Get all the scopes of the included types and generate the scope name.
            string scopeName = string.Empty;
            for (int i = 0; i < IncludedTypes.Length; i++)
            {
                // Get the current type's scope.
                Scope typeScope = IncludedTypes[i].GetObjectScope();

                if (typeScope != null)
                {
                    // Cope the elements.
                    _scope.CopyAll(typeScope, null);

                    // Append to the scope name.
                    scopeName += "'" + typeScope.ErrorName + "'";
                    if (i < IncludedTypes.Length - 1) scopeName += ", ";
                }
            }

            // Set the scope's name.
            _scope.ErrorName = scopeName;
        }

        public override Scope GetObjectScope() => _scope;
        public override bool Implements(CodeType type)
        {
            foreach (CodeType included in IncludedTypes)
                if (included.Implements(type))
                    return true;
            return false;
        }
        public override bool Is(CodeType type)
        {
            foreach (CodeType included in IncludedTypes)
                if (included.Is(type))
                    return true;
            return false;
        }
        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            foreach (CodeType included in IncludedTypes)
                included.AddObjectVariablesToAssigner(reference, assigner);
        }

		public override TypeOperation GetOperation(TypeOperator op, CodeType right) {
			foreach(CodeType type in IncludedTypes) {
				var result = type.GetOperation(op, right);
				if(result != null)
					return result;
			}
			return null;
		}

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => throw new NotImplementedException();
        private static string GetName(CodeType[] types) => string.Join(" | ", types?.Select(t => t.GetName()));
    }
}
