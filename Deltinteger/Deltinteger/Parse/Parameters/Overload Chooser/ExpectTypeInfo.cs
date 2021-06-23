using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class ExpectTypeInfo
    {
        public CodeType Type { get; private set; }
        public bool RegisterOccursLater { get; }
        private readonly List<IExpectingTypeReady> _apply = new List<IExpectingTypeReady>();

        public ExpectTypeInfo()
        {
            RegisterOccursLater = true;
        }

        public ExpectTypeInfo(CodeType type)
        {
            RegisterOccursLater = false;
            Type = type;
        }

        public void Apply(IExpectingTypeReady applier)
        {
            if (!RegisterOccursLater)
                // The arrow registration occurs now, parse the statement.
                applier.TypeReady(Type);
            else
                // Otherwise, add it to the _apply list so we can apply it later.
                _apply.Add(applier);
        }

        public void FinishAppliers(CodeType type)
        {
            Type = type;
            foreach (var apply in _apply) apply.TypeReady(type);
        }

        public void FinishAppliers()
        {
            foreach (var apply in _apply) apply.NoTypeReady();
        }
    }

    public interface IExpectingTypeReady
    {
        void NoTypeReady();
        void TypeReady(CodeType type);
    }
}