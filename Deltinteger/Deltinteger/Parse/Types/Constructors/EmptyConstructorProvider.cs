using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Types.Constructors
{
    public class EmptyConstructorProvider : IConstructorProvider<Constructor>
    {
        private readonly Location _definedAt;

        public EmptyConstructorProvider(Location definedAt)
        {
            _definedAt = definedAt;
        }

        public Constructor GetInstance(CodeType initializedType, InstanceAnonymousTypeLinker genericsLinker) => new Constructor(initializedType, _definedAt, AccessLevel.Public);
    }
}