namespace Deltin.Deltinteger.Parse.Workshop
{
    public class ToWorkshop
    {
        readonly DeltinScript _deltinScript;
        public CompileRelations Relations { get; }
        public PortableAssigner PortableAssigner { get; }

        public ToWorkshop(DeltinScript deltinScript)
        {
            _deltinScript = deltinScript;
            Relations = new CompileRelations(deltinScript);
            PortableAssigner = new PortableAssigner(deltinScript);
        }

        public T GetComponent<T>() where T: IComponent, new() => _deltinScript.GetComponent<T>();
    }
}