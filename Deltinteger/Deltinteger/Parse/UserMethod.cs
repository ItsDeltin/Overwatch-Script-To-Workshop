using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Parse
{
    public class UserMethod : IMethod
    {
        public UserMethod(UserMethodNode node)
        {
            Name = node.Name;
            Block = node.Block;

            Parameters = new Parameter[node.Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                Parameters[i] = new Parameter(node.Parameters[i], Elements.ValueType.Any, null);
            }

            IsRecursive = node.IsRecursive;
            Documentation = node.Documentation;
            Wiki = new WikiMethod(Name, Documentation, null);
        }

        public string Name { get; }

        public BlockNode Block { get; }

        public ParameterBase[] Parameters { get; }

        public bool IsRecursive { get; }

        public string Documentation { get; }

        public string GetLabel(bool markdown)
        {
            return Name + "(" + Parameter.ParameterGroupToString(Parameters, markdown) + ")"
            + "\n\r"
            + Documentation;
        }
        
        public WikiMethod Wiki { get; }


        public override string ToString()
        {
            return GetLabel(false);
        }

        public static UserMethod GetUserMethod(UserMethod[] methods, string name)
        {
            return methods.FirstOrDefault(um => um.Name == name);
        }

        public static CompletionItem[] CollectionCompletion(UserMethod[] methods)
        {
            return methods.Select(method => 
                new CompletionItem(method.ToString())
                {
                    kind = CompletionItem.Method
                }
            ).ToArray();
        }
    }

    public class MethodStack
    {
        public UserMethod UserMethod { get; private set; }
        public RecursiveVar[] ParameterVars { get; private set; }
        public int ActionIndex { get; private set; }
        public Var Return { get; private set; }
        public Var ContinueSkipArray { get; private set; }

        public MethodStack(UserMethod userMethod, RecursiveVar[] parameterVars, int actionIndex, Var @return, Var continueSkipArray)
        {
            UserMethod = userMethod;
            ParameterVars = parameterVars;
            ActionIndex = actionIndex;
            Return = @return;
            ContinueSkipArray = continueSkipArray;
        }
    }
}