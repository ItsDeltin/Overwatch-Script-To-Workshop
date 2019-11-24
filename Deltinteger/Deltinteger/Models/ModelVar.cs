using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Models
{
    public class ModelVar : Var
    {
        public Model Model { get; }

        public ModelVar(string name, ScopeGroup scope, Node node, Model model) : base(name, scope, node)
        {
            Model = model;
        }

        override public Element GetVariable(Element targetPlayer = null)
        {
            throw new NotImplementedException();
        }

        override public bool Gettable() => false;
        override public bool Settable() => false;
    }
}