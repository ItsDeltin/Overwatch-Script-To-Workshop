using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Types.Constructors
{
    public class EmptyConstructorProvider : IConstructorProvider<Constructor>
    {
        private readonly CodeType _type;
        private readonly Location _definedAt;

        public EmptyConstructorProvider(CodeType type, Location definedAt)
        {
            _type = type;
            _definedAt = definedAt;
        }

        public Constructor GetInstance(InstanceAnonymousTypeLinker genericsLinker) => new Constructor(_type, _definedAt, AccessLevel.Public);
    }
}