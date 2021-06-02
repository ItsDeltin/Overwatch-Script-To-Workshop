using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptElements
    {
        // Lists
        readonly List<DefinedMethodProvider> _definedMethods = new List<DefinedMethodProvider>();
        readonly List<DefinedClassInitializer> _definedClasses = new List<DefinedClassInitializer>();
        readonly List<DefinedStructInitializer> _definedStructs = new List<DefinedStructInitializer>();
        readonly List<Lambda.LambdaAction> _lambdas = new List<Lambda.LambdaAction>();
        readonly List<CallMethodGroup> _methodGroupCalls = new List<CallMethodGroup>();
        readonly List<TypeArgCall> _typeArgCalls = new List<TypeArgCall>();
        readonly Dictionary<IDeclarationKey, List<DeclarationCall>> _declarationCalls = new Dictionary<IDeclarationKey, List<DeclarationCall>>();

        // Public reading
        public IReadOnlyList<DefinedMethodProvider> DefinedMethods => _definedMethods;
        public IReadOnlyList<DefinedClassInitializer> DefinedClasses => _definedClasses;
        public IReadOnlyList<DefinedStructInitializer> DefinedStructs => _definedStructs;
        public IReadOnlyList<Lambda.LambdaAction> Lambdas => _lambdas;
        public IReadOnlyList<CallMethodGroup> MethodGroupCalls => _methodGroupCalls;
        public IReadOnlyList<TypeArgCall> TypeArgCalls => _typeArgCalls;
        public IReadOnlyDictionary<IDeclarationKey, List<DeclarationCall>> DeclarationCalls => _declarationCalls;

        // Public adding
        public void AddMethodDeclaration(DefinedMethodProvider definedMethod) => _definedMethods.Add(definedMethod);
        public void AddClassDeclaration(DefinedClassInitializer definedClass) => _definedClasses.Add(definedClass);
        public void AddStructDeclaration(DefinedStructInitializer definedStruct) => _definedStructs.Add(definedStruct);
        public void AddLambda(Lambda.LambdaAction lambdaAction) => _lambdas.Add(lambdaAction);
        public void AddMethodGroupCall(CallMethodGroup callMethodGroup) => _methodGroupCalls.Add(callMethodGroup);
        public void AddTypeArgCall(TypeArgCall typeArgCall) => _typeArgCalls.Add(typeArgCall);
        public void AddDeclarationCall(IDeclarationKey key, DeclarationCall declarationCall) => _declarationCalls.GetValueOrAddKey(key).Add(declarationCall);

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