using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Variables.VanillaLink;
using Deltin.Deltinteger.Parse.Vanilla;

namespace Deltin.Deltinteger.Parse.Variables.Build
{
    public class VariableComponentCollection : IApplyAttribute
    {
        public IReadOnlyList<IVariableComponent> Components => _components.AsReadOnly();
        readonly List<IVariableComponent> _components = new List<IVariableComponent>();
        public FileDiagnostics Diagnostics { get; }
        bool _finishedObtainingComponents;

        public VariableComponentCollection(FileDiagnostics diagnostics)
        {
            Diagnostics = diagnostics;
        }

        /// <summary>Will return true if any of the variable's components are an AttributeComponent whose attribute type
        /// equals the specified type.</summary>
        /// <param name="type">The specified type.</param>
        /// <returns>True if the attribute exists, false otherwise.</returns>
        public bool IsAttribute(AttributeType type)
            => IsComponent(new AttributeComponentIdentifier(type));

        /// <summary>Determines if the specified component exists.</summary>
        /// <typeparam name="T">The type of the component. Must be an IVariableComponent.</typeparam>
        /// <returns>True if the component exists, false otherwise.</returns>
        public bool IsComponent<T>() where T : IVariableComponent
            => IsComponent(new ComponentIdentifier<T>());

        public bool TryGetComponent<T>(out T result) where T : class, IVariableComponent
        {
            T r = null;
            bool didMatch = MatchComponent(new ComponentIdentifier<T>(), component => r = (T)component, true);
            result = r;
            return didMatch;
        }

        public bool IsComponent(IComponentIdentifier identifier)
        {
            bool result = false;
            MatchComponent(identifier, _ => result = true, true);
            return result;
        }

        /// <summary>Adds a component to the list of variable components.</summary>
        public void AddComponent(IVariableComponent component)
        {
            ThrowIfComponentsAlreadyObtained();
            if (component.CheckConflicts(this))
                _components.Add(component);
        }

        /// <summary>Invalidates a component. This will add a diagnostic error and prevent the component from being applied to the variable.</summary>
        public void RejectComponent(IComponentIdentifier rejectComponent) => MatchComponent(rejectComponent, component =>
        {
            component.WasRejected = true;
            Diagnostics.Error(component.RejectMessage(), component.Range);
        }, false);

        /// <summary>Validates existing components and prevents any more components from being added to the collection.</summary>
        public void FinishedObtainingComponents()
        {
            ThrowIfComponentsAlreadyObtained();
            _finishedObtainingComponents = true;

            foreach (var component in _components)
                component.Validate(this);
        }

        /// <summary>Applies the variable to the builder.</summary>
        public void Apply(VarInfo varInfo)
        {
            foreach (var component in _components)
                if (!component.WasRejected)
                    component.Apply(varInfo);
        }

        void ThrowIfComponentsAlreadyObtained()
        {
            if (_finishedObtainingComponents)
                throw new Exception("The component collection was already completed.");
        }

        bool MatchComponent(IComponentIdentifier identifier, Action<IVariableComponent> onComponent, bool stopOnFirst)
        {
            bool anyMatch = false;
            foreach (var element in _components)
                if (identifier.IsMatch(element))
                {
                    onComponent(element);
                    anyMatch = true;
                    if (stopOnFirst) return true;
                }
            return anyMatch;
        }

        bool IApplyAttribute.Apply(FileDiagnostics diagnostics, AttributeContext attributeContext)
        {
            AddComponent(new AttributeComponent(attributeContext.Type, attributeContext.Range));
            return true;
        }
    }

    // * Components *

    /// <summary>A component of a variable.</summary>
    public interface IVariableComponent
    {
        DocRange Range { get; }
        bool WasRejected { get; set; }
        string RejectMessage();
        bool CheckConflicts(VariableComponentCollection componentCollection);
        void Validate(VariableComponentCollection componentCollection);
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

                // Persist
                case AttributeType.Persist:
                    varInfo.Persist = true;
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

                // Virtual
                case AttributeType.Virtual:
                    varInfo.Virtual = true;
                    break;

                // Override
                case AttributeType.Override:
                    varInfo.Override = true;
                    break;

