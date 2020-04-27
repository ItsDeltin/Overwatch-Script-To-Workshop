using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class CodeTypeInstance
    {
        public CodeType Source { get; }
        public CodeTypeInstance[] Generics { get; }
        
        public CodeTypeInstance(CodeType source, params CodeTypeInstance[] generics)
        {
            Source = source;
            Generics = generics;
        }

        public override bool Equals(object obj)
        {
            return obj is CodeTypeInstance instance &&
                EqualityComparer<CodeType>.Default.Equals(Source, instance.Source) &&
                EqualityComparer<CodeTypeInstance[]>.Default.Equals(Generics, instance.Generics);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Generics);
        }
    }
}