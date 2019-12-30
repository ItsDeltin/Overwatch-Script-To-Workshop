using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse
{
    public abstract class CodeType : IExpression, ICallable
    {
        public string Name { get; }
        public Constructor[] Constructors { get; protected set; }
        public string Description { get; protected set; }
        protected abstract string TypeKindString { get; }

        public CodeType(string name)
        {
            Name = name;
        }

        // Static
        public abstract Scope ReturningScope();
        // Object
        public virtual Scope GetObjectScope()
        {
            return null;
        }

        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => null;

        /// <summary>
        /// Determines if variables with this type can have their value changed.
        /// </summary>
        /// <returns></returns>
        public virtual TypeSettable Constant() => TypeSettable.Normal;

        public virtual IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Classes that can't be created shouldn't have constructors.
            throw new NotImplementedException();
        }

        public virtual IndexReference GetObjectSource(DeltinScript translateInfo, IWorkshopTree element) => null;

        public virtual void Call(ScriptFile script, DocRange callRange)
        {
            if (Description != null)
                script.AddHover(callRange, Description);
        }

        public abstract CompletionItem GetCompletion();

        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, DeltinScriptParser.Code_typeContext typeContext)
        {
            if (typeContext == null) return null;
            CodeType type = parseInfo.TranslateInfo.GetCodeType(typeContext.PART().GetText(), parseInfo.Script.Diagnostics, DocRange.GetRange(typeContext));

            if (type != null)
            {
                type.Call(parseInfo.Script, DocRange.GetRange(typeContext));

                if (typeContext.INDEX_START() != null)
                    for (int i = 0; i < typeContext.INDEX_START().Length; i++)
                        type = new ArrayType(type);
            }
            return type;
        }

        public static bool TypeMatches(CodeType parameterType, CodeType valueType)
        {
            return parameterType == null || parameterType.Name == valueType?.Name;
        }

        static List<CodeType> _defaultTypes;
        public static List<CodeType> DefaultTypes {
            get {
                if (_defaultTypes == null) GetDefaultTypes();
                return _defaultTypes;
            }
        }
        private static void GetDefaultTypes()
        {
            _defaultTypes = new List<CodeType>();
            foreach (var enumData in EnumData.GetEnumData())
                _defaultTypes.Add(new WorkshopEnumType(enumData));
            
            // Add custom classes here.
            _defaultTypes.Add(new Pathfinder.PathmapClass());
            _defaultTypes.Add(new Models.AssetClass());
        }
    }

    public enum TypeSettable
    {
        Normal, Convertable, Constant
    }

    public class WorkshopEnumType : CodeType
    {
        private Scope EnumScope { get; } = new Scope();
        public EnumData EnumData { get; }
        override protected string TypeKindString { get; } = "enum";

        public WorkshopEnumType(EnumData enumData) : base(enumData.CodeName)
        {
            EnumData = enumData;
            foreach (var member in enumData.Members)
            {
                var scopedMember = new ScopedEnumMember(this, member);
                EnumScope.AddVariable(scopedMember, null, null);
            }
            EnumScope.ErrorName = "enum " + Name;
        }

        override public Scope ReturningScope()
        {
            return EnumScope;
        }

        override public TypeSettable Constant() => EnumData.ConvertableToElement() ? TypeSettable.Convertable : TypeSettable.Constant;

        override public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = EnumData.CodeName,
                Kind = CompletionItemKind.Enum
            };
        }
    
        public static WorkshopEnumType GetEnumType(EnumData enumData)
        {
            return (WorkshopEnumType)CodeType.DefaultTypes.First(t => t is WorkshopEnumType && ((WorkshopEnumType)t).EnumData == enumData);
        }
        public static WorkshopEnumType GetEnumType<T>()
        {
            var enumData = EnumData.GetEnum<T>();
            return (WorkshopEnumType)CodeType.DefaultTypes.First(t => t is WorkshopEnumType && ((WorkshopEnumType)t).EnumData == enumData);
        }
    }

    public class ScopedEnumMember : IScopeable, IExpression
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public LanguageServer.Location DefinedAt { get; } = null;
        public bool WholeContext { get; } = true;
        
        public CodeType Enum { get; }
        public EnumMember EnumMember { get; }

        private Scope debugScope { get; } = new Scope();
        
        public ScopedEnumMember(CodeType parent, EnumMember enumMember)
        {
            Enum = parent;
            Name = enumMember.CodeName;
            EnumMember = enumMember;
            debugScope.ErrorName = "enum value " + Name;
        }

        public Scope ReturningScope()
        {
            return debugScope;
        }

        public CodeType Type() => Enum;

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            if (asElement) return EnumData.ToElement(EnumMember);
            return (IWorkshopTree)EnumMember;
        }

        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.EnumMember
            };
        }
    }

    public class ArrayType : CodeType
    {
        public CodeType ArrayOfType { get; }

        public ArrayType(CodeType arrayOfType) : base(arrayOfType.Name + "[]")
        {
            ArrayOfType = arrayOfType;
        }

        protected override string TypeKindString => "array";
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => throw new NotImplementedException();
    }

    public class DefinedType : CodeType
    {
        public TypeKind TypeKind { get; }
        protected override string TypeKindString { get; }
        public LanguageServer.Location DefinedAt { get; }
        private Scope objectScope { get; }
        private Scope staticScope { get; }
        private List<Var> objectVariables { get; } = new List<Var>();
        private DeltinScript translateInfo { get; }

        public DefinedType(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Type_defineContext typeContext, List<IApplyBlock> applyMethods) : base(typeContext.name.Text)
        {
            this.translateInfo = parseInfo.TranslateInfo;
            if (translateInfo.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", DocRange.GetRange(typeContext.name));
            
            DefinedAt = new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(typeContext.name));
            translateInfo.AddSymbolLink(this, DefinedAt);

            if (typeContext.CLASS() != null) 
            { 
                TypeKind = TypeKind.Class;
                TypeKindString = "class";
            }
            else if (typeContext.STRUCT() != null) 
            { 
                TypeKind = TypeKind.Struct;
                TypeKindString = "struct";
            }
            else throw new NotImplementedException();

            staticScope = translateInfo.GlobalScope.Child(TypeKindString + " " + Name);
            objectScope = staticScope.Child(TypeKindString + " " + Name);

            // Get the variables defined in the type.
            foreach (var definedVariable in typeContext.define())
            {
                Var newVar = Var.CreateVarFromContext(VariableDefineType.InClass, parseInfo, definedVariable);
                newVar.Finalize(UseScope(newVar.Static));
                if (!newVar.Static) objectVariables.Add(newVar);
            }

            // Todo: Static methods/macros.
            foreach (var definedMethod in typeContext.define_method())
            {
                var newMethod = new DefinedMethod(parseInfo, UseScope(false), definedMethod);
                applyMethods.Add(newMethod);
            }

            foreach (var macroContext in typeContext.define_macro())
            {
                DeltinScript.GetMacro(parseInfo, UseScope(false), macroContext, applyMethods);
            }

            // Get the constructors.
            if (typeContext.constructor().Length > 0)
            {
                Constructors = new Constructor[typeContext.constructor().Length];
                for (int i = 0; i < Constructors.Length; i++)
                {
                    Constructors[i] = new DefinedConstructor(parseInfo, this, typeContext.constructor(i));
                    applyMethods.Add((DefinedConstructor)Constructors[i]);
                }
            }
            else
            {
                // If there are no constructors, create a default constructor.
                Constructors = new Constructor[] {
                    new Constructor(this, new Location(parseInfo.Script.Uri, DocRange.GetRange(typeContext.name)), AccessLevel.Public)
                };
            }
        }

        private Scope UseScope(bool isStatic)
        {
            return isStatic ? staticScope : objectScope;
        }

        override public Scope ReturningScope()
        {
            return staticScope;
        }

        override public Scope GetObjectScope()
        {
            return objectScope;
        }

        override public IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            if (TypeKind == TypeKind.Class) return NewClass(actionSet.New(actionSet.IndexAssigner.CreateContained()), constructor, constructorValues);
            else if (TypeKind == TypeKind.Struct) return NewStruct(actionSet.New(actionSet.IndexAssigner.CreateContained()), constructor, constructorValues);
            else throw new NotImplementedException();
        }

        public const bool CLASS_INDEX_WORKAROUND = true;

        private IWorkshopTree NewClass(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues)
        {
            var classData = actionSet.Translate.DeltinScript.SetupClasses();
            
            // Classes are stored in the class array (`classData.ClassArray`),
            // this stores the index where the new class is created at.
            var classReference = actionSet.VarCollection.Assign("_new_" + Name + "_class_index", actionSet.IsGlobal, true);
            GetClassIndex(classReference, actionSet, classData);
            
            var classObject = classData.ClassArray.CreateChild((Element)classReference.GetVariable());
            SetInitialVariables(classObject, actionSet);

            // Run the constructor.
            AddObjectVariablesToAssigner(classObject, actionSet.IndexAssigner);
            constructor.Parse(actionSet, constructorValues);

            return classReference.GetVariable();
        }

        public static void GetClassIndex(IndexReference classReference, ActionSet actionSet, ClassData classData)
        {
            // GetClassIndex() is less server load intensive than GetClassIndexWorkaround,
            // but due to a workshop bug with `Index Of Array Value`, the workaround may
            // need to be used instead.

            if (!CLASS_INDEX_WORKAROUND)
            {
                // Get the index of the first null value in the class array.
                actionSet.AddAction(classReference.SetVariable(
                    Element.Part<V_IndexOfArrayValue>(
                        classData.ClassArray.GetVariable(),
                        new V_Null()
                    )
                ));
                
                // If the index equals -1, use the count of the class array instead.
                // TODO: Try setting the 1000th index of the class array to null instead.
                actionSet.AddAction(classReference.SetVariable(
                    Element.TernaryConditional(
                        new V_Compare(classReference.GetVariable(), Operators.Equal, new V_Number(-1)),
                        Element.Part<V_CountOf>(classData.ClassArray.GetVariable()),
                        classReference.GetVariable()
                    )
                ));
            }
            else
            {
                // Get an empty index in the class array to store the new class.
                Element firstFree = (
                    Element.Part<V_FirstOf>(
                        Element.Part<V_FilteredArray>(
                            // Sort the taken index array.
                            Element.Part<V_SortedArray>(classData.ClassIndexes.GetVariable(), new V_ArrayElement()),
                            // Filter
                            Element.Part<V_And>(
                                // If the previous index was not taken, use that index.
                                !Element.Part<V_ArrayContains>(
                                    classData.ClassIndexes.GetVariable(),
                                    new V_ArrayElement() - 1
                                ),
                                // Make sure the index does not equal 0 so the resulting index is not -1.
                                new V_Compare(new V_ArrayElement(), Operators.NotEqual, new V_Number(0))
                            )
                        )
                    ) - 1 // Subtract 1 to get the previous index
                );
                // If the taken index array has 0 elements, use the length of the class array subtracted by 1.
                firstFree = Element.TernaryConditional(
                    new V_Compare(Element.Part<V_CountOf>(classData.ClassIndexes.GetVariable()), Operators.NotEqual, new V_Number(0)),
                    firstFree,
                    Element.Part<V_CountOf>(classData.ClassArray.GetVariable()) - 1
                );

                actionSet.AddAction(classReference.SetVariable(firstFree));
                actionSet.AddAction(classReference.SetVariable(
                    Element.TernaryConditional(
                        // If the index equals -1, use the length of the class array instead.
                        new V_Compare(classReference.GetVariable(), Operators.Equal, new V_Number(-1)),
                        Element.Part<V_CountOf>(classData.ClassArray.GetVariable()),
                        classReference.GetVariable()
                    )
                ));

                // Add the selected index to the taken indexes array.
                actionSet.AddAction(
                    classData.ClassIndexes.SetVariable(
                        Element.Part<V_Append>(
                            classData.ClassIndexes.GetVariable(),
                            classReference.GetVariable()
                        )
                    )
                );
            }
        }

        private IWorkshopTree NewStruct(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues)
        {
            var structObject = actionSet.VarCollection.Assign("_new_" + Name + "_class_index", actionSet.IsGlobal, true);
            SetInitialVariables(structObject, actionSet);

            // Run the constructor.
            AddObjectVariablesToAssigner(structObject, actionSet.IndexAssigner);
            constructor.Parse(actionSet, constructorValues);

            return structObject.GetVariable();
        }

        private void SetInitialVariables(IndexReference typeObject, ActionSet actionSet)
        {
            for (int i = 0; i < objectVariables.Count; i++)
            if (objectVariables[i].InitialValue != null)
            {
                actionSet.AddAction(typeObject.SetVariable(
                    value: (Element)objectVariables[i].InitialValue.Parse(actionSet),
                    index: i
                ));
            }
        }

        public override IndexReference GetObjectSource(DeltinScript translateInfo, IWorkshopTree element)
        {
            if (TypeKind == TypeKind.Struct) throw new NotImplementedException();
            return translateInfo.SetupClasses().ClassArray.CreateChild((Element)element);
        }

        /// <summary>
        /// Adds the class objects to the index assigner.
        /// </summary>
        /// <param name="source">The source of the type.</param>
        /// <param name="assigner">The assigner that the object variables will be added to.</param>
        public void AddObjectVariablesToAssigner(IndexReference source, VarIndexAssigner assigner)
        {
            for (int i = 0; i < objectVariables.Count; i++)
                assigner.Add(objectVariables[i], source.CreateChild(i));
        }

        public override void Call(ScriptFile script, DocRange callRange)
        {
            base.Call(script, callRange);
            script.AddDefinitionLink(callRange, DefinedAt);
            AddLink(new LanguageServer.Location(script.Uri, callRange));
        }
        public void AddLink(LanguageServer.Location location)
        {
            translateInfo.AddSymbolLink(this, location);
        }

        override public CompletionItem GetCompletion()
        {
            CompletionItemKind kind;
            if (TypeKind == TypeKind.Class) kind = CompletionItemKind.Class;
            else if (TypeKind == TypeKind.Struct) kind = CompletionItemKind.Struct;
            else throw new NotImplementedException();

            return new CompletionItem()
            {
                Label = Name,
                Kind = kind
            };
        }
    }
    
    public enum TypeKind
    {
        Class,
        Struct
    }

    public class Constructor : IParameterCallable, ICallable
    {
        public string Name => Type.Name;
        public AccessLevel AccessLevel { get; }
        public CodeParameter[] Parameters { get; protected set; }
        public LanguageServer.Location DefinedAt { get; }
        public CodeType Type { get; }
        public StringOrMarkupContent Documentation { get; protected set; }

        public Constructor(CodeType type, LanguageServer.Location definedAt, AccessLevel accessLevel)
        {
            Type = type;
            DefinedAt = definedAt;
            AccessLevel = accessLevel;
            Parameters = new CodeParameter[0];
        }

        public virtual void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues) {}

        public void Call(ScriptFile script, DocRange callRange)
        {
            if (DefinedAt == null) return;
            
            script.AddDefinitionLink(callRange, DefinedAt);
            if (Type is DefinedType)
                ((DefinedType)Type).AddLink(new LanguageServer.Location(script.Uri, callRange));
        }

        public string GetLabel(bool markdown) => HoverHandler.GetLabel("new " + Type.Name, Parameters, markdown, Documentation);
    }

    public class DefinedConstructor : Constructor, IApplyBlock
    {
        public Var[] ParameterVars { get; private set; }
        public Scope ConstructorScope { get; }
        public BlockAction Block { get; private set; }

        private ParseInfo parseInfo { get; }
        private DeltinScriptParser.ConstructorContext context { get; }

        public CallInfo CallInfo { get; }

        public DefinedConstructor(ParseInfo parseInfo, CodeType type, DeltinScriptParser.ConstructorContext context) : base(
            type,
            new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)),
            context.accessor()?.GetAccessLevel() ?? AccessLevel.Private)
        {
            this.parseInfo = parseInfo;
            this.context = context;
            CallInfo = new CallInfo(this, parseInfo.Script);

            ConstructorScope = type.GetObjectScope().Child();

            if (Type is DefinedType)
                ((DefinedType)Type).AddLink(DefinedAt);
        }

        public void SetupBlock()
        {
            var parameterInfo = CodeParameter.GetParameters(parseInfo, ConstructorScope, context.setParameters());
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;

            Block = new BlockAction(parseInfo, ConstructorScope, context.block());
            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            DefinedMethod.AssignParameters(actionSet, ParameterVars, parameterValues);
            Block.Translate(actionSet);
        }
    }

    public class ClassData
    {
        public IndexReference ClassIndexes { get; }
        public IndexReference ClassArray { get; }

        public ClassData(VarCollection varCollection)
        {
            ClassArray = varCollection.Assign("_classArray", true, false);
            if (DefinedType.CLASS_INDEX_WORKAROUND)
                ClassIndexes = varCollection.Assign("_classIndexes", true, false);
        }

        public ClassObjectResult CreateObject(ActionSet actionSet, string internalName)
        {
            var classReference = actionSet.VarCollection.Assign(internalName, actionSet.IsGlobal, true);
            DefinedType.GetClassIndex(classReference, actionSet, this);
            var classObject = ClassArray.CreateChild((Element)classReference.GetVariable());

            return new ClassObjectResult(classReference, classObject);
        }
    }

    public class ClassObjectResult
    {
        public IndexReference ClassReference { get; }
        public IndexReference ClassObject { get; }

        public ClassObjectResult(IndexReference classReference, IndexReference classObject)
        {
            ClassReference = classReference;
            ClassObject = classObject;
        }
    }
}