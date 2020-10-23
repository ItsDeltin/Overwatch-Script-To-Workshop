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
            WorkshopSettingHero(deltinScript),
            WorkshopSettingCombo(deltinScript)
        };
    }
}