using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Lambda;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse
{
    public abstract class CodeType : IExpression, ICallable, IWorkshopInit, ICodeTypeSolver
    {
        public string Name { get; }
        public CodeType[] Generics { get; protected set; } = new CodeType[0];
        public Constructor[] Constructors { get; protected set; } = new Constructor[0];
        public CodeType Extends { get; private set; }
        public string Description { get; protected set; }
        public IInvokeInfo InvokeInfo { get; protected set; }
        public Debugger.IDebugVariableResolver DebugVariableResolver { get; protected set; } = new Debugger.DefaultResolver();
        protected TypeKind Kind = TypeKind.Struct;
        protected SemanticTokenType TokenType { get; set; } = SemanticTokenType.Type;
        protected List<TokenModifier> TokenModifiers { get; set; } = new List<TokenModifier>();

        /// <summary>Determines if the class can be deleted with the delete keyword.</summary>
        public bool CanBeDeleted { get; protected set; } = false;

        /// <summary>Determines if other classes can inherit this class.</summary>
        public bool CanBeExtended { get; protected set; } = false;

        /// <summary>Determines if the type is a struct or struct array.</summary>
        public bool IsStruct { get; protected set; }

        /// <summary>Tracks type arg usage.</summary>
        public IGenericUsage GenericUsage { get; protected set; }

        /// <summary>Overrides execution of array functions when this type is used in an array.</summary>
        public virtual ITypeArrayHandler ArrayHandler { get; protected set; } = new DefaultArrayHandler();

        /// <summary>Type operations for assignment, comparison, etc.</summary>
        public TypeOperatorInfo Operations { get; protected set; }

        protected List<IMethod> VirtualFunctions { get; } = new List<IMethod>();
        protected List<IVariableInstance> VirtualVariables { get; } = new List<IVariableInstance>();

        public CodeType(string name)
        {
            Name = name;
            Operations = new TypeOperatorInfo(this);
            GenericUsage = new AddToGenericsUsage(this);
        }

        public virtual bool Implements(CodeType type)
        {
            if (type is PipeType union)
                foreach (var unionType in union.IncludedTypes)
                    if (DoesImplement(unionType))
                        return true;
            return DoesImplement(type);
        }

        protected virtual bool DoesImplement(CodeType type)
        {
            // Iterate through all extended classes.
            CodeType checkType = this;
            while (checkType != null)
            {
                if (type.Is(checkType) || (!checkType.IsConstant() && type is AnyType)) return true;
                checkType = checkType.Extends;
            }

            return false;
        }

        public virtual CodeType[] UnionTypes() => new[] {this};

        public virtual bool Is(CodeType type) => this == type;

        // Static
        public virtual Scope ReturningScope() => null;
        // Object
        public virtual Scope GetObjectScope() => null;

        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet) => null;

        /// <summary>Determines if variables with this type can have their value changed.</summary>
        public virtual bool IsConstant() => false;

        /// <summary>The returning value when `new TypeName` is called.</summary>
        /// <param name="actionSet">The actionset to use.</param>
        /// <param name="constructor">The constuctor that was called.</param>
        /// <param name="constructorValues">The parameter values of the constructor.</param>
        /// <param name="additionalParameterData">Additional parameter data.</param>
        public virtual IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Classes that can't be created shouldn't have constructors.
            throw new NotImplementedException();
        }

        /// <summary>Sets up an object reference when a new object is created. Is also called when a new object of a class extending this type is created.</summary>
        /// <param name="actionSet">The actionset to use.</param>
        /// <param name="reference">The reference of the object.</param>
        public virtual void BaseSetup(ActionSet actionSet, Element reference) => throw new NotImplementedException();

        public virtual void ResolveElements() {}

        /// <summary>Assigns workshop elements so the class can function. Implementers should check if `wasCalled` is true.</summary>
        public virtual void WorkshopInit(DeltinScript translateInfo) { }

        /// <summary>Adds the class objects to the index assigner.</summary>
        /// <param name="source">The source of the type.</param>
        /// <param name="assigner">The assigner that the object variables will be added to.</param>
        public virtual void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner) { }

        /// <summary>Deletes a variable from memory.</summary>
        /// <param name="actionSet">The action set to add the actions to.</param>
        /// <param name="reference">The object reference.</param>
        public virtual void Delete(ActionSet actionSet, Element reference) {}

        public virtual IGettableAssigner GetGettableAssigner(IVariable variable) => new DataTypeAssigner((Var)variable);

        /// <summary>Calls a type from the specified document range.</summary>
        /// <param name="parseInfo">The script that the type was called from.</param>
        /// <param name="callRange">The range of the call.</param>
        public virtual void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.TranslateInfo.AddWorkshopInit(this);
            parseInfo.Script.AddToken(callRange, TokenType, TokenModifiers.ToArray());
            
            var hover = new MarkupBuilder().StartCodeLine().Add(Kind.ToString().ToLower() + " " + Name).EndCodeLine();
            if (Description != null) hover.NewSection().Add(Description);
            parseInfo.Script.AddHover(callRange, hover);
        }

        public virtual AnonymousType[] ExtractAnonymousTypes()
        {
            var types = new HashSet<AnonymousType>();

            foreach (var typeArg in Generics)
                foreach (var typeArgExtracted in typeArg.ExtractAnonymousTypes())
                    types.Add(typeArgExtracted);

            return types.ToArray();
        }

        /// <summary>Gets the completion that will show up for the language server.</summary>
        public abstract CompletionItem GetCompletion();

        public static CompletionItem GetTypeCompletion(CodeType type) => new CompletionItem() {
            Label = type.GetName(),
            Kind = type.Kind == TypeKind.Class ? CompletionItemKind.Class : type.Kind == TypeKind.Constant ? CompletionItemKind.Constant : type.Kind == TypeKind.Enum ? CompletionItemKind.Enum : CompletionItemKind.Struct
        };

        /// <summary>Gets the full name of the type.</summary>
        public virtual string GetName()
        {
            string result = Name;

            if (Generics.Length > 0)
                result += "<" + string.Join(", ", Generics.Select(g => g.GetName())) + ">";

            return result;
        }

        public virtual void AddLink(Location location) {}

        public virtual CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo) => this;

        public override string ToString() => GetName();

        public IMethod GetVirtualFunction(DeltinScript deltinScript, string name, CodeType[] parameterTypes)
        {
            // Loop through each virtual function.
            foreach (var virtualFunction in VirtualFunctions)
                // If the function's name matches and the parameter lengths are the same.
                if (virtualFunction.Name == name && parameterTypes.Length == virtualFunction.Parameters.Length)
                {
                    bool matches = true;
                    // Loop though the parameters.
                    for (int i = 0; i < parameterTypes.Length; i++)
                        // Make sure the parameter types match.
                        if (!parameterTypes[i].Is(virtualFunction.Parameters[i].GetCodeType(deltinScript)))
                        {
                            matches = false;
                            break;
                        }
                    
                    if (matches)
                        return virtualFunction;
                }
            
            if (Extends != null) return Extends.GetVirtualFunction(deltinScript, name, parameterTypes);
            return null;
        }

        public IVariableInstance GetVirtualVariable(string name)
        {
            // Loop through each virtual variable.
            foreach (var virtualVariable in VirtualVariables)
                if (virtualVariable.Name == name)
                    return virtualVariable;
            
            if (Extends != null) return Extends.GetVirtualVariable(name);
            return null;
        }

        CodeType ICodeTypeSolver.GetCodeType(DeltinScript deltinScript) => this;
    }
}
