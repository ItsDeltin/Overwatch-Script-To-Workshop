using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public abstract class VarBuilder
    {
        protected readonly IVarContextHandler _contextHandler;
        protected ParseInfo _parseInfo;
        protected FileDiagnostics _diagnostics;
        protected DocRange _nameRange;
        protected DocRange _typeRange;

        protected VarBuilderAttribute[] _attributes;
        protected VarInfo _varInfo;
        protected bool _canInferType = false;

        public VarBuilder(IVarContextHandler contextHandler)
        {
            _contextHandler = contextHandler;
        }

        public Var GetVar()
        {
            _parseInfo = _contextHandler.ParseInfo;
            _diagnostics = _parseInfo.Script.Diagnostics;
            _nameRange = _contextHandler.GetNameRange();

            // Get the attributes.
            _attributes = _contextHandler.GetAttributes();

            // Stores all values in the enum AttributeType as a list.
            List<AttributeType> missingTypes = Enum.GetValues(typeof(AttributeType)).Cast<AttributeType>().ToList();

            // Filter missing attributes.
            foreach (VarBuilderAttribute attribute in _attributes)
                missingTypes.Remove(attribute.Type);
            
            // Check missing attributes.
            MissingAttribute(missingTypes.ToArray());

            // Check existing attributes.
            CheckAttributes();

            // Create the varinfo.
            _varInfo = new VarInfo(_contextHandler.GetName(), _contextHandler.GetDefineLocation(), _parseInfo);

            // Apply attributes.
            foreach (VarBuilderAttribute attribute in _attributes)
                attribute.Apply(_varInfo);
            
            // Get the variable type.
            _typeRange = _contextHandler.GetTypeRange();
            GetCodeType();

            if (_varInfo.Type is Lambda.PortableLambdaType)
                _varInfo.TokenType = TokenType.Function;
            
            Apply();
            TypeCheck();
            _varInfo.Recursive = IsRecursive();

            // Set the scope.
            var scope = OperationalScope();
            _varInfo.OperationalScope = scope;

            // Get the resulting variable.
            var result = new Var(_varInfo);

            // Add the variable to the operational scope.
            if (_contextHandler.CheckName()) scope.AddVariable(result, _diagnostics, _nameRange);
            else scope.CopyVariable(result);

            return result;
        }

        protected void RejectAttributes(params AttributeType[] types)
        {
            // Rejects attributes whos type is in the types array.
            foreach (VarBuilderAttribute attribute in _attributes)
                if (types.Contains(attribute.Type))
                    attribute.Reject(_diagnostics);
        }

        protected virtual void GetCodeType()
        {
            if (_contextHandler.GetCodeType() == null) return;

            if (_canInferType && _contextHandler.GetCodeType().Infer && _varInfo.InitialValueContext != null)
            {
                _varInfo.InferType = true;
            }
            else
            {
                // Get the type.
                CodeType type = CodeType.GetCodeTypeFromContext(_parseInfo, _contextHandler.GetCodeType());
                ApplyCodeType(type);
            }
        }

        protected void ApplyCodeType(CodeType type)
        {
            _varInfo.VariableTypeHandler.SetType(type);
            _varInfo.Type = type;
        }

        protected virtual void MissingAttribute(AttributeType[] attributeTypes) {}
        protected abstract void CheckAttributes();
        protected abstract void Apply();
        protected abstract Scope OperationalScope();

        protected virtual void TypeCheck()
        {
            // If the type of the variable is a constant workshop value and there is no initial value, throw a syntax error.
            if (_varInfo.Type != null && _varInfo.Type.IsConstant() && _varInfo.InitialValueContext == null)
                _diagnostics.Error("Variables with constant workshop types must have an initial value.", _nameRange);
        }

        protected virtual bool IsRecursive()
        {
            return _parseInfo.CurrentCallInfo != null && _parseInfo.CurrentCallInfo.Function is IMethod iMethod && iMethod.Attributes.Recursive;
        }

        public static implicit operator Var(VarBuilder builder) => builder.GetVar();
    }

    public interface IVarContextHandler
    {
        ParseInfo ParseInfo { get; }
        Location GetDefineLocation();
        string GetName();
        DocRange GetNameRange();
        VarBuilderAttribute[] GetAttributes();
        IParseType GetCodeType();
        DocRange GetTypeRange();
        bool CheckName();
    }

    public class VarBuilderAttribute
    {
        public AttributeType Type { get; }
        public DocRange Range { get; }

        public VarBuilderAttribute(AttributeType type, DocRange range)
        {
            Type = type;
            Range = range;
        }

        public void Reject(FileDiagnostics diagnostics)
        {
            switch (Type)
            {
                // Accessors
                case AttributeType.Public:
                case AttributeType.Protected:
                case AttributeType.Private:
                    diagnostics.Error("Accessor not valid here.", Range);
                    break;
                
                // Workshop ID override
                case AttributeType.ID:
                    diagnostics.Error($"Cannot override workshop variable ID here.", Range);
                    break;
                
                // Extended collection
                case AttributeType.Ext:
                    diagnostics.Error($"Cannot put variable in the extended collection.", Range);
                    break;
                
                // Initial value
                case AttributeType.Initial:
                    diagnostics.Error($"Variable cannot have an initial value.", Range);
                    break;
                
                // Use attribute name
                case AttributeType.Static:
                case AttributeType.Globalvar:
                case AttributeType.Playervar:
                case AttributeType.Ref:
                default:
                    diagnostics.Error($"'{Type.ToString().ToLower()}' attribute not valid here.", Range);
                    break;
            }
        }
    
        public virtual void Apply(VarInfo varInfo)
        {
            switch (Type)
            {
                // Extended collection attribute.
                case AttributeType.Ext:
                    varInfo.InExtendedCollection = true;
                    break;
                
                // Access levels
                case AttributeType.Public: varInfo.AccessLevel = AccessLevel.Public; break;
                case AttributeType.Protected: varInfo.AccessLevel = AccessLevel.Protected; break;
                case AttributeType.Private: varInfo.AccessLevel = AccessLevel.Private; break;

                // globalvar
                case AttributeType.Globalvar:
                    varInfo.VariableTypeHandler.SetAttribute(true);
                    break;
                
                // playervar
                case AttributeType.Playervar:
                    varInfo.VariableTypeHandler.SetAttribute(false);
                    break;
                
                // ref
                case AttributeType.Ref:
                    varInfo.VariableTypeHandler.SetWorkshopReference();
                    break;
                
                // Static
                case AttributeType.Static:
                    varInfo.Static = true;
                    break;
                
                // Should be handled by overrides.
                case AttributeType.ID:
                case AttributeType.Initial:
                // Missing attribute function
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class IDAttribute : VarBuilderAttribute
    {
        public int ID { get; }

        public IDAttribute(Token numberToken) : base(AttributeType.ID, numberToken.Range)
        {
            ID = int.Parse(numberToken.Text);
        }

        public override void Apply(VarInfo varInfo)
        {
            varInfo.ID = ID;
        }
    }

    public class InitialValueAttribute : VarBuilderAttribute
    {
        public IParseExpression ExprContext { get; }

        public InitialValueAttribute(IParseExpression exprContext) : base(AttributeType.Initial, exprContext.Range)
        {
            ExprContext = exprContext;
        }

        public override void Apply(VarInfo varInfo)
        {
            varInfo.InitialValueContext = ExprContext;
        }
    }

    public enum AttributeType
    {
        Public, Protected, Private, Static, Globalvar, Playervar, Ref, ID, Ext, Initial
    }

    public class VariableTypeHandler
    {
        private readonly VarInfo _varInfo;
        private bool _preset;
        private bool _presetIsGlobal;
        private bool _setType;
        private CodeType _setTypeValue;
        private bool _isWorkshopReference;

        public VariableTypeHandler(VarInfo varInfo)
        {
            _varInfo = varInfo;
        }

        public void SetAttribute(bool isGlobal)
        {
            if (_preset)
                throw new Exception("Preset cannot be set more than once.");

            _preset = true;
            _presetIsGlobal = isGlobal;
        }

        public void SetType(CodeType type)
        {
            if (_setType)
                throw new Exception("Type cannot be set more than once.");
            
            if (type == null)
                return;

            _setType = true;
            _setTypeValue = type;
            _isWorkshopReference = _isWorkshopReference || _setTypeValue.IsConstant();
        }

        public void SetWorkshopReference()
        {
            _isWorkshopReference = true;
        }

        public VariableType GetVariableType()
        {
            if (_isWorkshopReference || (_setType && _setTypeValue.IsConstant()))
                return VariableType.ElementReference;

            else if (_preset)
                return _presetIsGlobal ? VariableType.Global : VariableType.Player;
            
            else
                return VariableType.Dynamic;
        }

        public StoreType GetStoreType()
        {
            // Set the variable and store types.
            if (_isWorkshopReference)
                return StoreType.None;
            // In extended collection.
            else if (_varInfo.InExtendedCollection)
                return StoreType.Indexed;
            // Full workshop variable.
            else
                return StoreType.FullVariable;
        }
    }
}