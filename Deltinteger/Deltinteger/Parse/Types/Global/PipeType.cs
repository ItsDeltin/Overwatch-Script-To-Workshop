using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Workshop;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse
{
    public class PipeType : CodeType
    {
        public CodeType[] IncludedTypes { get; }
        public Lazy<Scope> _scope;

        public PipeType(params CodeType[] types) : base(GetName(types))
        {
            IncludedTypes = types;
            Operations = new PipeTypeOperatorInfo(this);

            _scope = new Lazy<Scope>(() => {
                Scope scope = new Scope() { TagPlayerVariables = true };

                // Get all the scopes of the included types and generate the scope name.
                string scopeName = string.Empty;
                for (int i = 0; i < IncludedTypes.Length; i++)
                {
                    // Get the current type's scope.
                    Scope typeScope = IncludedTypes[i].GetObjectScope();

                    if (typeScope != null)
                    {
                        // Cope the elements.
                        scope.CopyAll(typeScope);

                        // Append to the scope name.
                        scopeName += "'" + typeScope.ErrorName + "'";
                        if (i < IncludedTypes.Length - 1) scopeName += ", ";
                    }
                }

                // Set the scope's name.
                scope.ErrorName = scopeName;

                return scope;
            });
        }

        public override Scope GetObjectScope() => _scope.Value;
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
        public override CodeType[] UnionTypes() => IncludedTypes;
        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            foreach (CodeType included in IncludedTypes)
                included.AddObjectVariablesToAssigner(toWorkshop, reference, assigner);
        }
        public override AnonymousType[] ExtractAnonymousTypes()
        {
            var types = new HashSet<AnonymousType>();

            foreach (var type in IncludedTypes)
                foreach (var extractedUnionType in type.ExtractAnonymousTypes())
                    types.Add(extractedUnionType);

            return types.ToArray();
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => throw new NotImplementedException();
        private static string GetName(CodeType[] types) => string.Join(" | ", types?.Select(t => t.GetNameOrAny()));
    }

    public class PipeTypeOperatorInfo : TypeOperatorInfo
    {
        private readonly PipeType _pipeType;

        public PipeTypeOperatorInfo(PipeType type) : base(type)
        {
            _pipeType = type;
        }

        public override ITypeOperation GetOperation(TypeOperator op, CodeType right) => GetOperation(() => base.GetOperation(op, right), toi => toi.GetOperation(op, right));
        public override IUnaryTypeOperation GetOperation(UnaryTypeOperator op) => GetOperation(() => base.GetOperation(op), toi => toi.GetOperation(op));
        public override IAssignmentOperation GetOperation(AssignmentOperator op, CodeType value) => GetOperation(() => base.GetOperation(op, value), toi => toi.GetOperation(op, value));

        T GetOperation<T>(Func<T> getBase, Func<TypeOperatorInfo, T> fallback)
        {
            var baseResult = getBase();
            if (baseResult != null) return baseResult;

            foreach (var type in _pipeType.IncludedTypes)
            {
                var typeOperator = fallback(type.Operations);
                if (typeOperator != null) return typeOperator;
            }

            return default(T);
        }
    }
}
