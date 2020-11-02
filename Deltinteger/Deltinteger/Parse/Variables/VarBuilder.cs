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

            // Get the variable type.
            _typeRange = _contextHandler.GetTypeRange();
            GetCodeType();

            if (_varInfo.Type is Lambda.PortableLambdaType)
                _varInfo.TokenType = TokenType.Function;

            // Apply attributes.
            foreach (VarBuilderAttribute attribute in _attributes)
                attribute.Apply(_varInfo);
            
            Apply();
            
            // Set the variable and store types.
            if (_varInfo.IsWorkshopReference)
            {
                // If the variable is a workshop reference, set the variable type to ElementReference.
                _varInfo.VariableType = VariableType.ElementReference;
                _varInfo.StoreType = StoreType.None;
            }
            // In extended collection.
            else if (_varInfo.InExtendedCollection)
            {
                _varInfo.StoreType = StoreType.Indexed;
            }
            // Full workshop variable.
            else
            {
                _varInfo.StoreType = StoreType.FullVariable;
            }

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

            // Get the type.
            CodeType type = TypeFromContext.GetCodeTypeFromContext(_parseInfo, OperationalScope(), _contextHandler.GetCodeType());
            ApplyCodeType(type);
        }

        protected void ApplyCodeType(CodeType type)
        {
            if (type != null && type.IsConstant())
                _varInfo.IsWorkshopReference = true;
            
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
                    varInfo.VariableType = VariableType.Global;
                    break;
                
                // playervar
                case AttributeType.Playervar:
                    varInfo.VariableType = VariableType.Player;
                    break;
                
                // ref
                case AttributeType.Ref:
                    varInfo.IsWorkshopReference = true;
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
}