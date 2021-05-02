using System.Collections.Generic;

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

        // Public reading
        public IReadOnlyList<DefinedMethodProvider> DefinedMethods => _definedMethods;
        public IReadOnlyList<DefinedClassInitializer> DefinedClasses => _definedClasses;
        public IReadOnlyList<DefinedStructInitializer> DefinedStructs => _definedStructs;
        public IReadOnlyList<Lambda.LambdaAction> Lambdas => _lambdas;
        public IReadOnlyList<CallMethodGroup> MethodGroupCalls => _methodGroupCalls;
        public IReadOnlyList<TypeArgCall> TypeArgCalls => _typeArgCalls;

        // Public adding
        public void AddMethodDeclaration(DefinedMethodProvider definedMethod) => _definedMethods.Add(definedMethod);
        public void AddClassDeclaration(DefinedClassInitializer definedClass) => _definedClasses.Add(definedClass);
        public void AddStructDeclaration(DefinedStructInitializer definedStruct) => _definedStructs.Add(definedStruct);
        public void AddLambda(Lambda.LambdaAction lambdaAction) => _lambdas.Add(lambdaAction);
        public void AddMethodGroupCall(CallMethodGroup callMethodGroup) => _methodGroupCalls.Add(callMethodGroup);
        public void AddTypeArgCall(TypeArgCall typeArgCall) => _typeArgCalls.Add(typeArgCall);
    }
}