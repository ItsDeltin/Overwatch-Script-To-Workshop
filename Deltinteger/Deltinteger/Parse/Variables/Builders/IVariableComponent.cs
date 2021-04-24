using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse.Variables.Build
{
    public class VariableComponentsCollection : List<IVariableComponent>
    {
        /// <summary>Will return true if any of the variable's components are an AttributeComponent whose attribute type
        /// equals the specified type.</summary>
        /// <param name="type">The specified type.</param>
        /// <returns>True if the attribute exists, false otherwise.</returns>
        public bool IsAttribute(AttributeType type)
            => this.Any(component => component is AttributeComponent attributeComponent && attributeComponent.Attribute == type);
        
        /// <summary>Determines if the specified component exists.</summary>
        /// <typeparam name="T">The type of the component. Must be an IVariableComponent.</typeparam>
        /// <returns>True if the component exists, false otherwise.</returns>
        public bool IsComponent<T>() where T: IVariableComponent
            => this.Any(component => component is T);
        
        public VariableComponentsCollection(IVariableComponent[] components) : base(components) {}
    }

    // * Components *

    /// <summary>A component of a variable.</summary>
    public interface IVariableComponent
    {
        DocRange Range { get; }
        bool WasRejected { get; set; }
        string RejectMessage();
        void Apply(VarInfo varInfo);
    }

    /// <summary>Variable attribute component.</summary>
    class AttributeComponent : IVariableComponent
    {
        public AttributeType Attribute { get; }
        public DocRange Range { get; }
        public bool WasRejected { get; set; }

        public AttributeComponent(AttributeType attribute, DocRange range)
        {
            Attribute = attribute;
            Range = range;
        }

        public string RejectMessage()
        {
            switch (Attribute)
            {
                // Accessors
                case AttributeType.Public:
                case AttributeType.Protected:
                case AttributeType.Private:
                    return "Accessor not valid here.";
                
                // Use attribute name
                default:
                    return $"'{Attribute.ToString().ToLower()}' attribute not valid here.";
            }
        }

        public void Apply(VarInfo varInfo)
        {
            switch (Attribute)
            {
                // Access levels
                case AttributeType.Public: varInfo.AccessLevel = AccessLevel.Public; break;
                case AttributeType.Protected: varInfo.AccessLevel = AccessLevel.Protected; break;
                case AttributeType.Private: varInfo.AccessLevel = AccessLevel.Private; break;

                // globalvar
                case AttributeType.GlobalVar:
                    varInfo.VariableTypeHandler.SetAttribute(true);
                    break;
                
                // playervar
                case AttributeType.PlayerVar:
                    varInfo.VariableTypeHandler.SetAttribute(false);
                    break;
                
                // ref
                case AttributeType.Ref:
                    varInfo.Ref = true;
                    break;

                // in
                case AttributeType.In:
                    varInfo.VariableTypeHandler.SetWorkshopReference();
                    break;
                
                // Static
                case AttributeType.Static:
                    varInfo.Static = true;
                    break;
                
                // Missing attribute function
                default:
                    throw new NotImplementedException();
            }
        }
    }

    /// <summary>Initial variable value component.</summary>
    class InitialValueComponent : IVariableComponent
    {
        public IParseExpression Expression { get; }
        public DocRange Range => Expression.Range;
        public bool WasRejected { get; set; }

        public InitialValueComponent(IParseExpression expression)
        {
            Expression = expression;
        }

        public string RejectMessage() => "Variable cannot have an initial value.";
        public void Apply(VarInfo varInfo) => varInfo.InitialValueContext = Expression;
    }

    /// <summary>Variable workshop ID override component.</summary>
    class WorkshopIndexComponent : IVariableComponent
    {
        public int Value { get; }
        public DocRange Range { get; }
        public bool WasRejected { get; set; }

        public WorkshopIndexComponent(int value, DocRange range)
        {
            Value = value;
            Range = range;
        }

        public string RejectMessage() => "Cannot override workshop variable ID here.";
        public void Apply(VarInfo varInfo) => varInfo.ID = Value;
    }

    /// <summary>Extended collection component.</summary>
    class ExtendedCollectionComponent : IVariableComponent
    {
        public DocRange Range { get; }
        public bool WasRejected { get; set; }

        public ExtendedCollectionComponent(DocRange range)
        {
            Range = range;
        }

        public string RejectMessage() => "Cannot put variable in the extended collection.";
        public void Apply(VarInfo varInfo) => varInfo.InExtendedCollection = true;
    }

    /// <summary>Macro variable component.</summary>
    class MacroComponent : IVariableComponent
    {
        public DocRange Range { get; }
        public bool WasRejected { get; set; }

        public MacroComponent(DocRange symbolRange)
        {
            Range = symbolRange;
        }

        public void Apply(VarInfo varInfo)
        {
            varInfo.VariableTypeHandler.SetWorkshopReference();
        }

        public string RejectMessage() => "Macros cannot be declared here.";
    }

    // * Rejectors *

    /// <summary>Rejects an attribute.</summary>
    /// <typeparam name="T">The type of attribute this handles.</typeparam>
    public interface IRejectComponent
    {
        void Reject(FileDiagnostics diagnostics, IVariableComponent component);
    }

    /// <summary>Reject element attributes (accessors, virtual, etc.)</summary>
    class RejectAttributeComponent : IRejectComponent
    {
        private readonly AttributeType[] _rejecting;

        public RejectAttributeComponent(params AttributeType[] rejecting)
        {
            _rejecting = rejecting;
        }

        public void Reject(FileDiagnostics diagnostics, IVariableComponent component)
        {
            if (!component.WasRejected && component is AttributeComponent attributeComponent && _rejecting.Contains(attributeComponent.Attribute))
            {
                component.WasRejected = true;
                diagnostics.Error(component.RejectMessage(), component.Range);
            }
        }
    }

    /// <summary>Reject attributes without special parameters.</summary>
    class RejectComponent<T> : IRejectComponent where T : IVariableComponent
    {
        public void Reject(FileDiagnostics diagnostics, IVariableComponent component)
        {
            if (!component.WasRejected && component is T)
            {
                component.WasRejected = true;
                diagnostics.Error(component.RejectMessage(), component.Range);
            }
        }
    }

    // * Attributes extract *
    class ExtractAttributeComponents : IApplyAttribute
    {
        public List<AttributeComponent> Attributes { get; } = new List<AttributeComponent>();
        public bool Apply(FileDiagnostics diagnostics, AttributeContext attribute)
        {
            Attributes.Add(new AttributeComponent(attribute.Type, attribute.Range));
            return true;
        }

        public static AttributeComponent[] Get(FileDiagnostics diagnostics, AttributeTokens context)
        {
            var extract = new ExtractAttributeComponents();
            new AttributesGetter(context, extract).GetAttributes(diagnostics);
            return extract.Attributes.ToArray();
        }
    }
}