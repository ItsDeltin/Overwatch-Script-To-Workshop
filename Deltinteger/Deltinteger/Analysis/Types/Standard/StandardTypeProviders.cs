namespace DS.Analysis.Types.Standard
{
    static class StandardTypeProviders
    {
        public static readonly CodeTypeProvider Unknown = new CodeTypeProvider("?");
        public static readonly CodeType UnknownInstance;

        static StandardTypeProviders()
        {
            UnknownInstance = Unknown.CreateInstance();
        }
    }
}