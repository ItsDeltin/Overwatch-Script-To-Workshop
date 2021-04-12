namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    /// <summary>Creates an IWorkshopFunctionController.</summary>
    public interface IWorkshopFunctionControllerProvider
    {
        IWorkshopFunctionController Get(DeltinScript deltinScript);
    }

    /// <summary> The root interface representing function and function-like patterns being compiled to the workshop.
    /// This is used by the WorkshopFunctionBuilder to create inline functions and subroutines.</summary>
    public interface IWorkshopFunctionController
    {
        /// <summary>The attributes of the function controller.</summary>
        WorkshopFunctionControllerAttributes Attributes { get; }

        /// <summary>Creates a return handler.</summary>
        ReturnHandler GetReturnHandler(ActionSet actionSet);

        /// <summary>Creates parameter handlers.</summary>
        IParameterHandler[] CreateParameterHandlers(ActionSet actionSet);

        /// <summary>The function's subroutine. May be null.</summary>
        SubroutineCatalogItem GetSubroutine();

        object StackIdentifier();

        /// <summary>Build the inner functions.</summary>
        void Build(ActionSet actionSet);
    }

    public class WorkshopFunctionControllerAttributes
    {
        /// <summary>Determines whether the function is recursive.</summary>
        public bool IsRecursive { get; set; }

        /// <summary>Determines whether the function needs to track the type of the object instance recursively.
        /// This may be required when calling a recursive, virtual, instanced function.</summary>
        public bool RecursiveRequiresObjectStack { get; set; }

        /// <summary>Determines if the function is an instanced function.</summary>
        public bool IsInstance { get; set; }
    }
}