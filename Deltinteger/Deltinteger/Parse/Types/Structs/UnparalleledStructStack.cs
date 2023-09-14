namespace Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

readonly struct UnparalleledStructStack
{
    public readonly IVariableInstance Variable;
    public readonly IGettable SteppedValue;

    private UnparalleledStructStack(IVariableInstance variable, IGettable steppedValue)
    {
        Variable = variable;
        SteppedValue = steppedValue;
    }

    public static UnparalleledStructStack[] StacksFromType(IGettable source, StructInstance structInstance)
    {
        var result = new UnparalleledStructStack[structInstance.Variables.Length];

        // delta is the current index in the source array.
        int delta = 0;
        for (int i = 0; i < structInstance.Variables.Length; i++)
        {
            var variable = structInstance.Variables[i];
            var variableAssigner = variable.GetAssigner();
            int stackDelta = variableAssigner.StackDelta();

            // This is a bit strange since it uses the 'AssignClassStacks' function
            // that classes use to assign variables to structs. If the current variable
            // is a value like '{ x: 1, y: 2 }', 'stackDelta' will be 2 and 'stacks' will
            // look like '[source[0], source[1]]' with 0 and 1 shifted according to the current
            // delta value.
            var stacks = new IGettable[stackDelta];
            for (int s = 0; s < stacks.Length; s++)
                stacks[s] = source.ChildFromClassReference(Element.Num(delta + s));

            var getStacks = new GetClassStacks(IStackFromIndex.FromArray(stacks), 0);
            // When 'AssignClassStacks' queries a new stack, getStacks will return 'source[requestedIndex + delta]'
            var gettable = variableAssigner.AssignClassStacks(getStacks);

            result[i] = new UnparalleledStructStack(variable, gettable);
            delta += stackDelta;
        }

        return result;
    }
}