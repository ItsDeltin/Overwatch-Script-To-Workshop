using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.GlobalFunctions
{
    public static partial class GlobalFunctions
    {
        public static void Add(DeltinScript deltinScript, Scope scope)
        {
            var functions = GetFunctions(deltinScript);
            foreach (var function in functions)
                scope.AddNativeMethod(function);
        }

        public static IMethod[] GetFunctions(DeltinScript deltinScript) => new IMethod[] {
            ModifyVariable(deltinScript),

            ChaseVariableAtRate(deltinScript),
            ChaseVariableOverTime(deltinScript),
            StopChasingVariable(deltinScript),

            ClassMemory(deltinScript),
            ClassMemoryRemaining(deltinScript),
            ClassMemoryUsed(deltinScript),

            WorkshopSettingHero(deltinScript),
            WorkshopSettingCombo(deltinScript),

            MinWait()
        };
    }
}