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

        public Looper()
        {
            _rule = new Rule(Constants.INTERNAL_ELEMENT + "Chase");
            _actions.Add(Element.Part<A_Wait>(new V_Number(Constants.MINIMUM_WAIT)));
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
    }
}
