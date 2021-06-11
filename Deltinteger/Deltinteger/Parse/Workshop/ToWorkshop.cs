using SubroutineCatalog = Deltin.Deltinteger.Parse.Functions.Builder.SubroutineCatalog;
using LambdaBuilder = Deltin.Deltinteger.Parse.Lambda.Workshop.LambdaBuilder;

namespace Deltin.Deltinteger.Parse.Workshop
{
    public class ToWorkshop
    {
        public DeltinScript DeltinScript { get; }
        public VarCollection VarCollection => DeltinScript.VarCollection;
        public GlobTypeArgCollector TypeArgGlob { get; }
        public ClassWorkshopInitializerComponent ClassInitializer { get; }
        public SubroutineCatalog SubroutineCatalog { get; } = new SubroutineCatalog();
        public LambdaBuilder LambdaBuilder { get; }

        public ToWorkshop(DeltinScript deltinScript)
        {
            DeltinScript = deltinScript;
            TypeArgGlob = new GlobTypeArgCollector(deltinScript.Importer.ScriptFiles.ToArray());
            ClassInitializer = new ClassWorkshopInitializerComponent(this);
            LambdaBuilder = new LambdaBuilder(this);
        }

        public T GetComponent<T>() where T: IComponent, new() => DeltinScript.GetComponent<T>();
    }
}