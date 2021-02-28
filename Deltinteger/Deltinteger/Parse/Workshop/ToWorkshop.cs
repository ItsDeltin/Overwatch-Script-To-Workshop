namespace Deltin.Deltinteger.Parse
{
    public class ToWorkshop
    {
        readonly DeltinScript _deltinScript;

        public ToWorkshop(DeltinScript deltinScript)
        {
            _deltinScript = deltinScript;
        }

        public T GetComponent<T>() where T: IComponent, new() => _deltinScript.GetComponent<T>();
    }
}