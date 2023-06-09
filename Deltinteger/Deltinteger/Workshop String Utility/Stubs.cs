namespace Deltin.WorkshopString;
using System.Collections.Generic;

struct Stubs
{
    List<StringChunk> stubs;

    public Stubs() => stubs = new List<StringChunk>();

    public void Add(StringChunk chunk) => stubs.Add(chunk);
    public int Count() => stubs.Count;
    public bool IsFirst() => stubs.Count == 0;
    public bool IsLast() => stubs.Count == 3;
    public bool IsCompleted() => stubs.Count == 4;
    public StringChunk[] Pop()
    {
        var values = stubs.ToArray();
        stubs.Clear();
        return values;
    }
}