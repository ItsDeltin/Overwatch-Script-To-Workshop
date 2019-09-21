using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    abstract class LoopBuilder
    {
        protected TranslateRule Context { get; }
        private A_SkipIf SkipCondition { get; set; }
        private int StartIndex { get; set; }
        public bool IsSetup { get; private set; } = false;
        public bool Finished { get; private set; } = false;

        public LoopBuilder(TranslateRule context)
        {
            Context = context;
        }

        public void Setup()
        {
            if (Finished || IsSetup) throw new Exception();

            PreLoopStart();

            StartIndex = Context.ContinueSkip.GetSkipCount();

            // Setup the skip if.
            SkipCondition = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
            SkipCondition.ParameterValues[0] = Element.Part<V_Not>(Condition());
            AddActions(SkipCondition);
            IsSetup = true;
        }

        public void Finish()
        {
            if (Finished || !IsSetup) throw new Exception();

            PreLoopEnd();
            SkipCondition.ParameterValues[1] = new V_Number(Context.ContinueSkip.GetSkipCount() - StartIndex);
            Context.ContinueSkip.SetSkipCount(StartIndex);
            AddActions(new A_Loop());
            AddActions(Context.ContinueSkip.ResetSkipActions());
            Finished = true;
        }

        protected virtual void PreLoopStart() {}
        protected virtual void PreLoopEnd() {}

        protected abstract Element Condition();

        public void AddActions(Element[] actions)
        {
            Context.Actions.AddRange(actions);
        }
        public void AddActions(Element action)
        {
            Context.Actions.Add(action);
        }
    }

    class WhileBuilder : LoopBuilder
    {
        Element condition { get; }

        public WhileBuilder(TranslateRule context, Element condition) : base(context)
        {
            this.condition = condition;
        }

        override protected Element Condition() => condition;
    }

    class ForEachBuilder : LoopBuilder
    {
        Element array { get; }
        IndexedVar indexVar { get; set; }

        public Element Index { get { return indexVar.GetVariable(); }}
        public Element IndexValue { get { return Element.Part<V_ValueInArray>(array, indexVar.GetVariable()); }}

        public ForEachBuilder(TranslateRule context, Element array) : base(context)
        {
            this.array = array;
        }

        override protected Element Condition()
        {
            return new V_Compare();
        }

        override protected void PreLoopStart()
        {
            indexVar = Context.VarCollection.AssignVar(null, "Index", Context.IsGlobal, null);
            AddActions(indexVar.SetVariable(new V_Number(0)));
        }

        override protected void PreLoopEnd()
        {
            AddActions(indexVar.SetVariable(Element.Part<V_Add>(indexVar.GetVariable(), new V_Number(1))));
        }
    }
}