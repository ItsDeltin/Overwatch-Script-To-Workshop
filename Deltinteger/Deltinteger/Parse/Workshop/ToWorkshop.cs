namespace Deltin.Deltinteger.Parse.Workshop
{
    public class ToWorkshop
    {
        public DeltinScript DeltinScript { get; }
        public CompileRelations Relations { get; }
        public GlobTypeArgCollector TypeArgGlob { get; }
        public PortableAssigner PortableAssigner { get; }
        public ClassWorkshopInitializerComponent ClassInitializer { get; }

        public ToWorkshop(DeltinScript deltinScript)
        {
            DeltinScript = deltinScript;
            // Relations = new CompileRelations(deltinScript);
            TypeArgGlob = new GlobTypeArgCollector(deltinScript.Importer.ScriptFiles.ToArray());
            PortableAssigner = new PortableAssigner(deltinScript);
            ClassInitializer = new ClassWorkshopInitializerComponent(this);
        }

        public T GetComponent<T>() where T: IComponent, new() => DeltinScript.GetComponent<T>();
    }
}