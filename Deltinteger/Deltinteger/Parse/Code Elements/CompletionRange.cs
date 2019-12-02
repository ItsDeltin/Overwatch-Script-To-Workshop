using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class CompletionRange
    {
        public Scope Scope { get; }
        public DocRange Range { get; }

        public CompletionRange(Scope scope, DocRange range)
        {
            Scope = scope;
            Range = range;
        }
    }
}