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

        private readonly List<VariableChase> _variableChases = new List<VariableChase>();
        private readonly List<VectorChase> _vectorChases = new List<VectorChase>();

        public Looper(bool global)
        {
            _rule = new Rule(Constants.INTERNAL_ELEMENT + "Chase", global ? RuleEvent.OngoingGlobal : RuleEvent.OngoingPlayer, Team.All, PlayerSelector.All);
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

        public VariableChase GetVariableChaseData(Var var)
        {
            var existingChaseData = _variableChases.FirstOrDefault(cd => cd.Var == var);
            if (existingChaseData != null)
                return existingChaseData;
            
            Var destination = var.VarCollection.AssignVar($"'{var.Name}' chase destination", true);
            Var rate        = var.VarCollection.AssignVar($"'{var.Name}' chase duration", true);

            VariableChase newChaseData = new VariableChase(var, destination, rate);
            _variableChases.Add(newChaseData);

            AddActions(GetOneActionChase(var, destination, rate));

            return newChaseData;
        }

        private static Element[] GetOneActionChase(Var var, Var destination, Var rate)
        {
            Element isGoingUp = Element.Part<V_Compare>(var.GetVariable(), EnumData.GetEnumValue(Operators.LessThanOrEqual), destination.GetVariable());

            Element rateAdjusted = Element.Part<V_Multiply>(rate.GetVariable(), Element.Part<V_Multiply>(new V_Number(Constants.MINIMUM_WAIT), Element.TernaryConditional(isGoingUp, new V_Number(1), new V_Number(-1))));

            Element varAdjusted = Element.Part<V_Add>(var.GetVariable(), rateAdjusted);

            Element setVar = var.SetVariable(
                Element.TernaryConditional(isGoingUp,
                    Element.Part<V_Min>(varAdjusted, destination.GetVariable()),
                    Element.Part<V_Max>(varAdjusted, destination.GetVariable())
                )
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

        public VectorChase GetVectorChaseData(Var var)
        {
            var existingChaseData = _vectorChases.FirstOrDefault(cd => cd.Var == var);
            if (existingChaseData != null)
                return existingChaseData;
            
            Var destination = var.VarCollection.AssignVar($"'{var.Name}' chase destination", true);
            Var rate        = var.VarCollection.AssignVar($"'{var.Name}' chase duration", true);

            VectorChase newChaseData = new VectorChase(var, destination, rate);
            _vectorChases.Add(newChaseData);

            AddActions(ChaseVectorActions(var, destination, rate));

            return newChaseData;
        }

        private static Element[] ChaseVectorActions(Var var, Var destination, Var rate)
        {
            Element rateAdjusted = Element.Part<V_Multiply>(rate.GetVariable(), new V_Number(Constants.MINIMUM_WAIT));

            Element distance = Element.Part<V_DistanceBetween>(var.GetVariable(), destination.GetVariable());

            Element ratio = Element.Part<V_Divide>(rateAdjusted, distance);

            Element delta = Element.Part<V_Subtract>(destination.GetVariable(), var.GetVariable());

            Element result = Element.TernaryConditional(
                new V_Compare(distance, Operators.GreaterThan, rateAdjusted),
                Element.Part<V_Add>(var.GetVariable(), Element.Part<V_Multiply>(ratio, delta)),
                destination.GetVariable()
            );

            Element setVar = var.SetVariable(result);

            return new Element[] { setVar };
        }
    }

    public class VariableChase
    {
        public readonly Var Var;
        public readonly Var Destination;
        public readonly Var Rate;

        public VariableChase(Var var, Var destination, Var rate)
        {
            Var = var;
            Destination = destination;
            Rate = rate;
        }
    }

    public class VectorChase
    {
        public readonly Var Var;
        public readonly Var Destination;
        public readonly Var Rate;

        public VectorChase(Var var, Var destination, Var rate)
        {
            Var = var;
            Destination = destination;
            Rate = rate;
        }
    }
}
