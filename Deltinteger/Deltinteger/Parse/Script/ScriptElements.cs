using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptElements
    {
        // Lists
        readonly List<TypeArgCall> _typeArgCalls = new List<TypeArgCall>();
        readonly Dictionary<IDeclarationKey, List<DeclarationCall>> _declarationCalls = new Dictionary<IDeclarationKey, List<DeclarationCall>>();
        readonly List<IVariable> _staticVariables = new List<IVariable>();

        // Public reading
        public IReadOnlyList<TypeArgCall> TypeArgCalls => _typeArgCalls;
        public IReadOnlyDictionary<IDeclarationKey, List<DeclarationCall>> DeclarationCalls => _declarationCalls;
        public IReadOnlyList<IVariable> StaticVariables => _staticVariables;

        // Public adding
        public void AddTypeArgCall(TypeArgCall typeArgCall) => _typeArgCalls.Add(typeArgCall);
        public void AddDeclarationCall(IDeclarationKey key, DeclarationCall declarationCall) => _declarationCalls.GetValueOrAddKey(key).Add(declarationCall);
        public void AddStaticVariable(IVariable variable) => _staticVariables.Add(variable);

        public (IDeclarationKey key, DocRange range) KeyFromPosition(DocPos position)
        {
            foreach (var callList in _declarationCalls)
                foreach (var pair in callList.Value)
                    if (pair.CallRange.IsInside(position))
                        return new(callList.Key, pair.CallRange);

            return new(null, null);
        }
    }
}