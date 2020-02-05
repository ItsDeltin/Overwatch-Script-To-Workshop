using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public abstract class VarBuilder
    {
        protected readonly ParseInfo _parseInfo;
        protected readonly FileDiagnostics _diagnostics;
        protected readonly IVarContextHandler _contextHandler;
        protected readonly DocRange _nameRange;

        protected VarBuilderAttribute[] _attributes;
        protected VarInfo _varInfo;

        public Var Var { get; }

        public VarBuilder(IVarContextHandler contextHandler)
        {
            _contextHandler = contextHandler;
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
            GetCodeType();

            // Apply attributes.
            foreach (VarBuilderAttribute attribute in _attributes)
                attribute.Apply(_varInfo);
            
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

            Var = new Var(_varInfo);
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
            // Get the type.
            CodeType type = CodeType.GetCodeTypeFromContext(_parseInfo, _contextHandler.GetCodeType());
            
            if (type != null && type.Constant() != TypeSettable.Normal)
                _varInfo.IsWorkshopReference = true;
            
            _varInfo.Type = type;
        }

        protected virtual void MissingAttribute(AttributeType[] attributeTypes) {}
        protected abstract void CheckAttributes();
        protected abstract void Apply();
    }

    public interface IVarContextHandler
    {
        ParseInfo ParseInfo { get; }
        Location GetDefineLocation();
        string GetName();
        DocRange GetNameRange();
        VarBuilderAttribute[] GetAttributes();
        DeltinScriptParser.Code_typeContext GetCodeType();
    }

    class DefineContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly DeltinScriptParser.DefineContext _defineContext;

        public DefineContextHandler(ParseInfo parseInfo, DeltinScriptParser.DefineContext defineContext)
        {
            _defineContext = defineContext;
            ParseInfo = parseInfo;
        }

        // Define location.
        public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, GetNameRange());

        // Get the name.
        public string GetName() => _defineContext.name?.Text;
        public DocRange GetNameRange()
        {
            if (_defineContext.name == null) return DocRange.GetRange(_defineContext);
            return DocRange.GetRange(_defineContext.name);
        }

        // Gets the code type context.
        public DeltinScriptParser.Code_typeContext GetCodeType() => _defineContext.code_type();

        // Gets the attributes.
        public VarBuilderAttribute[] GetAttributes()
        {
            List<VarBuilderAttribute> attributes = new List<VarBuilderAttribute>();

            // Get the accessor.
            if (_defineContext.accessor() != null)
            {
                DocRange accessorRange = DocRange.GetRange(_defineContext.accessor());

                if (_defineContext.accessor().PUBLIC() != null)
                    attributes.Add(new VarBuilderAttribute(AttributeType.Public, accessorRange));
                else if (_defineContext.accessor().PRIVATE() != null)
                    attributes.Add(new VarBuilderAttribute(AttributeType.Private, accessorRange));
            }
            
            // Get the static attribute.
            if (_defineContext.STATIC() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Static, DocRange.GetRange(_defineContext.STATIC())));
            
            // Get the globalvar attribute.
            if (_defineContext.GLOBAL() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Globalvar, DocRange.GetRange(_defineContext.GLOBAL())));
            
            // Get the playervar attribute.
            if (_defineContext.PLAYER() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Playervar, DocRange.GetRange(_defineContext.PLAYER())));
            
            // Get the ref attribute.
            if (_defineContext.REF() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Ref, DocRange.GetRange(_defineContext.REF())));
            
            // Get the ID attribute.
            if (_defineContext.id != null)
                attributes.Add(new IDAttribute(_defineContext.id));
            
            // Get the extended attribute.
            if (_defineContext.NOT() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Ext, DocRange.GetRange(_defineContext.NOT())));
            
            // Get the initial value.
            if (_defineContext.expr() != null)
                attributes.Add(new InitialValueAttribute(_defineContext.expr()));
            
            return attributes.ToArray();
        }
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
                //case AttributeType.Protected: varInfo.AccessLevel = AccessLevel.Protected; break;
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

        public IDAttribute(DeltinScriptParser.NumberContext context) : base(AttributeType.ID, DocRange.GetRange(context))
        {
            ID = int.Parse(context.GetText());
        }

        public override void Apply(VarInfo varInfo)
        {
            varInfo.ID = ID;
        }
    }

    public class InitialValueAttribute : VarBuilderAttribute
    {
        public DeltinScriptParser.ExprContext ExprContext { get; }

        public InitialValueAttribute(DeltinScriptParser.ExprContext exprContext) : base(AttributeType.Initial, DocRange.GetRange(exprContext))
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