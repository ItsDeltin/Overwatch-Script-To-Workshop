using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class UserMethod
    {
        public UserMethod(DeltinScriptParser.User_methodContext context)
        {
            Name = context.PART()[0].GetText();
            Block = context.block();

            var contextParams = context.PART().Skip(1).ToArray();
            Parameters = new Parameter[contextParams.Length];

            for (int i = 0; i < Parameters.Length; i++)
            {
                var name = contextParams[i].GetText();
                Parameters[i] = new Parameter(name, Elements.ValueType.Any, null);
            }

            UserMethodCollection.Add(this);
        }

        public string Name { get; private set; }

        public DeltinScriptParser.BlockContext Block { get; private set; }

        public Parameter[] Parameters { get; private set; }

        public static readonly List<UserMethod> UserMethodCollection = new List<UserMethod>();

        public static UserMethod GetUserMethod(string name)
        {
            return UserMethodCollection.FirstOrDefault(um => um.Name == name);
        }
    }
}