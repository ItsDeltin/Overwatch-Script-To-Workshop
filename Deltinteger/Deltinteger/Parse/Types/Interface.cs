using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    class InterfaceLinkComponent : IComponent, IWorkshopInitComponent
    {
        public DeltinScript DeltinScript { get; set; }
        public List<InterfaceLink> Links { get; } = new List<InterfaceLink>();

        public void Init() {}

        public void Add(string variableName)
        {
            if (!Links.Any(link => link.VariableName == variableName)) Links.Add(new InterfaceLink(variableName));
        }

        public void WorkshopInit()
        {
            foreach (var link in Links)
                link.SetArrayStore(DeltinScript.VarCollection.Assign(link.VariableName, true, false));
        }

        /// <summary>Gets an interface variable link. Will throw KeyNotFoundException if the link is not found.</summary>
        public IndexReference Get(string variableName)
        {
            foreach (var link in Links)
                if (link.VariableName == variableName)
                    return link.ArrayStore;
            
            throw new KeyNotFoundException("'" + variableName + "' is not a linked interface variable.");
        }

        public bool TryGet(string variableName, out IndexReference arrayStore)
        {
            foreach (var link in Links)
                if (variableName == link.VariableName)
                {
                    arrayStore = link.ArrayStore;
                    return true;
                }
            arrayStore = null;
            return false;
        }
    }

    class InterfaceLink
    {
        public string VariableName { get; }
        public IndexReference ArrayStore { get; private set; }

        public InterfaceLink(string variableName)
        {
            VariableName = variableName;
        }

        public void SetArrayStore(IndexReference indexReference)
        {
            ArrayStore = indexReference;
        }
    }

    public interface IImplementer : INamed
    {
        List<Interface> Contracts { get; }
        CodeType Extends { get; set; }
    }

    public abstract class Interface : CodeType, IImplementer
    {
        public List<Interface> Contracts { get; } = new List<Interface>();
        protected Scope InterfaceScope { get; } = new Scope();
        protected List<InterfaceVariable> Variables { get; } = new List<InterfaceVariable>();
        protected List<InterfaceFunction> Functions { get; } = new List<InterfaceFunction>();
        protected ParseInfo ParseInfo { get; }

        public Interface(ParseInfo parseInfo, string name) : base(name)
        {
            ParseInfo = parseInfo;
            CanBeExtended = true;
            CanBeDeleted = true;
        }

        public void SetupImplementer(ClassType implementer)
        {
            LinkVariables(implementer);
            LinkFunctions(implementer);
            
            // ???
            // if (implementer.Extends != null) SetupImplementer((ClassType)implementer.Extends);
        }

        private void LinkVariables(ClassType implementer)
        {
            // Check classType's inheritance tree for the variable.
            foreach (InterfaceVariable variable in Variables)
            {
                // Get the matching ObjectVariable.
                ObjectVariable match = VariableFromName(implementer, variable.Name);

                // Get the workshop variable that the interface variable is stored in.
                IndexReference linkIndex = ParseInfo.TranslateInfo.GetComponent<InterfaceLinkComponent>().Get(variable.Name);

                if (match == null)
                {
                    // 'match' will by null if classType does not implement the variable.
                    // Maybe throw a syntax error if the implementing class needs to explicitly define the variable.
                }
                else
                {
                    // Link the variable.
                    match.SetArrayStore(linkIndex);
                }
            }
        }

        private void LinkFunctions(ClassType implementer)
        {
            foreach (InterfaceFunction interfaceFunction in Functions)
                IterateParents(implementer, classType => {
                    // Iterate through each function.
                    foreach (IMethod classFunction in classType.ObjectFunctions)
                        if (Scope.OverloadMatches(interfaceFunction, classFunction))
                        {
                            interfaceFunction.AddImplementation(classFunction);
                            return true;
                        }
                    // Function was not found in the current class.
                    return false;
                });
        }

        /// <summary>Gets an ObjectVariable from a ClassType by name. If the class has no variables that matches the name, the type that the class extends is checked.</summary>
        private static ObjectVariable VariableFromName(ClassType classType, string name)
        {
            ObjectVariable result = null;
            IterateParents(classType, type => {
                foreach (ObjectVariable variable in type.ObjectVariables)
                    if (variable.Variable.Name == name)
                    {
                        result = variable;
                        return true;
                    }
                return false;
            });
            return result;
        }

        private static void IterateParents(ClassType classType, Func<ClassType, bool> action)
        {
            ClassType current = classType;
            while (current != null && !action.Invoke(current)) current = (ClassType)current.Extends;
        }

        protected void AddVariable(InterfaceVariable variable)
        {
            Variables.Add(variable);
            InterfaceScope.AddVariable(variable, ParseInfo.Script.Diagnostics, variable.DefinedAt.range);
            ParseInfo.TranslateInfo.GetComponent<InterfaceLinkComponent>().Add(variable.Name);
        }

        public void AddFunction(InterfaceFunction function)
        {
            Functions.Add(function);
            InterfaceScope.AddMethod(function, ParseInfo.Script.Diagnostics, function.DefinedAt.range);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            foreach (InterfaceVariable variable in Variables)
                assigner.Add(variable, ParseInfo.TranslateInfo.GetComponent<InterfaceLinkComponent>().Get(variable.Name).CreateChild((Element)reference));
        }

        public override bool DoesImplement(CodeType type)
        {
            if (this == type) return true;
            foreach (Interface contract in Contracts)
                if (contract.DoesImplement(type))
                    return true;
            return false;
        }

        public override Scope GetObjectScope() => InterfaceScope;
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Detail = "interface " + Name,
            Kind = CompletionItemKind.Interface
        };
    }

    public class DefinedInterface : Interface
    {
        public LanguageServer.Location DefinedAt { get; }
        private readonly DeltinScriptParser.InterfaceContext _context;
        private bool _elementsResolved = false;

        public DefinedInterface(ParseInfo parseInfo, DeltinScriptParser.InterfaceContext context) : base(parseInfo, context.name.Text)
        {
            _context = context;

            if (parseInfo.TranslateInfo.Types.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", DocRange.GetRange(context.name));
            
            DefinedAt = new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
        }

        public override void ResolveElements()
        {
            if (_elementsResolved) return;
            _elementsResolved = true;

            // Get implementing interfaces.
            InheritContext.InheritFromContext(this, ParseInfo, _context.inherit());

            ResolveVariables();
            ResolveFunctions();
        }

        private void ResolveVariables()
        {
            // Get interface variables.
            foreach (var variable in _context.interface_variable())
            {
                // Get the variable's type.
                CodeType variableType = CodeType.GetCodeTypeFromContext(ParseInfo, variable.code_type());

                // Interface variables cannot be constants.
                if (variableType != null && variableType.IsConstant())
                {
                    ParseInfo.Script.Diagnostics.Error("Interface variables cannot have a constant type.", DocRange.GetRange(variable.code_type()));
                    continue;
                }

                InterfaceVariable newVariable = new InterfaceVariable(variableType, variable.name.Text, new LanguageServer.Location(ParseInfo.Script.Uri, DocRange.GetRange(variable.name)));
                //AddVariable(newVariable, DocRange.GetRange(variable.name));
                AddVariable(newVariable);
            }
        }

        private void ResolveFunctions()
        {
            // Get the interface functions.
            foreach (var function in _context.interface_function())
            {
                InterfaceFunction newFunction = new InterfaceFunction(ParseInfo, InterfaceScope, function);
                AddFunction(newFunction);
            }
        }

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            ParseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new LanguageServer.Location(parseInfo.Script.Uri, callRange));
        }
    }

    public class InterfaceVariable : IIndexReferencer
    {
        public CodeType CodeType { get; }
        public string Name { get; }
        public LanguageServer.Location DefinedAt { get; }
        public VariableType VariableType => VariableType.Global;
        public bool Static => false;
        public bool WholeContext => true;
        public AccessLevel AccessLevel => AccessLevel.Public;

        public InterfaceVariable(CodeType type, string name, LanguageServer.Location definedAt)
        {
            CodeType = type;
            Name = name;
            DefinedAt = definedAt;
        }

        public bool Settable() => true;
        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Variable
        };
        public Scope ReturningScope() => Type()?.GetObjectScope();
        public CodeType Type() => CodeType;
        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            //todo
        }

        public string GetLabel(bool markdown) => "todo";
    }

    public class InterfaceFunction : IMethod
    {
        public string Name { get; }
        public CodeParameter[] Parameters { get; }
        public CodeType ReturnType { get; }
        public MethodAttributes Attributes { get; }
        public LanguageServer.Location DefinedAt { get; }
        public bool Static => false;
        public bool WholeContext => true;
        public string Documentation => ""; // TODO
        public AccessLevel AccessLevel => AccessLevel.Public;
        private bool ReturnsValue { get; }
        private List<IMethod> Implementations { get; } = new List<IMethod>();
        private Scope Scope { get; }
        private SubroutineInfo Subroutine { get; }

        public InterfaceFunction(ParseInfo parseInfo, Scope interfaceScope, DeltinScriptParser.Interface_functionContext context)
        {
            Name = context.name.Text;
            Scope = interfaceScope.Child();

            var parameterInfo = CodeParameter.GetParameters(parseInfo, Scope, context.setParameters(), false);
            Parameters = parameterInfo.Parameters;

            if (context.VOID() == null)
                ReturnsValue = false;
            else
            {
                ReturnsValue = true;
                ReturnType = CodeType.GetCodeTypeFromContext(parseInfo, context.code_type());
            }

            DefinedAt = new Location(parseInfo.Script.Uri, DocRange.GetRange(context.name));
        }

        public bool DoesReturnValue() => ReturnsValue;
        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => "todo";

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            throw new NotImplementedException();
        }

        public void AddImplementation(IMethod implementation)
        {
            Implementations.Add(implementation);
        }
    }
}