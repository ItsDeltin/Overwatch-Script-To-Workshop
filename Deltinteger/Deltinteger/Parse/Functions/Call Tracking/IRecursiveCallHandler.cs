namespace Deltin.Deltinteger.Parse
{
    // Manages recursive calls.
    public interface IRecursiveCallHandler
    {
        CallInfo CallInfo { get; }
        string TypeName { get; }
        bool DoesRecursivelyCall(IRecursiveCallHandler calling);
        bool CanBeRecursivelyCalled();
        string GetLabel(DeltinScript deltinScript);
    }

    public class RecursiveCallHandler : IRecursiveCallHandler
    {
        readonly IApplyBlock _applyBlock;
        readonly bool _recursionAllowed;
        readonly string _typeName;

        public RecursiveCallHandler(IApplyBlock applyBlock, bool recursionAllowed, string typeName = "function")
        {
            _applyBlock = applyBlock;
            _recursionAllowed = recursionAllowed;
            _typeName = typeName;
        }

        public CallInfo CallInfo => _applyBlock.CallInfo;
        public string TypeName => _typeName;
        public bool DoesRecursivelyCall(IRecursiveCallHandler calling) => this == calling;
        public bool CanBeRecursivelyCalled() => _recursionAllowed;
        public string GetLabel(DeltinScript deltinScript) => _applyBlock.GetLabel(deltinScript, LabelInfo.RecursionError).ToString(false);
    }

    public interface IApplyBlock : ILabeled
    {
        CallInfo CallInfo { get; }
    }
}