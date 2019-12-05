using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class CompletionRange
    {
        public Scope Scope { get; }
        public DocRange Range { get; }
        public bool Priority { get; }

        public CompletionRange(Scope scope, DocRange range)
        {
            Scope = scope;
            Range = range;
        }

        public CompletionRange(Scope scope, DocRange range, bool priority) : this(scope, range)
        {
            Priority = priority;
        }
    }
}