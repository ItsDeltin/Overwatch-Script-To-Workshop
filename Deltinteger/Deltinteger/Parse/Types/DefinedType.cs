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
        public LanguageServer.Location DefinedAt { get; }
        private Scope objectScope { get; }
        private Scope staticScope { get; }
        private List<ObjectVariable> objectVariables { get; } = new List<ObjectVariable>();
        private ParseInfo parseInfo { get; }
        private DeltinScriptParser.Type_defineContext typeContext { get; }

        public DefinedType(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Type_defineContext typeContext, List<IApplyBlock> applyMethods) : base(typeContext.name.Text)
        {
            CanBeDeleted = true;
            this.typeContext = typeContext;
            this.parseInfo = parseInfo;

            if (parseInfo.TranslateInfo.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", DocRange.GetRange(typeContext.name));
            
            DefinedAt = new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(typeContext.name));
            parseInfo.TranslateInfo.AddSymbolLink(this, DefinedAt);

            staticScope = parseInfo.TranslateInfo.GlobalScope.Child("class " + Name);
            staticScope.GroupCatch = true;
            objectScope = staticScope.Child("class " + Name);
            objectScope.This = this;

            // Todo: Static methods/macros.
            foreach (var definedMethod in typeContext.define_method())
            {
                var newMethod = new DefinedMethod(parseInfo, UseScope(false), definedMethod, this);
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

        public void ResolveElements()
        {
            // Get the variables defined in the type.
            foreach (var definedVariable in typeContext.define())
            {
                Var newVar = Var.CreateVarFromContext(VariableDefineType.InClass, parseInfo, definedVariable);
                newVar.Finalize(UseScope(newVar.Static));
                if (!newVar.Static) objectVariables.Add(new ObjectVariable(newVar));
            }
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            foreach (ObjectVariable variable in objectVariables)
                variable.SetArrayStore(translateInfo.VarCollection);
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
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            var classData = actionSet.Translate.DeltinScript.SetupClasses();
            
            // Classes are stored in the class array (`classData.ClassArray`),
            // this stores the index where the new class is created at.
            var classReference = actionSet.VarCollection.Assign("_new_" + Name + "_class_index", actionSet.IsGlobal, true);
            classData.GetClassIndex(classReference, actionSet);
            
            // Run the constructor.
            SetInitialVariables(actionSet, (Element)classReference.GetVariable());
            AddObjectVariablesToAssigner((Element)classReference.GetVariable(), actionSet.IndexAssigner);
            constructor.Parse(actionSet.New((Element)classReference.GetVariable()), constructorValues, null);

            return classReference.GetVariable();
        }

        private void SetInitialVariables(ActionSet actionSet, Element reference)
        {
            foreach (ObjectVariable variable in objectVariables)
            if (variable.Variable.InitialValue != null)
            {
                actionSet.AddAction(variable.ArrayStore.SetVariable(
                    value: (Element)variable.Variable.InitialValue.Parse(actionSet),
                    index: reference
                ));
            }
        }

        /// <summary>
        /// Adds the class objects to the index assigner.
        /// </summary>
        /// <param name="source">The source of the type.</param>
        /// <param name="assigner">The assigner that the object variables will be added to.</param>
        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            for (int i = 0; i < objectVariables.Count; i++)
                objectVariables[i].AddToAssigner((Element)reference, assigner);
        }

        /// <summary>
        /// Deletes a variable from memory.
        /// </summary>
        /// <param name="actionSet">The action set to add the actions to.</param>
        /// <param name="reference">The object reference.</param>
        public override void Delete(ActionSet actionSet, Element reference)
        {
            foreach (ObjectVariable objectVariable in objectVariables)
                actionSet.AddAction(objectVariable.ArrayStore.SetVariable(
                    value: new V_Number(0),
                    index: reference
                ));
        }

        public override void Call(ScriptFile script, DocRange callRange)
        {
            base.Call(script, callRange);
            script.AddDefinitionLink(callRange, DefinedAt);
            AddLink(new LanguageServer.Location(script.Uri, callRange));
        }
        public void AddLink(LanguageServer.Location location)
        {
            parseInfo.TranslateInfo.AddSymbolLink(this, location);
        }

        override public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.Class
            };
        }
    }

    class ObjectVariable
    {
        public Var Variable { get; }
        public IndexReference ArrayStore { get; private set; }

        public ObjectVariable(Var variable)
        {
            Variable = variable;
        }

        public void SetArrayStore(VarCollection varCollection)
        {
            ArrayStore = varCollection.Assign(Variable.Name, true, false);
        }

        public void AddToAssigner(Element reference, VarIndexAssigner assigner)
        {
            assigner.Add(Variable, ArrayStore.CreateChild(reference));
        }
    }
}