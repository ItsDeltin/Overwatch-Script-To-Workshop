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
            scope.AddNativeMethod(Parse.Lambda.WaitAsyncComponent.Method(deltinScript.Types));
        }

        public static IMethod[] GetFunctions(DeltinScript deltinScript) => new IMethod[] {
            AngleFromVectors(deltinScript),
            ChaseVariableAtRate(deltinScript),
            ChaseVariableOverTime(deltinScript),
            ClassMemory(deltinScript),
            ClassMemoryRemaining(deltinScript),
            ClassMemoryUsed(deltinScript),
            TruncateClassData(deltinScript),
            DeleteAllClasses(deltinScript),
            CompareMap(deltinScript),
            CustomColor(deltinScript),
            Destination(deltinScript),
            DestroyDummyBot(deltinScript),
            DoesLineIntersectSphere(deltinScript),
            EvaluateOnce(deltinScript),
            InsertValueInArray(deltinScript),
            LinearInterpolate(deltinScript),
            LinearInterpolateDistance(deltinScript),
            LinePlaneIntersection(deltinScript),
            Midpoint(deltinScript),
            MinWait(),
            ModifyVariable(deltinScript),
            Pi(deltinScript),
            RemoveFromArrayAtIndex(deltinScript),
            SphereHitboxRaycast(deltinScript),
            StopChasingVariable(deltinScript),
            UpdateEveryFrame(deltinScript),
            WorkshopSettingCombo(deltinScript),
            WorkshopSettingHero(deltinScript),
            GetLines(deltinScript),
            GetPoints(deltinScript),
            DoNotOptimize(deltinScript),
        };
    }
}