                    // Missing attribute function
                    // default:
                    //     throw new NotImplementedException();
            }
        }

        public void Validate(VariableComponentCollection componentCollection)
        {
            switch (Attribute)
            {
                // Ref variables cannot be used alongside initial values.
                case AttributeType.Ref:
                    if (componentCollection.TryGetComponent(out InitialValueComponent initialValueComponent))
                    {
                        componentCollection.Diagnostics.Error("'ref' variables cannot have an initial value", initialValueComponent.Range);
                        initialValueComponent.WasRejected = true;
                    }
                    break;
            }
        }

        public bool CheckConflicts(VariableComponentCollection collection)
        {
            switch (Attribute)
            {
                // globalvar conflicting with playervar
                case AttributeType.GlobalVar:
                    return Conflict(collection, AttributeType.PlayerVar, "The 'globalvar' attribute cannot be used alongside the 'playervar' attribute");
                // playervar conflicting with globalvar
                case AttributeType.PlayerVar:
                    return Conflict(collection, AttributeType.GlobalVar, "The 'playervar' attribute cannot be used alongside the 'globalvar' attribute");
                // in conflicting with ref
                case AttributeType.In:
                    return Conflict(collection, AttributeType.Ref, "The 'in' attribute cannot be used alongside the 'ref' attribute");
                // ref conflicting with in
                case AttributeType.Ref:
                    return Conflict(collection, AttributeType.In, "The 'ref' attribute cannot be used alongside the 'in' attribute");
            }
            return true;
        }

        bool Conflict(VariableComponentCollection collection, AttributeType attributeType, string message)
        {
            if (collection.IsAttribute(attributeType))
            {
                collection.Diagnostics.Error(message, Range);
                WasRejected = true;
                return false;
            }
            return true;
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
        public void Validate(VariableComponentCollection componentCollection) { }
        public bool CheckConflicts(VariableComponentCollection componentCollection) => true;
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
        public void Validate(VariableComponentCollection componentCollection) { }
        public bool CheckConflicts(VariableComponentCollection componentCollection) => true;
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
        public void Validate(VariableComponentCollection componentCollection) { }
        public bool CheckConflicts(VariableComponentCollection componentCollection) => true;
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
            varInfo.IsMacro = true;
        }
        public string RejectMessage() => "Macros cannot be declared here.";
        public void Validate(VariableComponentCollection componentCollection) { }
        public bool CheckConflicts(VariableComponentCollection componentCollection) => true;
    }

    class TargetWorkshopComponent : IVariableComponent
    {
        public DocRange Range => _syntax.Range;
        public bool WasRejected { get; set; }
        readonly TargetWorkshopVariable _syntax;
        readonly VariableSetKind _variableSetKind;
        bool _isGlobal;

        public TargetWorkshopComponent(TargetWorkshopVariable syntax, VariableSetKind variableSetKind)
        {
            _syntax = syntax;
            _variableSetKind = variableSetKind;
        }

        public string RejectMessage() => "Only rule-level variables can target workshop variables";
        public bool CheckConflicts(VariableComponentCollection componentCollection) => true;
        public void Validate(VariableComponentCollection componentCollection)
        {
            _isGlobal = _variableSetKind switch
            {
                VariableSetKind.Global => true,
                VariableSetKind.Player => false,
                VariableSetKind.Unknown or _ => componentCollection.IsAttribute(AttributeType.GlobalVar)
            };
        }
        public void Apply(VarInfo varInfo)
        {
            var parseInfo = varInfo.ParseInfo;

            var linkTargets = new List<VariableLinkExpressionTarget>();
            bool anySpread = false;
            foreach (var target in _syntax.Targets)
            {
                string targetName = Extras.RemoveQuotes(target.Target.Text);

                // Is the vanilla variable in scope?
                if (parseInfo.ScopedVanillaVariables.GetScopedVariable(targetName, _isGlobal).Variable is null)
                {
                    parseInfo.Error($"No {VanillaHelper.GlobalOrPlayerString(_isGlobal)} workshop variable named '{targetName}' is in the current scope", target.Target);
                }

                bool didSpread = false;
                var analyzedIndexers = new List<IExpression>();
                foreach (var indexer in target.Indexer)
                {
                    // !'a'[..][0] is not allowed.
                    if (didSpread)
                    {
                        parseInfo.Error("Cannot index further after spread expression", indexer.Spread ?? indexer.Expression.Range);
                    }
                    else if (indexer.Spread)
                    {
                        // !{'a', 'b'[..]} is not allowed.
                        if (_syntax.Targets.Count > 1)
                            parseInfo.Error("Cannot use spread syntax with multiple variable targets", indexer.Spread);
                        didSpread = true;
                        anySpread = true;
                    }
                    else
                    {
                        // Get the expression.
                        var nextExpr = parseInfo.GetExpression(varInfo.Scope, indexer.Expression);
                        // Any more indexers after didSpread are invalid.
                        if (!didSpread)
                            analyzedIndexers.Add(nextExpr);
                    }
                }
                linkTargets.Add(new(
                    LinkingTo: targetName,
                    Indexers: analyzedIndexers.ToArray()
                ));
            }

            // Not very nice but required because of some fun struct stuff.
            varInfo.ParseInfo.TranslateInfo.StagedInitiation.On(InitiationStage.PostContent, () =>
            {
                int requiredStackLength = varInfo.Type.Attributes.StackLength;
                if (!anySpread && _syntax.Targets.Count != requiredStackLength)
                {
                    parseInfo.Error($"Type '{varInfo.Type.GetName()}' requires {requiredStackLength} target variables, got {_syntax.Targets.Count}", _syntax.Range);
                }
            });


            var links = new VariableLinkExpressionCollection(anySpread, linkTargets.ToArray());
            varInfo.LinkTargetVanilla = links;
        }
    }

    // * Rejectors *

    /// <summary>Rejects an attribute.</summary>
    /// <typeparam name="T">The type of attribute this handles.</typeparam>
    public interface IComponentIdentifier
    {
        bool IsMatch(IVariableComponent component);
    }

    /// <summary>Identifies attributes (accessors, virtual, etc.)</summary>
    class AttributeComponentIdentifier : IComponentIdentifier
    {
        private readonly AttributeType[] _identifying;

        public AttributeComponentIdentifier(params AttributeType[] identifying)
        {
            _identifying = identifying;
        }

        public bool IsMatch(IVariableComponent component) => component is AttributeComponent attributeComponent && _identifying.Contains(attributeComponent.Attribute);
    }

    /// <summary>Identifies type components.</summary>
    class ComponentIdentifier<T> : IComponentIdentifier where T : IVariableComponent
    {
        public bool IsMatch(IVariableComponent component) => component is T;
    }
}