using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Parse
{
    abstract public class UserMethod : IMethod, IScopeable, ITypeRegister, ICallable
    {
        public static UserMethod CreateUserMethod(ScopeGroup scope, UserMethodBase node)
        {
            if (node is UserMethodNode)
                return new UserMethodBlock(scope, (UserMethodNode)node);
            else if (node is MacroNode)
                return new Macro(scope, (MacroNode)node);
            else throw new Exception();
        }

        protected UserMethod(ScopeGroup scope, UserMethodBase node)
        {
            Name = node.Name;
            ParameterNodes = node.Parameters;
            Documentation = node.Documentation;
            Wiki = new WikiMethod(Name, Documentation, null);
            AccessLevel = node.AccessLevel;
            Node = node;

            scope.In(this);
        }

        public void RegisterParameters(ParsingData parser)
        {
            if (TypeString != null)
                Type = parser.GetDefinedType(TypeString, Node.Location);
            Parameters = ParameterDefineNode.GetParameters(parser, ParameterNodes);
        }

        public string Name { get; }
        public DefinedType Type { get; private set; }
        protected string TypeString { get; set; }
        public ParameterBase[] Parameters { get; private set; }
        private ParameterDefineNode[] ParameterNodes { get; }
        public string Documentation { get; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Public;
        public Node Node { get; }
        public WikiMethod Wiki { get; }

        abstract public Element Get(TranslateRule context, ScopeGroup scope, MethodNode methodNode, IWorkshopTree[] parameters);

        public Element Parse(TranslateRule context, bool needsToBeValue, ScopeGroup scope, MethodNode methodNode, IWorkshopTree[] parameters)
        {
            Element result = Get(context, scope, methodNode, parameters);
            if (!needsToBeValue)
                result = null;
            
            return result;
        }

        public string GetLabel(bool markdown)
        {
            return Name + "(" + Parameter.ParameterGroupToString(Parameters, markdown) + ")"
            + (markdown && Documentation != null ? "\n\r" + Documentation : "");
        }

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

    public class UserMethodBlock : UserMethod
    {
        public BlockNode Block { get; }
        public bool IsRecursive { get; }
        public bool DoesReturn { get; }

        public UserMethodBlock(ScopeGroup scope, UserMethodNode node) : base(scope, node)
        {
            List<ReturnNode> returnNodes = new List<ReturnNode>();
            ReturnNodes(node.Block, returnNodes);
            DoesReturn = node.Type != null || returnNodes.Any(returnNode => returnNode.Value != null);
            if (DoesReturn)
            {
                foreach (var returnNode in returnNodes)
                    if (returnNode.Value == null)
                        throw new SyntaxErrorException("A return value is required.", returnNode.Location);
                CheckContainer((IBlockContainer)node);
            }

            Block = node.Block;
            IsRecursive = node.IsRecursive;
            TypeString = node.Type;
        }
        
        private static void ReturnNodes(BlockNode block, List<ReturnNode> returnNodes)
        {
            foreach (var statement in block.Statements)
            {
                if (statement is IBlockContainer)
                    foreach (var container in ((IBlockContainer)statement).Paths())
                        ReturnNodes(container.Block, returnNodes);
                
                if (statement is ReturnNode)
                    returnNodes.Add((ReturnNode)statement);
            }
        }

        private static void CheckContainer(IBlockContainer container)
        {
            foreach(var path in container.Paths())
            {
                bool blockReturns = false;
                for (int i = path.Block.Statements.Length - 1; i >= 0; i--)
                {
                    if (path.Block.Statements[i] is ReturnNode)
                    {
                        blockReturns = true;
                        break;
                    }
                    
                    if (path.Block.Statements[i] is IBlockContainer)
                    {
                        if (((IBlockContainer)path.Block.Statements[i]).Paths().Any(containerPath => containerPath.WillRun)) blockReturns = true;

                        CheckContainer((IBlockContainer)path.Block.Statements[i]);
                    }
                }
                if (!blockReturns)
                    throw new SyntaxErrorException("Path does not return a value.", path.ErrorRange);
            }
        }

        override public Element Get(TranslateRule context, ScopeGroup scope, MethodNode methodNode, IWorkshopTree[] parameters)
        {
            Element result;
            if (!IsRecursive)
            {
                // Check the method stack if this method was already called.
                // Throw a syntax error if it was.
                if (context.MethodStackNotRecursive.Contains(this))
                    throw SyntaxErrorException.RecursionNotAllowed(methodNode.Location);

                var methodScope = scope.Root().Child();

                // Add the parameter variables to the scope.
                context.AssignParameterVariables(methodScope, Parameters, parameters, methodNode);

                // The variable that stores the return value.
                IndexedVar returns = null;
                if (DoesReturn)
                {
                    returns = IndexedVar.AssignVar(context.VarCollection, scope, $"{methodNode.Name}: return temp value", context.IsGlobal, null);
                    returns.Type = Type;
                }

                // Add the method to the method stack
                context.MethodStackNotRecursive.Add(this);

                Block.RelatedScopeGroup = methodScope;

                // Parse the block of the method
                context.ParseBlock(methodScope, methodScope, Block, true, returns);

                // Take the method scope out of scope.
                methodScope.Out(context);

                // Remove the method from the stack.
                context.MethodStackNotRecursive.Remove(this);

                if (DoesReturn)
                    result = returns.GetVariable();
                else result = new V_Null();
            }
            else
            {
                // Check the method stack if this method was already called. It will be null if it wasn't called.
                MethodStack lastMethod = context.MethodStackRecursive.FirstOrDefault(ms => ms.UserMethod == this);
                if (lastMethod != null)
                {
                    context.ContinueSkip.Setup();

                    // Re-push the paramaters.
                    for (int i = 0; i < lastMethod.ParameterVars.Length; i++)
                    {
                        if (lastMethod.ParameterVars[i] is RecursiveVar)
                            context.Actions.AddRange
                            (
                                ((RecursiveVar)lastMethod.ParameterVars[i]).InScope((Element)parameters[i])
                            );
                    }

                    // Add to the continue skip array.
                    context.Actions.AddRange(
                        lastMethod.ContinueSkipArray.SetVariable(
                            Element.Part<V_Append>(lastMethod.ContinueSkipArray.GetVariable(), new V_Number(context.ContinueSkip.GetSkipCount() + 3))
                        )
                    );

                    // Loop back to the start of the method.
                    context.ContinueSkip.SetSkipCount(lastMethod.ActionIndex);
                    context.Actions.Add(Element.Part<A_Loop>());

                    result = lastMethod.Return.GetVariable();
                }
                else
                {
                    var methodScope = scope.Root().Child(true);

                    // Add the parameter variables to the scope.
                    Var[] parameterVars = new Var[Parameters.Length];
                    for (int i = 0; i < parameterVars.Length; i++)
                    {
                        if (parameters[i] is Element)
                        {
                            // Create a new variable using the parameter input.
                            parameterVars[i] = (RecursiveVar)IndexedVar.AssignVar(context.VarCollection, methodScope, Parameters[i].Name, context.IsGlobal, methodNode);
                            ((RecursiveVar)parameterVars[i]).Type = ((Element)parameters[i]).SupportedType?.Type;
                            context.Actions.AddRange
                            (
                                ((RecursiveVar)parameterVars[i]).InScope((Element)parameters[i])
                            );
                        }
                        else if (parameters[i] is EnumMember)
                        {
                            parameterVars[i] = new ElementReferenceVar(Parameters[i].Name, methodScope, methodNode, parameters[i]);
                        }
                        else throw new NotImplementedException();
                    }

                    var returns = IndexedVar.AssignInternalVarExt(context.VarCollection, null, $"{methodNode.Name}: return temp value", context.IsGlobal);
                    returns.Type = Type;

                    // Setup the continue skip array.
                    IndexedVar continueSkipArray = IndexedVar.AssignInternalVar(context.VarCollection, null, $"{methodNode.Name}: continue skip array", context.IsGlobal);
                    var stack = new MethodStack(this, parameterVars, context.ContinueSkip.GetSkipCount(), returns, continueSkipArray);

                    // Add the method to the stack.
                    context.MethodStackRecursive.Add(stack);

                    Block.RelatedScopeGroup = methodScope;
                    
                    // Parse the method block
                    context.ParseBlock(methodScope, methodScope, Block, true, returns);

                    // No return value if the method is being used as an action.
                    result = returns.GetVariable();

                    // Take the method out of scope.
                    //Actions.AddRange(methodScope.RecursiveMethodStackPop());
                    methodScope.Out(context);

                    // Setup the next continue skip.
                    context.ContinueSkip.Setup();
                    context.ContinueSkip.SetSkipCount(Element.Part<V_LastOf>(continueSkipArray.GetVariable()));

                    // Remove the last continue skip.
                    context.Actions.AddRange(
                        continueSkipArray.SetVariable(
                            Element.Part<V_ArraySlice>(
                                continueSkipArray.GetVariable(), 
                                new V_Number(0),
                                Element.Part<V_CountOf>(continueSkipArray.GetVariable()) - 1
                            )
                        )
                    );

                    // Loop if the method goes any deeper by checking the length of the continue skip array.
                    context.Actions.Add(
                        Element.Part<A_LoopIf>(
                            Element.Part<V_Compare>(
                                Element.Part<V_CountOf>(continueSkipArray.GetVariable()),
                                EnumData.GetEnumValue(Operators.NotEqual),
                                new V_Number(0)
                            )
                        )
                    );

                    // Reset the continue skip.
                    context.ContinueSkip.ResetSkip();
                    context.Actions.AddRange(continueSkipArray.SetVariable(0));
                    
                    // Remove the method from the stack.
                    context.MethodStackRecursive.Remove(stack);
                }
            }

            return result;
        }
    }

    public class Macro : UserMethod
    {
        public Node Expression { get; }

        public Macro(ScopeGroup scope, MacroNode node) : base(scope, node)
        {
            Expression = node.Expression;
            TypeString = null;
        }

        override public Element Get(TranslateRule context, ScopeGroup scope, MethodNode methodNode, IWorkshopTree[] parameters)
        {
            // Check the method stack if this method was already called.
            // Throw a syntax error if it was.
            if (context.MethodStackNotRecursive.Contains(this))
                throw new SyntaxErrorException("Recursion is not allowed in macros.", methodNode.Location);
            context.MethodStackNotRecursive.Add(this);

            int actionCount = context.Actions.Count;

            ScopeGroup methodScope = scope.Root().Child();

            for (int i = 0; i < parameters.Length; i++)
                new ElementReferenceVar(Parameters[i].Name, methodScope, methodNode.Parameters[i], parameters[i]);

            Element result = context.ParseExpression(methodScope, methodScope, Expression);

            methodScope.Out(context);

            context.MethodStackNotRecursive.Remove(this);

            if (context.Actions.Count > actionCount)
                throw new SyntaxErrorException("Macro cannot result in any actions.", methodNode.Location);
            
            return result;
        }
    }

    public class MethodStack
    {
        public UserMethod UserMethod { get; private set; }
        public Var[] ParameterVars { get; private set; }
        public int ActionIndex { get; private set; }
        public IndexedVar Return { get; private set; }
        public IndexedVar ContinueSkipArray { get; private set; }

        public MethodStack(UserMethod userMethod, Var[] parameterVars, int actionIndex, IndexedVar @return, IndexedVar continueSkipArray)
        {
            UserMethod = userMethod;
            ParameterVars = parameterVars;
            ActionIndex = actionIndex;
            Return = @return;
            ContinueSkipArray = continueSkipArray;
        }
    }
}