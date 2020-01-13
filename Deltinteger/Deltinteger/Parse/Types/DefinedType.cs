using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedType : CodeType
    {
        public TypeKind TypeKind { get; }
        private string TypeKindString { get; }
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
            staticScope.GroupCatch = true;
            objectScope = staticScope.Child(TypeKindString + " " + Name);
            objectScope.This = this;

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
            constructor.Parse(actionSet.New(classObject), constructorValues, null);

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
            constructor.Parse(actionSet, constructorValues, null);

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
        public override void AddObjectVariablesToAssigner(IndexReference source, VarIndexAssigner assigner)
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
}