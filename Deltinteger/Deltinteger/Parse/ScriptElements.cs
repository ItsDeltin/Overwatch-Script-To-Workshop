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

        // Public reading
        public IReadOnlyList<DefinedMethodProvider> DefinedMethods => _definedMethods;
        public IReadOnlyList<DefinedClassInitializer> DefinedClasses => _definedClasses;
        public IReadOnlyList<DefinedStructInitializer> DefinedStructs => _definedStructs;
        public IReadOnlyList<Lambda.LambdaAction> Lambdas => _lambdas;
        public IReadOnlyList<CallMethodGroup> MethodGroupCalls => _methodGroupCalls;

        // Public adding
        public void AddMethod(DefinedMethodProvider definedMethod) => _definedMethods.Add(definedMethod);
        public void AddClass(DefinedClassInitializer definedClass) => _definedClasses.Add(definedClass);
        public void AddStruct(DefinedStructInitializer definedStruct) => _definedStructs.Add(definedStruct);
        public void AddLambda(Lambda.LambdaAction lambdaAction) => _lambdas.Add(lambdaAction);
        public void AddMethodGroupCall(CallMethodGroup callMethodGroup) => _methodGroupCalls.Add(callMethodGroup);
    }
}