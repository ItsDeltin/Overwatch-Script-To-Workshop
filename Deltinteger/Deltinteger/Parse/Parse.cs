using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class ParsingData
    {
        public static ParsingData GetParser(string document)
        {
            return new ParsingData(document);
        }

        private ParsingData(string document)
        {
            AntlrInputStream inputStream = new AntlrInputStream(document);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            var errorListener = new ErrorListener(Diagnostics);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            DeltinScriptParser.RulesetContext ruleSetContext = parser.ruleset();

            Log log = new Log("Parse");
            log.Write(LogLevel.Verbose, ruleSetContext.ToStringTree(parser));

            // Get the ruleset node.
            if (!Diagnostics.ContainsErrors())
            {
                Bav = new BuildAstVisitor(Diagnostics);
                RuleSetNode = (RulesetNode)Bav.Visit(ruleSetContext);
            }

            AdditionalErrorChecking aec = new AdditionalErrorChecking(parser, Diagnostics);
            aec.Visit(ruleSetContext);
            
            if (!Diagnostics.ContainsErrors())
            {
                VarCollection = new VarCollection();
                Root = new ScopeGroup(VarCollection);
                UserMethods = new List<UserMethod>();
                DefinedTypes = new List<DefinedType>();

                Rule initialGlobalValues = new Rule("Initial Global Values");
                Rule initialPlayerValues = new Rule("Initial Player Values", RuleEvent.OngoingPlayer, Team.All, PlayerSelector.All);

                TranslateRule globalTranslate = new TranslateRule(initialGlobalValues, Root, this);
                TranslateRule playerTranslate = new TranslateRule(initialPlayerValues, Root, this);

                // Get the defined types
                foreach (var definedType in RuleSetNode.DefinedTypes)
                    try
                    {
                        DefinedTypes.Add(new DefinedType(definedType));
                    }
                    catch (SyntaxErrorException ex)
                    {
                        Diagnostics.Error(ex);
                    }

                // Get the variables
                foreach (var definedVar in RuleSetNode.DefinedVars)
                    try
                    {
                        IndexedVar var;
                        if (definedVar.UseVar == null)
                            var = VarCollection.AssignVar(Root, definedVar.VariableName, definedVar.IsGlobal, definedVar);
                        else
                            var = VarCollection.AssignVar(
                                Root, 
                                definedVar.VariableName, 
                                definedVar.IsGlobal,
                                definedVar.UseVar.Variable, 
                                definedVar.UseVar.Index,
                                definedVar
                            );
                        if (definedVar.Type != null)
                            var.Type = GetDefinedType(definedVar.Type, definedVar.Range);

                        // Set initial values
                        if (definedVar.Value != null)
                            if (definedVar.IsGlobal)
                                globalTranslate.Actions.AddRange(var.SetVariable(globalTranslate.ParseExpression(Root, definedVar.Value)));
                            else
                                playerTranslate.Actions.AddRange(var.SetVariable(playerTranslate.ParseExpression(Root, definedVar.Value)));
                    }
                    catch (SyntaxErrorException ex)
                    {
                        Diagnostics.Error(ex);
                    }

                globalTranslate.Finish();
                playerTranslate.Finish();

                // Get the user methods.
                for (int i = 0; i < RuleSetNode.UserMethods.Length; i++)
                    try
                    {
                        UserMethods.Add(new UserMethod(Root, RuleSetNode.UserMethods[i]));
                    }
                    catch (SyntaxErrorException ex)
                    {
                        Diagnostics.Error(ex);
                    }

                // Parse the rules.
                Rules = new List<Rule>();

                if (initialGlobalValues.Actions.Length > 0)
                    Rules.Add(initialGlobalValues);
                
                if (initialPlayerValues.Actions.Length > 0)
                    Rules.Add(initialPlayerValues);

                // The looper rule
                GlobalLoop = new Looper(true);
                PlayerLoop = new Looper(false);

                for (int i = 0; i < RuleSetNode.Rules.Length; i++)
                {
                    try
                    {
                        var result = TranslateRule.GetRule(RuleSetNode.Rules[i], Root, this);
                        Rules.Add(result);
                    }
                    catch (SyntaxErrorException ex)
                    {
                        Diagnostics.Error(ex);
                    }
                }

                if (GlobalLoop.Used)
                    Rules.Add(GlobalLoop.Finalize());
                if (PlayerLoop.Used)
                    Rules.Add(PlayerLoop.Finalize());

                Success = true;
            }
        }

        public Diagnostics Diagnostics { get; private set; } = new Diagnostics();
        public BuildAstVisitor Bav { get; private set; }
        public List<Rule> Rules { get; private set; }
        public List<DefinedType> DefinedTypes { get; private set; }
        public List<UserMethod> UserMethods { get; private set; }
        public bool Success { get; private set; }
        public VarCollection VarCollection { get; private set; }
        public ScopeGroup Root { get; private set; }
        public RulesetNode RuleSetNode { get; private set; }
        private Looper GlobalLoop { get; set; }
        private Looper PlayerLoop { get; set; }

        public IMethod GetMethod(string name)
        {
            return (IMethod)UserMethods?.FirstOrDefault(um => um.Name == name) 
            ?? (IMethod)CustomMethodData.GetCustomMethod(name) 
            ?? (IMethod)Element.GetElement(name);
        }

        public DefinedType GetDefinedType(string name, Range range)
        {
            return DefinedTypes.FirstOrDefault(dt => dt.Name == name) ?? throw SyntaxErrorException.NonexistentType(name, range);
        }

        public Looper GetLooper(bool isGlobal)
        {
            return isGlobal? GlobalLoop : PlayerLoop;
        }
    }
}