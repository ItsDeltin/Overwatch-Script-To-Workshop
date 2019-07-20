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
            var errorListener = new ErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            DeltinScriptParser.RulesetContext ruleSetContext = parser.ruleset();

            Log log = new Log("Parse");
            log.Write(LogLevel.Verbose, ruleSetContext.ToStringTree(parser));

            Diagnostics = new List<Diagnostic>();
            Diagnostics.AddRange(errorListener.Errors);

            // Get the ruleset node.
            if (Diagnostics.Count == 0)
            {
                Bav = new BuildAstVisitor(Diagnostics);
                RuleSetNode = (RulesetNode)Bav.Visit(ruleSetContext);
            }

            AdditionalErrorChecking aec = new AdditionalErrorChecking(parser, Diagnostics);
            aec.Visit(ruleSetContext);

            if (Diagnostics.Count == 0)
            {
                VarCollection = new VarCollection();
                ScopeGroup root = new ScopeGroup(VarCollection);
                UserMethods = new List<UserMethod>();
                DefinedTypes = new List<DefinedType>();

                // Get the defined types
                foreach (var definedType in RuleSetNode.DefinedTypes)
                    DefinedTypes.Add(new DefinedType(definedType));

                // Get the variables
                foreach (var definedVar in RuleSetNode.DefinedVars)
                    if (definedVar.UseVar == null)
                        VarCollection.AssignVar(root, definedVar.VariableName, definedVar.IsGlobal, definedVar);
                    else
                        VarCollection.AssignVar(
                            root, 
                            definedVar.VariableName, 
                            definedVar.IsGlobal,
                            definedVar.UseVar.Variable, 
                            definedVar.UseVar.Index,
                            definedVar
                        );

                // Get the user methods.
                for (int i = 0; i < RuleSetNode.UserMethods.Length; i++)
                    UserMethods.Add(new UserMethod(root, RuleSetNode.UserMethods[i]));

                // Parse the rules.
                Rules = new List<Rule>();

                // The looper rule
                GlobalLoop = new Looper(true);
                PlayerLoop = new Looper(false);

                for (int i = 0; i < RuleSetNode.Rules.Length; i++)
                {
                    try
                    {
                        var result = TranslateRule.GetRule(RuleSetNode.Rules[i], root, this);
                        Rules.Add(result);
                    }
                    catch (SyntaxErrorException ex)
                    {
                        Diagnostics.Add(new Diagnostic(ex.GetInfo(), ex.Range) { severity = Diagnostic.Error });
                    }
                }

                if (GlobalLoop.Used)
                    Rules.Add(GlobalLoop.Finalize());
                if (PlayerLoop.Used)
                    Rules.Add(PlayerLoop.Finalize());

                Success = true;
            }
        }

        public List<Diagnostic> Diagnostics;
        public BuildAstVisitor Bav { get; private set; }
        public List<Rule> Rules { get; private set; }
        public List<DefinedType> DefinedTypes { get; private set; }
        public List<UserMethod> UserMethods { get; private set; }
        public bool Success { get; private set; }
        public VarCollection VarCollection { get; private set; }
        public RulesetNode RuleSetNode { get; private set; }
        private Looper GlobalLoop { get; set; }
        private Looper PlayerLoop { get; set; }

        public IMethod GetMethod(string name)
        {
            return (IMethod)UserMethods?.FirstOrDefault(um => um.Name == name) 
            ?? (IMethod)CustomMethodData.GetCustomMethod(name) 
            ?? (IMethod)Element.GetElement(name);
        }

        public DefinedType GetDefinedType(string name)
        {
            return DefinedTypes.FirstOrDefault(dt => dt.Name == name);
        }

        public Looper GetLooper(bool isGlobal)
        {
            return isGlobal? GlobalLoop : PlayerLoop;
        }
    }
}