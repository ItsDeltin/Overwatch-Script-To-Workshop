using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class BuildAstVisitor : DeltinScriptBaseVisitor<Node>
    {
        public Diagnostics _diagnostics { get; }
        public string file { get; }

        public BuildAstVisitor(string file, Diagnostics diagnostics)
        {
            this.file = file;
            _diagnostics = diagnostics;
        }

        public override Node VisitRuleset(DeltinScriptParser.RulesetContext context)
        {
            return new RulesetNode(context, this);
        }

        public override Node VisitImport_file(DeltinScriptParser.Import_fileContext context)
        {
            return new ImportNode(context, this);
        }

        public override Node VisitImport_object(DeltinScriptParser.Import_objectContext context)
        {
            return new ImportObjectNode(context, this);
        }

        public override Node VisitRule_define(DeltinScriptParser.Rule_defineContext context)
        {
            return new RuleDefineNode(context, this);
        }

        public override Node VisitDefine(DeltinScriptParser.DefineContext context)
        {
            return new DefineNode(context, this);
        }

        public override Node VisitInclass_define(DeltinScriptParser.Inclass_defineContext context)
        {
            return new InclassDefineNode(context, this);
        }

        public override Node VisitUser_method(DeltinScriptParser.User_methodContext context)
        {
            return new UserMethodNode(context, this);
        }

        public override Node VisitMacro(DeltinScriptParser.MacroContext context)
        {
            return new MacroNode(context, this);
        }

        public override Node VisitOw_rule(DeltinScriptParser.Ow_ruleContext context)
        {
            return new RuleNode(context, this);
        }

        public override Node VisitBlock(DeltinScriptParser.BlockContext context)
        {
            Node[] statements = new Node[context.statement().Length];
            for (int i = 0; i < statements.Length; i++)
                statements[i] = VisitStatement(context.statement()[i]);
            
            return new BlockNode(statements, new Location(file, DocRange.GetRange(context)));
        }

        public override Node VisitExpr(DeltinScriptParser.ExprContext context)
        {
            Node node;
            
            // Operations
            if (context.ChildCount == 3 && Constants.AllOperations.Contains(context.GetChild(1).GetText()))
            {
                Node left = Visit(context.GetChild(0));
                string operation = context.GetChild(1).GetText();
                Node right = Visit(context.GetChild(2));

                node = new OperationNode(left, operation, right, new Location(file, DocRange.GetRange(context)));
            }

            // Getting values in arrays
            else if (context.ChildCount == 4
            && context.GetChild(0) is DeltinScriptParser.ExprContext
            && context.GetChild(1).GetText() == "["
            && context.GetChild(2) is DeltinScriptParser.ExprContext
            && context.GetChild(3).GetText() == "]")
            {
                Node value = Visit(context.GetChild(0));
                Node index = Visit(context.GetChild(2));

                node = new ValueInArrayNode(value, index, new Location(file, DocRange.GetRange(context)));
            }

            // Seperator
            else if (context.SEPERATOR() != null && context.expr().Length >= 2)
            {
                node = new ExpressionTreeNode(context, this);
            }
            
            // Not
            else if (context.ChildCount == 2
            && context.GetChild(0).GetText() == "!"
            && context.GetChild(1) is DeltinScriptParser.ExprContext)
            {
                Node value = Visit(context.GetChild(1));
                node = new NotNode(value, new Location(file, DocRange.GetRange(context)));
            }

            else if (context.ChildCount == 2
            && context.GetChild(0).GetText() == "-"
            && context.GetChild(1) is DeltinScriptParser.ExprContext)
            {
                Node value = Visit(context.GetChild(1));
                node = new InvertNode(value, new Location(file, DocRange.GetRange(context)));
            }

            // Ternary Condition
            else if (context.ChildCount == 5
            && context.GetChild(0) is DeltinScriptParser.ExprContext
            && context.GetChild(1).GetText() == "?"
            && context.GetChild(2) is DeltinScriptParser.ExprContext
            && context.GetChild(3).GetText() == ":"
            && context.GetChild(4) is DeltinScriptParser.ExprContext)
            {
                Node condition = VisitExpr(context.expr(0));
                Node consequent = VisitExpr(context.expr(1));
                Node alternative = VisitExpr(context.expr(2));
                node = new TernaryConditionalNode(condition, consequent, alternative, new Location(file, DocRange.GetRange(context)));
            }

            // This
            else if (context.THIS() != null)
            {
                node = new ThisNode(new Location(file, DocRange.GetRange(context)));
            }

            else if (context.ROOT() != null)
            {
                node = new RootNode(new Location(file, DocRange.GetRange(context)));
            }

            else if (context.typeconvert() != null)
            {
                node = new TypeConvertNode(context.typeconvert(), this);
            }

            else
            {
                return Visit(context.GetChild(0));
            }

            return node;
        }
        
        #region Expressions
        // -123.456
        public override Node VisitNumber(DeltinScriptParser.NumberContext context)
        {
            double value = double.Parse(context.GetText());
            return new NumberNode(value, new Location(file, DocRange.GetRange(context)));
        }

        // "Hello <0>! Waiting game..."
        public override Node VisitString(DeltinScriptParser.StringContext context)
        {
            string value = context.STRINGLITERAL().GetText().Trim('"');
            return new StringNode(value, null, context.LOCALIZED() != null, new Location(file, DocRange.GetRange(context)));
        }

        // <"hello <0>! Waiting game...", EventPlayer()>
        public override Node VisitFormatted_string(DeltinScriptParser.Formatted_stringContext context)
        {
            string value = context.@string().STRINGLITERAL().GetText().Trim('"');
            Node[] format = new Node[context.expr().Length];
            for (int i = 0; i < format.Length; i++)
                format[i] = VisitExpr(context.expr()[i]);
            return new StringNode(value, format, context.@string().LOCALIZED() != null, new Location(file, DocRange.GetRange(context)));
        }

        // Method()
        public override Node VisitMethod(DeltinScriptParser.MethodContext context)
        {
            return new MethodNode(context, this);
        }

        public override Node VisitVariable(DeltinScriptParser.VariableContext context)
        {
            return new VariableNode(context, this);
        }

        // ( expr )
        public override Node VisitExprgroup(DeltinScriptParser.ExprgroupContext context)
        {
            return Visit(context.expr());
        }

        public override Node VisitTrue(DeltinScriptParser.TrueContext context)
        {
            return new BooleanNode(true, new Location(file, DocRange.GetRange(context)));
        }

        public override Node VisitFalse(DeltinScriptParser.FalseContext context)
        {
            return new BooleanNode(false, new Location(file, DocRange.GetRange(context)));
        }

        public override Node VisitNull(DeltinScriptParser.NullContext context)
        {
            return new NullNode(new Location(file, DocRange.GetRange(context)));
        }

        public override Node VisitEnum(DeltinScriptParser.EnumContext context)
        {
            string[] split = context.GetText().Split('.');
            string type = split[0];
            string value = split[1];
            return new EnumNode(type, value, new Location(file, DocRange.GetRange(context)));
        }
        
        public override Node VisitCreatearray(DeltinScriptParser.CreatearrayContext context)
        {
            Node[] values = new Node[context.expr().Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = VisitExpr(context.expr()[i]);

            return new CreateArrayNode(values, new Location(file, DocRange.GetRange(context)));
        }

        public override Node VisitCreate_object(DeltinScriptParser.Create_objectContext context)
        {
            return new CreateObjectNode(context, this);
        }
        #endregion
        
        public override Node VisitStatement(DeltinScriptParser.StatementContext context)
        {
            Node node = null;

            if (node == null)
            {
                return Visit(context.GetChild(0));
            }
            else
            {
                return node;
            }
        }

        #region Statements
        public override Node VisitVarset(DeltinScriptParser.VarsetContext context)
        {
            return new VarSetNode(context, this);
        }

        public override Node VisitFor(DeltinScriptParser.ForContext context)
        {
            return new ForNode(context, this);
        }

        public override Node VisitForeach(DeltinScriptParser.ForeachContext context)
        {
            return new ForEachNode(context, this);
        }

        public override Node VisitWhile(DeltinScriptParser.WhileContext context)
        {
            return new WhileNode(context, this);
        }

        public override Node VisitIf(DeltinScriptParser.IfContext context)
        {
            return new IfNode(context, this);
        }

        public override Node VisitReturn(DeltinScriptParser.ReturnContext context)
        {
            Node returnValue = null;
            if (context.expr() != null)
                returnValue = VisitExpr(context.expr());

            return new ReturnNode(returnValue, new Location(file, DocRange.GetRange(context)));
        }

        public override Node VisitDelete(DeltinScriptParser.DeleteContext context)
        {
            return new DeleteNode(context, this);
        }
        #endregion

        public override Node VisitType_define(DeltinScriptParser.Type_defineContext context)
        {
            return new TypeDefineNode(context, this);
        }

        public override Node VisitConstructor(DeltinScriptParser.ConstructorContext context)
        {
            return new ConstructorNode(context, this);
        }
    }
}