using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse
{
    public abstract class CodeType : IExpression
    {
        public string Name { get; }
        public Constructor[] Constructors { get; protected set; }

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

        public static readonly CodeType[] DefaultTypes = GetDefaultTypes();

        private static CodeType[] GetDefaultTypes()
        {
            var defaultTypes = new List<CodeType>();
            foreach (var enumData in EnumData.GetEnumData())
                defaultTypes.Add(new WorkshopEnumType(enumData));
            return defaultTypes.ToArray();
        }

        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            throw new Exception("Types can't be used like expressions.");
        }

        /// <summary>
        /// Determines if variables with this type can have their value changed.
        /// </summary>
        /// <returns></returns>
        public virtual bool Constant() => false;

        public virtual IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues)
        {
            // Classes that can't be created shouldn't have constructors.
            throw new NotImplementedException();
        }

        public abstract CompletionItem GetCompletion();

        public static bool TypeMatches(CodeType parameterType, CodeType valueType)
        {
            return parameterType == null || parameterType == valueType;
        }
    }

    public class WorkshopEnumType : CodeType
    {
        private Scope EnumScope { get; } = new Scope();
        public EnumData EnumData { get; }

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

        override public bool Constant() => true;

        override public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = EnumData.CodeName,
                Kind = CompletionItemKind.Enum
            };
        }
    }

    public class ScopedEnumMember : IScopeable, IExpression
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public Location DefinedAt { get; } = null;
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

        public IWorkshopTree Parse(ActionSet actionSet)
        {
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

    public class DefinedType : CodeType, ICallable
    {
        public TypeKind TypeKind { get; }
        public string KindString { get; }
        public Location DefinedAt { get; }
        private Scope objectScope { get; }
        private Scope staticScope { get; }
        private List<Var> objectVariables { get; } = new List<Var>();
        private DeltinScript translateInfo { get; }

        public DefinedType(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Type_defineContext typeContext) : base(typeContext.name.Text)
        {
            if (translateInfo.IsCodeType(Name))
                script.Diagnostics.Error($"A type with the name '{Name}' already exists.", DocRange.GetRange(typeContext.name));
            
            DefinedAt = new Location(script.Uri, DocRange.GetRange(typeContext.name));
            translateInfo.AddSymbolLink(this, DefinedAt);
            this.translateInfo = translateInfo;

            if (typeContext.CLASS() != null) 
            { 
                TypeKind = TypeKind.Class;
                KindString = "class";
            }
            else if (typeContext.STRUCT() != null) 
            { 
                TypeKind = TypeKind.Struct;
                KindString = "struct";
            }
            else throw new NotImplementedException();

            staticScope = translateInfo.GlobalScope.Child(KindString + " " + Name);
            objectScope = staticScope.Child(KindString + " " + Name);

            // Get the variables defined in the type.
            foreach (var definedVariable in typeContext.define())
            {
                Var newVar = Var.CreateVarFromContext(VariableDefineType.InClass, script, translateInfo, definedVariable);
                newVar.Finalize(UseScope(newVar.Static));
                if (!newVar.Static) objectVariables.Add(newVar);
            }

            // Todo: Static methods/macros.
            foreach (var definedMethod in typeContext.define_method())
                new DefinedMethod(script, translateInfo, UseScope(false), definedMethod);
                //UseScope(false).AddMethod(new DefinedMethod(script, translateInfo, UseScope(false), definedMethod), script.Diagnostics, DocRange.GetRange(definedMethod.name));

            foreach (var definedMacro in typeContext.define_macro())
                UseScope(false).AddMethod(new DefinedMacro(script, translateInfo, UseScope(false), definedMacro), script.Diagnostics, DocRange.GetRange(definedMacro.name));
            
            // Get the constructors.
            if (typeContext.constructor().Length > 0)
            {
                Constructors = new Constructor[typeContext.constructor().Length];
                for (int i = 0; i < Constructors.Length; i++)
                    Constructors[i] = new DefinedConstructor(script, translateInfo, this, typeContext.constructor(i));
            }
            else
            {
                // If there are no constructors, create a default constructor.
                Constructors = new Constructor[] {
                    new Constructor(this, new Location(script.Uri, DocRange.GetRange(typeContext.name)), AccessLevel.Public)
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

        override public IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues)
        {
            if (TypeKind == TypeKind.Class) return NewClass(actionSet.New(actionSet.IndexAssigner.CreateContained()), constructor, constructorValues);
            else if (TypeKind == TypeKind.Struct) return NewStruct(actionSet.New(actionSet.IndexAssigner.CreateContained()), constructor, constructorValues);
            else throw new NotImplementedException();
        }

        public const bool CLASS_INDEX_WORKAROUND = false;

        private IWorkshopTree NewClass(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues)
        {
            var classData = actionSet.Translate.DeltinScript.SetupClasses();
            
            // Classes are stored in the class array (`classData.ClassArray`),
            // this stores the index where the new class is created at.
            var classReference = actionSet.VarCollection.Assign("_new_" + Name + "_class_index", actionSet.IsGlobal, true);
            // GetClassIndex() is less server load intensive than GetClassIndexWorkaround,
            // but due to a workshop bug with `Index Of Array Value`, the workaround may
            // need to be used instead.
            if (!CLASS_INDEX_WORKAROUND)
                GetClassIndex(classReference, actionSet, classData);
            else
                GetClassIndexWorkaround(classReference, actionSet, classData);
            
            var classObject = classData.ClassArray.CreateChild((Element)classReference.GetVariable());
            SetInitialVariables(classObject, actionSet);

            // Run the constructor.
            AddObjectVariablesToAssigner(classObject, actionSet.IndexAssigner);
            constructor.Parse(actionSet, constructorValues);

            return classReference.GetVariable();
        }

        private void GetClassIndexWorkaround(IndexReference classReference, ActionSet actionSet, ClassData classData)
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

        private void GetClassIndex(IndexReference classReference, ActionSet actionSet, ClassData classData)
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

        // TODO: Should this be moved to the base class CodeType?
        public IndexReference GetObjectSource(DeltinScript translateInfo, IWorkshopTree element)
        {
            if (TypeKind == TypeKind.Struct) throw new NotImplementedException();
            return translateInfo.SetupClasses().ClassArray.CreateChild((Element)element);
        }

        // TODO: Should this be moved to the base class CodeType?
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

        public void Call(ScriptFile script, DocRange callRange)
        {
            script.AddDefinitionLink(callRange, DefinedAt);
            AddLink(new Location(script.Uri, callRange));
        }
        public void AddLink(Location location)
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
        public Location DefinedAt { get; }
        public CodeType Type { get; }
        public StringOrMarkupContent Documentation => null;

        public Constructor(CodeType type, Location definedAt, AccessLevel accessLevel)
        {
            Type = type;
            DefinedAt = definedAt;
            AccessLevel = accessLevel;
            Parameters = new CodeParameter[0];
        }

        public virtual void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues) {}

        public void Call(ScriptFile script, DocRange callRange)
        {
            script.AddDefinitionLink(callRange, DefinedAt);
            if (Type is DefinedType)
                ((DefinedType)Type).AddLink(new Location(script.Uri, callRange));
        }

        public string GetLabel(bool markdown) => HoverHandler.GetLabel(Type.Name, Parameters, markdown, null);
    }

    public class DefinedConstructor : Constructor
    {
        public Var[] ParameterVars { get; }
        public Scope ConstructorScope { get; }
        public BlockAction Block { get; }

        public DefinedConstructor(ScriptFile script, DeltinScript translateInfo, CodeType type, DeltinScriptParser.ConstructorContext context) : base(
            type,
            new Location(script.Uri, DocRange.GetRange(context.name)),
            context.accessor()?.GetAccessLevel() ?? AccessLevel.Private)
        {
            ConstructorScope = type.GetObjectScope().Child();

            var parameterInfo = CodeParameter.GetParameters(script, translateInfo, ConstructorScope, context.setParameters());
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;

            Block = new BlockAction(script, translateInfo, ConstructorScope, context.block());

            script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
            
            if (Type is DefinedType)
                ((DefinedType)Type).AddLink(DefinedAt);
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            DefinedMethod.AssignParameters(actionSet, ParameterVars, parameterValues);
            Block.Translate(actionSet);
        }
    }
}