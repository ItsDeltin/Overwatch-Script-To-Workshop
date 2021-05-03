using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    public interface IGenericUsage
    {
        // Used as type arg, apply usage to tracker.
        void UsedWithTypeArg(GlobTypeArgCollector collector, TypeArgCombo combo);
    }

    class AddToGenericsUsage : IGenericUsage
    {
        readonly CodeType _type;
        public AddToGenericsUsage(CodeType type) => _type = type;
        public void UsedWithTypeArg(GlobTypeArgCollector collector, TypeArgCombo combo)
        {
            combo.SetCurrent(_type);
            combo.StartNext();
        }
    }

    class BridgeAnonymousUsage : IGenericUsage
    {
        readonly AnonymousType _source;
        public BridgeAnonymousUsage(AnonymousType source) => _source = source;
        public void UsedWithTypeArg(GlobTypeArgCollector collector, TypeArgCombo combo)
        {
            var thisSource = collector.GlobFromTypeArg(_source);
            thisSource.OnTypeArg(type => {
                var clone = combo.Clone();
                clone.SetCurrent(type);
                clone.StartNext();
            });
        }
    }
}