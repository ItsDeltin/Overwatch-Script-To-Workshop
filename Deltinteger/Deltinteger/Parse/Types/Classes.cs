using System;
using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public abstract class ClassType : CodeType
    {
        protected Scope ObjectScope { get; }
        protected Scope StaticScope { get; }

        public ClassType(string name) : base(name)
        {
            StaticScope = new Scope("class " + name);
            ObjectScope = StaticScope.Child("class " + name);
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Create the class.
            ClassObjectResult objectData = actionSet.Translate.DeltinScript.SetupClasses().CreateObject(actionSet, "_new_PathMap");

            New(actionSet, objectData, constructor, constructorValues, additionalParameterData);

            // Return the reference.
            return objectData.ClassReference.GetVariable();
        }

        protected virtual void New(ActionSet actionSet, ClassObjectResult objectData, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Parse the constructor.
            constructor.Parse(actionSet, constructorValues, additionalParameterData);
        }

        public override Scope GetObjectScope() => ObjectScope;
        public override Scope ReturningScope() => StaticScope;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };
    }

    public class ClassData
    {
        public static readonly bool CLASS_INDEX_WORKAROUND = true;
        public IndexReference ClassIndexes { get; }
        public IndexReference ClassArray { get; }

        public ClassData(VarCollection varCollection)
        {
            ClassArray = varCollection.Assign("_classArray", true, false);
            if (CLASS_INDEX_WORKAROUND)
                ClassIndexes = varCollection.Assign("_classIndexes", true, false);
        }

        public ClassObjectResult CreateObject(ActionSet actionSet, string internalName)
        {
            var classReference = actionSet.VarCollection.Assign(internalName, actionSet.IsGlobal, true);
            GetClassIndex(classReference, actionSet);
            var classObject = ClassArray.CreateChild((Element)classReference.GetVariable());

            return new ClassObjectResult(classReference, classObject);
        }

        public void GetClassIndex(IndexReference classReference, ActionSet actionSet)
        {
            // GetClassIndex() is less server load intensive than GetClassIndexWorkaround,
            // but due to a workshop bug with `Index Of Array Value`, the workaround may
            // need to be used instead.

            if (!CLASS_INDEX_WORKAROUND)
            {
                // Get the index of the first null value in the class array.
                actionSet.AddAction(classReference.SetVariable(
                    Element.Part<V_IndexOfArrayValue>(
                        ClassArray.GetVariable(),
                        new V_Null()
                    )
                ));
                
                // If the index equals -1, use the count of the class array instead.
                // TODO: Try setting the 1000th index of the class array to null instead.
                actionSet.AddAction(classReference.SetVariable(
                    Element.TernaryConditional(
                        new V_Compare(classReference.GetVariable(), Operators.Equal, new V_Number(-1)),
                        Element.Part<V_CountOf>(ClassArray.GetVariable()),
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
                            Element.Part<V_SortedArray>(ClassIndexes.GetVariable(), new V_ArrayElement()),
                            // Filter
                            Element.Part<V_And>(
                                // If the previous index was not taken, use that index.
                                !Element.Part<V_ArrayContains>(
                                    ClassIndexes.GetVariable(),
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
                    new V_Compare(Element.Part<V_CountOf>(ClassIndexes.GetVariable()), Operators.NotEqual, new V_Number(0)),
                    firstFree,
                    Element.Part<V_CountOf>(ClassArray.GetVariable()) - 1
                );

                actionSet.AddAction(classReference.SetVariable(firstFree));
                actionSet.AddAction(classReference.SetVariable(
                    Element.TernaryConditional(
                        // If the index equals -1, use the length of the class array instead.
                        new V_Compare(classReference.GetVariable(), Operators.Equal, new V_Number(-1)),
                        Element.Part<V_CountOf>(ClassArray.GetVariable()),
                        classReference.GetVariable()
                    )
                ));

                // Add the selected index to the taken indexes array.
                actionSet.AddAction(
                    ClassIndexes.SetVariable(
                        Element.Part<V_Append>(
                            ClassIndexes.GetVariable(),
                            classReference.GetVariable()
                        )
                    )
                );
            }
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