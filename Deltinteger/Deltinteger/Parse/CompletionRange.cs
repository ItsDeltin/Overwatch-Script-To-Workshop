using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class CompletionRange
    {
        public Scope Scope { get; }
        public DocRange Range { get; }
        public bool Priority { get; }

        public CompletionRange(Scope scope, DocRange range, bool priority = false)
        {
            Priority = priority;
            Scope = scope;
            Range = range;
        }
    }

    public class SignatureGroup
    {
        public SignatureRange[] Signatures { get; }

        public SignatureGroup(SignatureRange[] signatures)
        {
            if (signatures == null) throw new ArgumentNullException(nameof(signatures));

            if (signatures.Length > 0)
            {
                var name = signatures[0].Method.Name;
                for (int i = 1; i < signatures.Length; i++)
                    if (signatures[i].Method.Name != name)
                        throw new Exception("Not all methods in the signature group have the same name.");
            }

            Signatures = signatures;
        }
    }

    public class SignatureRange
    {
        public IMethod Method { get; }
        public DocRange Range { get; }
        public ParameterRange[] ParameterRanges { get; }

        public SignatureRange(IMethod method, DocRange range, ParameterRange[] parameterRanges)
        {
            Method = method;
            Range = range;
            ParameterRanges = parameterRanges;
        }
    }

    public class ParameterRange
    {
        public CodeParameter Parameter { get; }
        public int ParameterIndex { get; }
        public DocRange Range { get; }

        public ParameterRange(CodeParameter parameter, int parameterIndex, DocRange range)
        {
            Parameter = parameter;
            ParameterIndex = parameterIndex;
            Range = range;
        }
    }
}