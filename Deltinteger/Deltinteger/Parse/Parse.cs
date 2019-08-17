using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class ParsingData
    {
        private static readonly Log Log = new Log("Parse");

        public static ParsingData GetParser(string document, string uri)
        {
            return new ParsingData(document, uri);
        }

        private ParsingData(string document, string uri)
        {
            URI = uri;

            Rule initialGlobalValues = new Rule(Constants.INTERNAL_ELEMENT + "Initial Global Values");
            Rule initialPlayerValues = new Rule(Constants.INTERNAL_ELEMENT + "Initial Player Values", RuleEvent.OngoingPlayer, Team.All, PlayerSelector.All);
            TranslateRule globalTranslate = new TranslateRule(initialGlobalValues, Root, this);
            TranslateRule playerTranslate = new TranslateRule(initialPlayerValues, Root, this);

            // The looper rule
            GlobalLoop = new Looper(true);
            PlayerLoop = new Looper(false);
            
            VarCollection = new VarCollection();
            Root = new ScopeGroup(VarCollection);
            UserMethods = new List<UserMethod>();
            DefinedTypes = new List<DefinedType>();

            GetObjects(document, URI, globalTranslate, playerTranslate);

            // Parse the rules.
            Rules = new List<Rule>();

            for (int i = 0; i < RuleNodes.Count; i++)
            {
                try
                {
                    var result = TranslateRule.GetRule(RuleNodes[i], Root, this);
                    Rules.Add(result);
                }
                catch (SyntaxErrorException ex)
                {
                    Diagnostics.Error(ex);
                }
            }

            globalTranslate.Finish();
            playerTranslate.Finish();

            if (initialGlobalValues.Actions.Length > 0)
                Rules.Add(initialGlobalValues);
            if (initialPlayerValues.Actions.Length > 0)
                Rules.Add(initialPlayerValues);
            if (GlobalLoop.Used)
                Rules.Add(GlobalLoop.Finalize());
            if (PlayerLoop.Used)
                Rules.Add(PlayerLoop.Finalize());

            Success = Diagnostics.ContainsErrors();
        }

        private RulesetNode GetRuleset(string document)
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

            Log.Write(LogLevel.Verbose, ruleSetContext.ToStringTree(parser));

            // Get the ruleset node.
            RulesetNode ruleset = null;
            if (!Diagnostics.ContainsErrors())
            {
                BuildAstVisitor bav = new BuildAstVisitor(Diagnostics);
                ruleset = (RulesetNode)bav.Visit(ruleSetContext);
            }

            AdditionalErrorChecking aec = new AdditionalErrorChecking(parser, Diagnostics);
            aec.Visit(ruleSetContext);

            return ruleset;
        }

        private void GetObjects(string document, string referenceFile, TranslateRule globalTranslate, TranslateRule playerTranslate)
        {
            RulesetNode ruleset = GetRuleset(document);

            if (ruleset != null)
            {
                // Get the defined types
                foreach (var definedType in ruleset.DefinedTypes)
                    try
                    {
                        DefinedTypes.Add(new DefinedType(definedType));
                    }
                    catch (SyntaxErrorException ex)
                    {
                        Diagnostics.Error(ex);
                    }

                // Get the variables
                foreach (var definedVar in ruleset.DefinedVars)
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

                // Get the user methods.
                for (int i = 0; i < ruleset.UserMethods.Length; i++)
                    try
                    {
                        UserMethods.Add(new UserMethod(Root, ruleset.UserMethods[i]));
                    }
                    catch (SyntaxErrorException ex)
                    {
                        Diagnostics.Error(ex);
                    }

                // Get the rules
                RuleNodes.AddRange(ruleset.Rules);

                // Check the imported files.
                foreach (ImportNode importNode in ruleset.Imports)
                    try
                    {
                        string directory = Path.GetDirectoryName(referenceFile);
                        string combined = Path.Combine(directory, importNode.File);
                        string uri = Path.GetFullPath(combined);

                        if (!File.Exists(uri))
                            throw SyntaxErrorException.ImportFileNotFound(importNode.File, importNode.Range);

                        string content = File.ReadAllText(uri);
                        
                        GetObjects(content, uri, globalTranslate, playerTranslate);
                    }
                    catch (SyntaxErrorException ex)
                    {
                        Diagnostics.Error(ex);
                    }
            }
        }

        public Diagnostics Diagnostics { get; private set; } = new Diagnostics();
        public List<Rule> Rules { get; private set; }
        private List<RuleNode> RuleNodes { get; set; } = new List<RuleNode>();
        public List<DefinedType> DefinedTypes { get; private set; }
        public List<UserMethod> UserMethods { get; private set; }
        public bool Success { get; private set; }
        public VarCollection VarCollection { get; private set; } = new VarCollection();
        public ScopeGroup Root { get; private set; }
        public RulesetNode RuleSetNode { get; private set; }
        private Looper GlobalLoop { get; set; }
        private Looper PlayerLoop { get; set; }
        private string URI { get; set; }

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