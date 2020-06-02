using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class SwitchAction : IStatement, IBlockContainer, IBreakContainer
    {
        private readonly IExpression Expression;
        private readonly SwitchSection[] paths;
        private readonly PathInfo[] pathInfo;
        private SwitchBuilder switchBuilder;

        public SwitchAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.SwitchContext switchContext)
        {
            // Get the expression.
            if (switchContext.expr() == null) parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(switchContext.RIGHT_PAREN()));
            else Expression = parseInfo.GetExpression(scope, switchContext.expr());

            paths = GetSections(ResolveElements(parseInfo.SetBreakHandler(this), scope, switchContext));
            pathInfo = new PathInfo[paths.Length];

            for (int i = 0; i < pathInfo.Length; i++)
                pathInfo[i] = new PathInfo(paths[i].Block, paths[i].ErrorRange, paths[i].IsDefault);
        }

        private SwitchElement[] ResolveElements(ParseInfo parseInfo, Scope scope, DeltinScriptParser.SwitchContext switchContext)
        {
            List<SwitchElement> elements = new List<SwitchElement>();
            bool inSection = false;
            bool caseError = false;
            bool gotDefault = false;

            // Resolve paths.
            foreach (var switchElement in switchContext.switch_element())
            {
                // Syntax error if there is a statement before a case.
                if (switchElement.documented_statement() != null && !inSection && !caseError)
                {
                    parseInfo.Script.Diagnostics.Error("Expected case or default.", DocRange.GetRange(switchElement));
                    caseError = true;
                }

                // Don't throw the syntax error multiple times in one switch.
                if (switchElement.DEFAULT() != null || switchElement.@case() != null) inSection = true;

                // Default case.
                if (switchElement.DEFAULT() != null)
                {
                    if (gotDefault) parseInfo.Script.Diagnostics.Error("Switch cannot have multiple defaults.", DocRange.GetRange(switchElement));
                    gotDefault = true;
                }

                // Get the statement
                if (switchElement.documented_statement() != null) elements.Add(new SwitchElement(parseInfo.GetStatement(scope, switchElement.documented_statement())));
                // Get the case
                else if (switchElement.@case() != null) elements.Add(new SwitchElement(DocRange.GetRange(switchElement.@case().CASE()), parseInfo.GetExpression(scope, switchElement.@case().expr())));
                // Get default
                else if (switchElement.DEFAULT() != null) elements.Add(new SwitchElement(DocRange.GetRange(switchElement.DEFAULT())));
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

        public void AddBreak(ActionSet actionSet)
        {
            SkipStartMarker breaker = new SkipStartMarker(actionSet);
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