using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    public abstract class VarBuilder
    {
        protected readonly IVarContextHandler _contextHandler;
        protected readonly IScopeHandler _scopeHandler;
        protected ParseInfo _parseInfo;
        protected FileDiagnostics _diagnostics;
        protected string _name;
        protected DocRange _nameRange;
        protected DocRange _typeRange;

        protected VariableComponentCollection ComponentCollection { get; private set; }
        protected VarInfo _varInfo;
        protected Scope _scope;
        protected bool _canInferType = false;

        public VarBuilder(IScopeHandler scopeHandler, IVarContextHandler contextHandler)
        {
            _scopeHandler = scopeHandler;
            _contextHandler = contextHandler;
        }

        public Var GetVar()
        {
            _parseInfo = _contextHandler.ParseInfo;
            _diagnostics = _parseInfo.Script.Diagnostics;
            _nameRange = _contextHandler.GetNameRange();
            _name = _contextHandler.GetName() ?? "?";

            // Get then check components.
            ComponentCollection = new VariableComponentCollection(_diagnostics);
            _contextHandler.GetComponents(ComponentCollection);
            CheckComponents();
            ComponentCollection.FinishedObtainingComponents();

            _scope = _scopeHandler.GetScope(ComponentCollection.IsAttribute(AttributeType.Static));

            // Create the varinfo.
            _varInfo = new VarInfo(_name, _contextHandler.GetDefineLocation(), _parseInfo);
            _varInfo.Scope = _scope;

            // Get the variable type.
            _typeRange = _contextHandler.GetTypeRange();
            GetCodeType();

            // Get the variable being overriden.
            GetOverridenVariable();

            // If the type of the variable is PortableLambdaType, set the TokenType to Function
            if (_varInfo.Type is Lambda.PortableLambdaType)
                _varInfo.TokenType = SemanticTokenType.Function;

            // Apply attributes.
            ComponentCollection.Apply(_varInfo);

            Apply();
            TypeCheck();
            _varInfo.Recursive = IsRecursive();

            // Get the resulting variable.
            var result = new Var(_varInfo);

            // Add the variable to the operational scope.
            if (_nameRange != null)
                _scopeHandler.CheckConflict(_parseInfo, new(_name), _nameRange);
            _scopeHandler.Add(result.GetDefaultInstance(_scopeHandler.DefinedIn()), ComponentCollection.IsAttribute(AttributeType.Static));

            // Done
            return result;
        }

        protected virtual void GetCodeType()
        {
            if (_canInferType && (_contextHandler.GetCodeType() == null || _contextHandler.GetCodeType().Infer) && ComponentCollection.IsComponent<InitialValueComponent>())
            {
                _varInfo.InferType = true;
            }
            else
            {
                CodeType type;

                if (_contextHandler.GetCodeType() == null)
                    type = _parseInfo.TranslateInfo.Types.Any();
                else
                    // Get the type.
                    type = TypeFromContext.GetCodeTypeFromContext(
                        _parseInfo,
                        _scope,
                        _contextHandler.GetCodeType()
                    );

                ApplyCodeType(type);
            }
        }

        protected virtual void ApplyCodeType(CodeType type)
        {
            _varInfo.VariableTypeHandler.SetType(type);
            _varInfo.Type = type;
        }

        protected abstract void CheckComponents();
        protected abstract void Apply();

        protected virtual void TypeCheck()
        {
            if (_nameRange == null) return;

            // If the type of the variable is a constant workshop value and there is no initial value, throw a syntax error.
            if (_varInfo.Type != null && _varInfo.Type.IsConstant() && _varInfo.InitialValueContext == null)
                _diagnostics.Error("Variables with constant workshop types must have an initial value", _nameRange);

            if (ComponentCollection.IsAttribute(AttributeType.Static) && SemanticsHelper.RecursivelyGetGenerics(_varInfo.Type).Any(type => type is AnonymousType))
                _diagnostics.Error("Static variables cannot use anonymous types", _nameRange);
        }

        private void GetOverridenVariable()
        {
            // No attribute is being overriden.
            if (!ComponentCollection.IsAttribute(AttributeType.Override)) return;

            var overriding = _scopeHandler.GetOverridenVariable(_name);
            var overridingType = overriding.CodeType.GetCodeType(_parseInfo.TranslateInfo);

            // Make sure the overriden variable's type matches.
            if (!overridingType.Is(_varInfo.Type))
                _parseInfo.Script.Diagnostics.Error($"'{_name}' type must be {overridingType.GetName()}", _typeRange);

            _varInfo.Overriding = overriding;
        }

        /// <summary>Determines if the variable should support recursion.</summary>
        /// <returns>True if the function the variable was declared in is marked as recursive.</returns>
        protected virtual bool IsRecursive()
            => _parseInfo.CurrentCallInfo != null && _parseInfo.CurrentCallInfo.Function is IMethod iMethod && iMethod.Attributes.Recursive;

        /// <summary>Rejects variable components.</summary>
        /// <param name="rejectComponents">The rejectors that will be reject the components.</param>
        protected void RejectAttributes(params IComponentIdentifier[] rejectComponents)
        {
            // Rejects attributes whos type is in the types array.
            foreach (var reject in rejectComponents)
                ComponentCollection.RejectComponent(reject);
        }

        /// <summary>If the variable is not a macro, the 'virtual' and 'override' attributes will be rejected.</summary>
        protected void RejectVirtualIfNotMacro()
        {
            // If the variable is not a macro, disallow 'virtual' and 'override'.
            if (!ComponentCollection.IsComponent<MacroComponent>())
                RejectAttributes(new AttributeComponentIdentifier(AttributeType.Virtual, AttributeType.Override));
        }
    }
}