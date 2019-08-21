using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Parse
{
    public class UserMethod : IMethod, IScopeable, ITypeRegister
    {
        public UserMethod(ScopeGroup scope, UserMethodNode node)
        {
            Name = node.Name;
            Block = node.Block;
            ParameterNodes = node.Parameters;
            IsRecursive = node.IsRecursive;
            Documentation = node.Documentation;
            Wiki = new WikiMethod(Name, Documentation, null);
            AccessLevel = node.AccessLevel;
            Node = node;

            scope.In(this);
        }

        public void RegisterParameters(ParsingData parser)
        {
            Parameters = ParameterDefineNode.GetParameters(parser, ParameterNodes);
        }

        public string Name { get; }

        public BlockNode Block { get; }

        public ParameterBase[] Parameters { get; private set; }
        private ParameterDefineNode[] ParameterNodes { get; }

        public bool IsRecursive { get; }

        public string Documentation { get; }

        public AccessLevel AccessLevel { get; set; } = AccessLevel.Public;

        public Node Node { get; }

        public string GetLabel(bool markdown)
        {
            return Name + "(" + Parameter.ParameterGroupToString(Parameters, markdown) + ")"
            + (markdown && Documentation != null ? "\n\r" + Documentation : "");
        }
        
        public WikiMethod Wiki { get; }

        public override string ToString()
        {
            return Name;
        }

        public static UserMethod GetUserMethod(UserMethod[] methods, string name)
        {
            return methods.FirstOrDefault(um => um.Name == name);
        }

        public static CompletionItem[] CollectionCompletion(UserMethod[] methods)
        {
            return methods.Select(method => 
                new CompletionItem(method.Name)
                {
                    detail = method.GetLabel(false),
                    kind = CompletionItem.Method,
                    documentation = method.Documentation
                }
            ).ToArray();
        }

    }

    public class MethodStack
    {
        public UserMethod UserMethod { get; private set; }
        public RecursiveVar[] ParameterVars { get; private set; }
        public int ActionIndex { get; private set; }
        public IndexedVar Return { get; private set; }
        public IndexedVar ContinueSkipArray { get; private set; }

        public MethodStack(UserMethod userMethod, RecursiveVar[] parameterVars, int actionIndex, IndexedVar @return, IndexedVar continueSkipArray)
        {
            UserMethod = userMethod;
            ParameterVars = parameterVars;
            ActionIndex = actionIndex;
            Return = @return;
            ContinueSkipArray = continueSkipArray;
        }
    }
}