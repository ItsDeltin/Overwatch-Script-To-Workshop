namespace DS.Analysis.Types.Standard
{
    static class StandardTypes
    {
        public static readonly CodeTypeProvider Unknown = new CodeTypeProvider("?");
        public static readonly ITypeDirector UnknownInstance;

        static StandardTypes()
        {
            UnknownInstance = Unknown.CreateInstance();
        }
    }
}