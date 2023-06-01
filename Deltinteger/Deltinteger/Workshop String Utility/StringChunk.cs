namespace Deltin.WorkshopString;
using System;

record LogList(StringChunk[] Chunks, bool NeedsFormatPrevention);

record StringChunk(string Value, StringChunkParameter[] Parameters);

abstract record StringChunkParameter
{
    public abstract T Match<T>(Func<int, T> parameterIndex, Func<T> addNextStub);

    public record InputParameter(int ParameterIndex) : StringChunkParameter
    {
        public override T Match<T>(Func<int, T> parameterIndex, Func<T> child) => parameterIndex(ParameterIndex);
    }
    public record ChildChunk() : StringChunkParameter
    {
        public override T Match<T>(Func<int, T> parameterIndex, Func<T> child) => child();
    }
}