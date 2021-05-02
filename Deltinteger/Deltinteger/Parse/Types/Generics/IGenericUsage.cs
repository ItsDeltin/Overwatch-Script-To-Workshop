using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    public interface IGenericUsage
    {
        // Used as type arg, apply usage to tracker.
        void UsedWithTypeArg(GlobTypeArgCollector collector, TypeArgGlob glob);
    }

    class AddToGenericsUsage : IGenericUsage
    {
        readonly CodeType _type;
        public AddToGenericsUsage(CodeType type) => _type = type;
        public void UsedWithTypeArg(GlobTypeArgCollector collector, TypeArgGlob glob) => glob.AddCodeType(_type);
    }

    class BridgeAnonymousUsage : IGenericUsage
    {
        readonly AnonymousType _source;

        public BridgeAnonymousUsage(AnonymousType source)
        {
            _source = source;
        }

        public void UsedWithTypeArg(GlobTypeArgCollector collector, TypeArgGlob glob)
        {
            // When this type arg is used as a type arg, create the bridge.
            var thisSource = collector.GlobFromTypeArg(_source);
            thisSource.OnTypeArg(type => glob.AddCodeType(type));
        }
    }
}