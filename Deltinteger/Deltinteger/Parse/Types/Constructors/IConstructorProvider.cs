namespace Deltin.Deltinteger.Parse.Types.Constructors
{
    public interface IConstructorProvider<out T> where T: Constructor
    {
        T GetInstance(CodeType initializedType, InstanceAnonymousTypeLinker genericsLinker);
    }
}