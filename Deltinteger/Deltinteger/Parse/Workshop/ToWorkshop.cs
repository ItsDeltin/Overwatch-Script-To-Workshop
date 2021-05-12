using SubroutineCatalog = Deltin.Deltinteger.Parse.Functions.Builder.SubroutineCatalog;

namespace Deltin.Deltinteger.Parse.Workshop
{
    public class ToWorkshop
    {
        public DeltinScript DeltinScript { get; }
        public GlobTypeArgCollector TypeArgGlob { get; }
        public ClassWorkshopInitializerComponent ClassInitializer { get; }
        public SubroutineCatalog SubroutineCatalog { get; } = new SubroutineCatalog();
        public PortableAssigner PortableAssigner { get; }

        public ToWorkshop(DeltinScript deltinScript)
        {
            DeltinScript = deltinScript;
            TypeArgGlob = new GlobTypeArgCollector(deltinScript.Importer.ScriptFiles.ToArray());
            PortableAssigner = new PortableAssigner(deltinScript);
            ClassInitializer = new ClassWorkshopInitializerComponent(this);
        }

        public T GetComponent<T>() where T: IComponent, new() => DeltinScript.GetComponent<T>();
    }
}