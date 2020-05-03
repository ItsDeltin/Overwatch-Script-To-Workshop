using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMethod : DefinedFunction
    {
        public readonly DeltinScriptParser.Define_methodContext context;
        private MethodAttributeHandler[] attributes;

        // Attributes
        public bool IsSubroutine { get; private set; }
        public string SubroutineName { get; private set; }

        // Block data
        public BlockAction block { get; private set; }

        /// <summary>If there is only one return statement, return the reference to
        /// the return expression instead of assigning it to a variable to reduce the number of actions.</summary>
        public bool multiplePaths;

        public DefinedMethod virtualSubroutineAssigned { get; set; }

        public SubroutineInfo subroutineInfo { get; private set; }

        public Scope BlockScope { get; }

        private readonly bool subroutineDefaultGlobal;

        public DefinedMethod(ParseInfo parseInfo, Scope objectScope, Scope staticScope, DeltinScriptParser.Define_methodContext context, CodeType containingType, bool addToScope)
            : base(parseInfo, context.name.Text, new Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)), addToScope)
        {
            this.context = context;

            Attributes.ContainingType = containingType;

            DocRange nameRange = DocRange.GetRange(context.name);

            // Get the attributes.
            GetAttributes();

            SetupScope(Static ? staticScope : objectScope);
            methodScope.MethodContainer = true;
            BlockScope = methodScope.Child();

            // Get the type.
            if (context.VOID() == null)
            {
                doesReturnValue = true;
                ReturnType = CodeType.GetCodeTypeFromContext(parseInfo, context.code_type());
            }

            // Setup the parameters and parse the block.
            if (!IsSubroutine)
                SetupParameters(context.setParameters(), false);
            else
            {
                subroutineDefaultGlobal = context.PLAYER() == null;
                Attributes.Parallelable = true;
                parseInfo.TranslateInfo.AddSubroutine(this);

                // Subroutines should not have parameters.
                SetupParameters(context.setParameters(), true);
            }

            // Override attribute.
            if (Attributes.Override)
            {
                IMethod overriding = objectScope.GetMethodOverload(this);

                // No method with the name and parameters found.
                if (overriding == null) parseInfo.Script.Diagnostics.Error("Could not find a method to override.", nameRange);
                else if (!overriding.Attributes.IsOverrideable) parseInfo.Script.Diagnostics.Error("The specified method is not marked as virtual.", nameRange);
                else overriding.Attributes.AddOverride(this);

                if (overriding != null && overriding.DefinedAt != null)
                {
                    // Make the override keyword go to the base method.
                    parseInfo.Script.AddDefinitionLink(
                        attributes.First(at => at.Type == MethodAttributeType.Override).Range,
                        overriding.DefinedAt
                    );

                    if (!Attributes.Recursive)
                        Attributes.Recursive = overriding.Attributes.Recursive;
                }
            }

            if (Attributes.IsOverrideable && AccessLevel == AccessLevel.Private)
                parseInfo.Script.Diagnostics.Error("A method marked as virtual or abstract must have the protection level 'public' or 'protected'.", nameRange);

            // Syntax error if the block is missing.
            if (context.block() == null) parseInfo.Script.Diagnostics.Error("Expected block.", nameRange);

            // Add to the scope. Check for conflicts if the method is not overriding.
            objectScope.AddMethod(this, parseInfo.Script.Diagnostics, nameRange, !Attributes.Override);

            // Add the hover info.
            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));

            if (Attributes.IsOverrideable)
                parseInfo.Script.AddCodeLensRange(new ImplementsCodeLensRange(this, parseInfo.Script, CodeLensSourceType.Function, nameRange));

            parseInfo.TranslateInfo.ApplyBlock(this);
        }

        private void GetAttributes()
        {
            // If the STRINGLITERAL is not null, the method will be stored in a subroutine.
            // Get the name of the rule the method will be stored in.
            if (context.STRINGLITERAL() != null)
            {
                SubroutineName = Extras.RemoveQuotes(context.STRINGLITERAL().GetText());
                IsSubroutine = true;
            }
            
            // method_attributes will ne null if there are no attributes.
            if (context.method_attributes() == null) return;

            int numberOfAttributes = context.method_attributes().Length;
            attributes = new MethodAttributeHandler[numberOfAttributes];

            // Loop through all attributes.
            for (int i = 0; i < numberOfAttributes; i++)
            {
                var newAttribute = new MethodAttributeHandler(context.method_attributes(i));
                attributes[i] = newAttribute;

                bool wasCopy = false;

                // If the attribute already exists, syntax error.
                for (int c = i - 1; c >= 0; c--)
                    if (attributes[c].Type == newAttribute.Type)
                    {
                        newAttribute.Copy(parseInfo.Script.Diagnostics);
                        wasCopy = true;
                        break;
                    }
                
                // Additonal syntax errors. Only throw if the attribute is not a copy.
                if (!wasCopy)
                {
                    // Virtual attribute on a static method (static attribute was first.)
                    if (Static && newAttribute.Type == MethodAttributeType.Virtual)
                        parseInfo.Script.Diagnostics.Error("Static methods cannot be virtual.", newAttribute.Range);
                    
                    // Static attribute on a virtual method (virtual attribute was first.)
                    if (Attributes.Virtual && newAttribute.Type == MethodAttributeType.Static)
                        parseInfo.Script.Diagnostics.Error("Virtual methods cannot be static.", newAttribute.Range);
                }
                
                // Apply the attribute.
                switch (newAttribute.Type)
                {
                    // Apply accessor
                    case MethodAttributeType.Accessor: AccessLevel = newAttribute.AttributeContext.accessor().GetAccessLevel(); break;
                    
                    // Apply static
                    case MethodAttributeType.Static: Static = true; break;
                    
                    // Apply virtual
                    case MethodAttributeType.Virtual: Attributes.Virtual = true; break;
                    
                    // Apply override
                    case MethodAttributeType.Override: Attributes.Override = true; break;
                    
                    // Apply Recursive
                    case MethodAttributeType.Recursive: Attributes.Recursive = true; break;
                }
            }
        }

        // Sets up the method's block.
        public override void SetupBlock()
        {
            if (context.block() != null)
            {
                block = new BlockAction(parseInfo.SetCallInfo(CallInfo), BlockScope, context.block());

                BlockTreeScan validation = new BlockTreeScan(doesReturnValue, parseInfo, this);
                validation.ValidateReturns();
                multiplePaths = validation.MultiplePaths;
            }
            foreach (var listener in listeners) listener.Applied();
        }

        // Parses the method.
        override public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            return MethodBuilder.Call(this, methodCall, actionSet);
        }

        // Sets up single-instance methods for methods with the 'rule' attribute.
        public void SetupSubroutine()
        {
            if (subroutineInfo != null || !IsSubroutine) return;

            // Setup the subroutine element.
            Subroutine subroutine = parseInfo.TranslateInfo.SubroutineCollection.NewSubroutine(Name);

            // Create the rule.
            TranslateRule subroutineRule = new TranslateRule(parseInfo.TranslateInfo, subroutine, SubroutineName, subroutineDefaultGlobal);

            // Setup the return handler.
            ReturnHandler returnHandler = new ReturnHandler(subroutineRule.ActionSet, Name, multiplePaths || Attributes.Virtual);
            ActionSet actionSet = subroutineRule.ActionSet.New(returnHandler).New(subroutineRule.ActionSet.IndexAssigner.CreateContained());

            // Get the variables that will be used to store the parameters.
            IndexReference[] parameterStores = new IndexReference[ParameterVars.Length];
            for (int i = 0; i < ParameterVars.Length; i++)
            {
                // Create the workshop variable the parameter will be stored as.
                IndexReference indexResult = actionSet.IndexAssigner.AddIndexReference(actionSet.VarCollection, ParameterVars[i], subroutineDefaultGlobal, Attributes.Recursive);
                parameterStores[i] = indexResult;

                // Assign virtual variables to the index reference.
                foreach (Var virtualParameterOption in VirtualVarGroup(i))
                    actionSet.IndexAssigner.Add(virtualParameterOption, indexResult);
            }
            
            // If the subroutine is an object function inside a class, create a variable to store the class object.
            IndexReference objectStore = null;
            if (Attributes.ContainingType != null && !Static)
            {
                objectStore = actionSet.VarCollection.Assign("_" + Name + "_subroutineStore", true, !Attributes.Recursive);

                // Set the objectStore as an empty array if the subroutine is recursive.
                if (Attributes.Recursive)
                {
                    actionSet.InitialSet().AddAction(objectStore.SetVariable(new V_EmptyArray()));
                    Attributes.ContainingType.AddObjectVariablesToAssigner(Element.Part<V_LastOf>(objectStore.GetVariable()), actionSet.IndexAssigner);
                    actionSet = actionSet.New(Element.Part<V_LastOf>(objectStore.GetVariable())).PackThis();
                }
                else
                {
                    Attributes.ContainingType.AddObjectVariablesToAssigner(objectStore.GetVariable(), actionSet.IndexAssigner);
                    actionSet = actionSet.New(objectStore.GetVariable()).PackThis();
                }
            }
            
            // Set the subroutine info.
            subroutineInfo = new SubroutineInfo(subroutine, returnHandler, subroutineRule, parameterStores, objectStore);

            MethodBuilder builder = new MethodBuilder(this, actionSet, returnHandler);
            builder.BuilderSet = builder.BuilderSet.New(Attributes.Recursive);
            builder.ParseInner();

            // Apply returns.
            returnHandler.ApplyReturnSkips();

            // Pop object array and parameters if recursive.
            if (Attributes.Recursive)
            {
                if (objectStore != null) actionSet.AddAction(objectStore.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(objectStore.GetVariable()) - 1));
                RecursiveStack.PopParameterStacks(actionSet, ParameterVars);
            }

            // Add the subroutine.
            Rule translatedRule = subroutineRule.GetRule();
            parseInfo.TranslateInfo.WorkshopRules.Add(translatedRule);

            var codeLens = new ElementCountCodeLens(DefinedAt.range, parseInfo.TranslateInfo.OptimizeOutput);
            parseInfo.Script.AddCodeLensRange(codeLens);
            codeLens.RuleParsed(translatedRule);
        }

        public Var[] VirtualVarGroup(int i)
        {
            List<Var> parameters = new List<Var>();

            foreach (var overrider in Attributes.AllOverrideOptions())
                parameters.Add(((DefinedMethod)overrider).ParameterVars[i]);
            
            return parameters.ToArray();
        }

        public void AssignParameters(ActionSet actionSet, IWorkshopTree[] parameterValues, bool recursive)
        {
            for (int i = 0; i < ParameterVars.Length; i++)
            {
                IGettable indexResult = actionSet.IndexAssigner.Add(actionSet.VarCollection, ParameterVars[i], actionSet.IsGlobal, parameterValues?[i], recursive);

                if (indexResult is IndexReference indexReference && parameterValues?[i] != null)
                    actionSet.AddAction(indexReference.SetVariable((Element)parameterValues[i]));

                foreach (Var virtualParameterOption in VirtualVarGroup(i))
                    actionSet.IndexAssigner.Add(virtualParameterOption, indexResult);
            }
        }
    
        // Assigns parameters to the index assigner. TODO: Move to OverloadChooser.
        public static void AssignParameters(ActionSet actionSet, Var[] parameterVars, IWorkshopTree[] parameterValues, bool recursive = false)
        {
            for (int i = 0; i < parameterVars.Length; i++)
            {
                actionSet.IndexAssigner.Add(actionSet.VarCollection, parameterVars[i], actionSet.IsGlobal, parameterValues?[i], recursive);

                if (actionSet.IndexAssigner[parameterVars[i]] is IndexReference && parameterValues?[i] != null)
                    actionSet.AddAction(
                        ((IndexReference)actionSet.IndexAssigner[parameterVars[i]]).SetVariable((Element)parameterValues[i])
                    );
            }
        }
    }

    class MethodAttributeHandler
    {
        public MethodAttributeType Type { get; }
        public DocRange Range { get; }
        public DeltinScriptParser.Method_attributesContext AttributeContext { get; }

        public MethodAttributeHandler(DeltinScriptParser.Method_attributesContext attributeContext)
        {
            AttributeContext = attributeContext; 
            Range = DocRange.GetRange(attributeContext);

            if (attributeContext.accessor() != null) Type = MethodAttributeType.Accessor;
            else if (attributeContext.STATIC() != null) Type = MethodAttributeType.Static;
            else if (attributeContext.VIRTUAL() != null) Type = MethodAttributeType.Virtual;
            else if (attributeContext.OVERRIDE() != null) Type = MethodAttributeType.Override;
            else if (attributeContext.RECURSIVE() != null) Type = MethodAttributeType.Recursive;
            else throw new NotImplementedException();
        }

        public void Copy(FileDiagnostics diagnostics)
        {
            diagnostics.Error($"Multiple '{Type.ToString().ToLower()}' attributes.", Range);
        }
    }

    enum MethodAttributeType
    {
        Accessor,
        Static,
        Override,
        Virtual,
        Recursive
    }
}