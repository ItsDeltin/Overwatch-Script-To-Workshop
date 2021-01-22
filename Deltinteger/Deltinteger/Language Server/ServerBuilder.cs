namespace Deltin.Deltinteger.LanguageServer
{
    public class LanguageServerBuilder
    {
        public DeltintegerLanguageServer Server { get; }
        public DidChangeWatchedFilesHandlerBuilder FileHandlerBuilder { get; }
        public ParserSettingsResolver ParserSettingsResolver { get; }

        public LanguageServerBuilder(DeltintegerLanguageServer server)
        {
            Server = server;
            FileHandlerBuilder = new DidChangeWatchedFilesHandlerBuilder();
            ParserSettingsResolver = new ParserSettingsResolver(this);
        }
    }
}