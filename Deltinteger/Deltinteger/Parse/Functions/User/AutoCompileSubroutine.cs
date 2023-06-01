namespace Deltin.Deltinteger.Parse;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Functions.Builder.User;
using Deltin.Deltinteger.Parse.Workshop;

class AutoCompileSubroutine : IComponent
{
    readonly List<DefinedMethodProvider> userSubroutines = new List<DefinedMethodProvider>();

    public void AddSubroutine(DefinedMethodProvider definedMethodProvider)
    {
        userSubroutines.Add(definedMethodProvider);
    }

    public void Init(DeltinScript deltinScript) { }

    public void ToWorkshop(ToWorkshop toWorkshop)
    {
        foreach (var userSubroutine in userSubroutines)
        {
            // If the subroutine is in a class, make sure the class was compiled in the output.
            if (userSubroutine.ContainingType == null ||
                userSubroutine.ContainingType.WorkingInstance is not ClassType classType ||
                toWorkshop.ClassInitializer.IsRegistered(classType))
            {
                var controller = new UserFunctionController(toWorkshop, (DefinedMethodInstance)userSubroutine.GetDefaultInstance(null), InstanceAnonymousTypeLinker.Empty);
                controller.GetSubroutine();
            }
        }
    }
}