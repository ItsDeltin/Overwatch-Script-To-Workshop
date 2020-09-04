using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class SwitchAction : IStatement, IBlockContainer, IBreakContainer
    {
        private readonly IExpression Expression;
        private readonly SwitchSection[] paths;
        private readonly PathInfo[] pathInfo;
        private SwitchBuilder switchBuilder;

        public SwitchAction(ParseInfo parseInfo, Scope scope, Switch switchContext)
        {
            // Get the expression.
            Expression = parseInfo.GetExpression(scope, switchContext.Expression);

            paths = GetSections(ResolveElements(parseInfo.SetBreakHandler(this), scope, switchContext));
            pathInfo = new PathInfo[paths.Length];

            for (int i = 0; i < pathInfo.Length; i++)
                pathInfo[i] = new PathInfo(paths[i].Block, paths[i].ErrorRange, paths[i].IsDefault);
        }

        private SwitchElement[] ResolveElements(ParseInfo parseInfo, Scope scope, Switch switchContext)
        {
            List<SwitchElement> elements = new List<SwitchElement>();
            bool inSection = false;
            bool caseError = false;
            bool gotDefault = false;

            // Resolve paths.
            foreach (var statement in switchContext.Statements)
            {
                var switchCase = statement as SwitchCase;

                // Syntax error if there is a statement before a case.
                if (switchCase != null && !inSection && !caseError)
                {
                    parseInfo.Script.Diagnostics.Error("Expected case or default.", statement.Range);
                    caseError = true;
                }

                // Don't throw the syntax error multiple times in one switch.
                if (switchCase != null) inSection = true;

                // Default case.
                if (switchCase != null && switchCase.IsDefault)
                {
                    if (gotDefault) parseInfo.Script.Diagnostics.Error("Switch cannot have multiple defaults.", switchCase.Range);
                    gotDefault = true;
                }

                // Get the statement
                if (switchCase == null) elements.Add(new SwitchElement(parseInfo.GetStatement(scope, statement)));
                // Get the case
                else if (!switchCase.IsDefault) elements.Add(new SwitchElement(switchCase.Token.Range, parseInfo.GetExpression(scope, switchCase.Value)));
                // Get default
                else elements.Add(new SwitchElement(switchCase.Token.Range));
            }

            return elements.ToArray();
        }

        private SwitchSection[] GetSections(SwitchElement[] elements)
        {
            List<SwitchSection> sections = new List<SwitchSection>();

            List<IStatement> currentStatements = new List<IStatement>();
            List<IExpression> currentCases = new List<IExpression>();
            bool currentIsDefault = false;
            bool addingStatements = false;
            DocRange errorRange = null;

            for (int i = 0; i < elements.Length; i++)
            {
                SwitchElement element = elements[i];
                bool switchCondition = element.Type == SwitchElementType.Case || element.Type == SwitchElementType.Default;
                bool isLast = i == elements.Length - 1;

                if (switchCondition && addingStatements)
                {
                    // Add the switch section.
                    sections.Add(new SwitchSection(errorRange, currentIsDefault, currentCases.ToArray(), currentStatements.ToArray()));    

                    // Reset case info.
                    currentStatements = new List<IStatement>();
                    currentCases = new List<IExpression>();
                    currentIsDefault = false;
                    addingStatements = false;
                    errorRange = null;
                }

                if (errorRange == null) errorRange = element.ErrorRange;

                switch (element.Type)
                {
                    // Set is default.
                    case SwitchElementType.Default:
                        currentIsDefault = true;
                        break;

                    // Add case.
                    case SwitchElementType.Case:
                        currentCases.Add(element.Condition);
                        break;
                    
                    // Add statement.
                    case SwitchElementType.Statement:
                        currentStatements.Add(element.Statement);
                        addingStatements = true;
                        break;
                }

                if (isLast && addingStatements)
                {
                    // Add the switch section.
                    sections.Add(new SwitchSection(errorRange, currentIsDefault, currentCases.ToArray(), currentStatements.ToArray()));
                }
            }

            return sections.ToArray();
        }

        public void Translate(ActionSet actionSet)
        {
            IWorkshopTree expression = Expression.Parse(actionSet);

            switchBuilder = new SwitchBuilder(actionSet);
            switchBuilder.AutoBreak = false;
            
            foreach (SwitchSection section in paths)
            {
                foreach (IExpression caseExpression in section.Cases)
                    switchBuilder.NextCase((Element)caseExpression.Parse(actionSet));
                
                if (section.IsDefault) switchBuilder.AddDefault();
                section.Block.Translate(actionSet);
            }

            switchBuilder.Finish((Element)expression);
        }

        public void AddBreak(ActionSet actionSet, string comment)
        {
            SkipStartMarker breaker = new SkipStartMarker(actionSet, comment);
            actionSet.AddAction(breaker);
            switchBuilder.SkipToEnd.Add(breaker);
        }

        public PathInfo[] GetPaths() => pathInfo;
    }

    class SwitchSection
    {
        public DocRange ErrorRange { get; }
        public bool IsDefault { get; }
        public IExpression[] Cases { get; }
        public BlockAction Block { get; }

        public SwitchSection(DocRange errorRange, bool isDefault, IExpression[] cases, IStatement[] statements)
        {
            ErrorRange = errorRange;
            IsDefault = isDefault;
            Cases = cases;
            Block = new BlockAction(statements);
        }
    }

    class SwitchElement
    {
        public SwitchElementType Type { get; }
        public IStatement Statement { get; }
        public IExpression Condition { get; }
        public DocRange ErrorRange { get; }

        public SwitchElement(DocRange range)
        {
            Type = SwitchElementType.Default;
            ErrorRange = range;
        }
        public SwitchElement(IStatement statement)
        {
            Type = SwitchElementType.Statement;
            Statement = statement;
        }
        public SwitchElement(DocRange range, IExpression condition)
        {
            Type = SwitchElementType.Case;
            Condition = condition;
            ErrorRange = range;
        }
    }

    enum SwitchElementType
    {
        Statement,
        Case,
        Default
    }
}