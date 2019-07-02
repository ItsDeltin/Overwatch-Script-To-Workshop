using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class Looper
    {
        private readonly List<Element> _actions = new List<Element>();
        private readonly Rule _rule;
        private int _insertAtIndex = 0;
        public bool Used { get; private set; }

        private readonly List<ChaseData> _chaseData = new List<ChaseData>();

        public Looper()
        {
            _rule = new Rule(Constants.INTERNAL_ELEMENT + "Chase");
            _actions.Add(A_Wait.MinimumWait);
            _actions.Add(Element.Part<A_Loop>());
        }

        public void AddActions(Element[] actions)
        {
            _actions.InsertRange(_insertAtIndex, actions);
            _insertAtIndex += actions.Length;
            Used = true;
        }

        public Rule Finalize()
        {
            _rule.Actions = _actions.ToArray();
            return _rule;
        }

        public ChaseData GetChaseData(Var var)
        {
            var existingChaseData = _chaseData.FirstOrDefault(cd => cd.Var == var);
            if (existingChaseData != null)
                return existingChaseData;
            
            Var destination = var.VarCollection.AssignVar($"'{var.Name}' chase destination", true);
            Var rate        = var.VarCollection.AssignVar($"'{var.Name}' chase duration", true);

            ChaseData newChaseData = new ChaseData(var, destination, rate);
            _chaseData.Add(newChaseData);

            AddActions(GetOneActionChase(var, destination, rate));

            return newChaseData;
        }

        private static Element[] GetOneActionChase(Var var, Var destination, Var rate)
        {
            Element direction = Element.Part<V_IndexOfArrayValue>(Element.CreateArray(new V_False(), new V_True()), Element.Part<V_Compare>(var.GetVariable(), EnumData.GetEnumValue(Operators.LessThanOrEqual), destination.GetVariable()));

            Element rateAdjusted = Element.Part<V_Multiply>(rate.GetVariable(), Element.Part<V_Multiply>(new V_Number(Constants.MINIMUM_WAIT), Element.Part<V_ValueInArray>(Element.CreateArray(new V_Number(-1), new V_Number(1)), direction)));

            Element varAdjusted = Element.Part<V_Add>(var.GetVariable(), rateAdjusted);

            Element setVar = var.SetVariable(
                Element.Part<V_ValueInArray>(Element.CreateArray(
                    Element.Part<V_Max>(varAdjusted, destination.GetVariable()),
                    Element.Part<V_Min>(varAdjusted, destination.GetVariable())
                ), 
                direction)
            );

            return new Element[] { setVar };
        }

        private static Element[] GetChaseActions(Var var, Var destination, Var rate)
        {
            return new Element[] 
            {
                Element.Part<A_SkipIf>(Element.Part<V_Not>(Element.Part<V_Compare>(var.GetVariable(), EnumData.GetEnumValue(Operators.LessThanOrEqual), destination.GetVariable())), new V_Number(2)),
                var.SetVariable(Element.Part<V_Min>(Element.Part<V_Add>(var.GetVariable(), Element.Part<V_Multiply>(rate.GetVariable(), new V_Number(Constants.MINIMUM_WAIT))), destination.GetVariable())),
                Element.Part<A_Skip>(new V_Number(1)),
                var.SetVariable(Element.Part<V_Max>(Element.Part<V_Add>(var.GetVariable(), Element.Part<V_Multiply>(rate.GetVariable(), new V_Number(-Constants.MINIMUM_WAIT))), destination.GetVariable())),
            };
        }
    }

    public class ChaseData
    {
        public readonly Var Var;
        public readonly Var Destination;
        public readonly Var Rate;

        public ChaseData(Var var, Var destination, Var rate)
        {
            Var = var;
            Destination = destination;
            Rate = rate;
        }
    }
}
