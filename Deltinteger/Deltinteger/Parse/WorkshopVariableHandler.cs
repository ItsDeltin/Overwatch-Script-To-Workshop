using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class WorkshopArrayBuilder
    {
        private WorkshopVariable Constructor { get; }
        IndexedVar Store;

        public WorkshopArrayBuilder(WorkshopVariable constructor, IndexedVar store)
        {
            Constructor = constructor;
            Store = store;
        }

        public static Element[] SetVariable(WorkshopArrayBuilder builder, Element value, Element targetPlayer, WorkshopVariable variable, bool optimize2ndDim, params Element[] index)
        {
            if (index == null || index.Length == 0)
            {
                if (variable.IsGlobal)
                    return new Element[] { Element.Part<A_SetGlobalVariable>(              variable, value) };
                else
                    return new Element[] { Element.Part<A_SetPlayerVariable>(targetPlayer, variable, value) };
            }

            if (index.Length == 1)
            {
                if (variable.IsGlobal)
                    return new Element[] { Element.Part<A_SetGlobalVariableAtIndex>(              variable, index[0], value) };
                else
                    return new Element[] { Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, variable, index[0], value) };
            }

            if (optimize2ndDim && index.Length > 2) throw new ArgumentOutOfRangeException("index", "Can't set more than 2 dimensions if optimizeIndexSet is true.");

            if (index.Length == 2 && optimize2ndDim)
            {
                Element baseArray = GetVariable(targetPlayer, variable, index[0]);
                Element baseArrayValue = Element.Part<V_Append>(
                    Element.Part<V_Append>(
                        Element.Part<V_ArraySlice>(
                            baseArray,
                            new V_Number(0),
                            index[1]
                        ),
                        value
                    ),
                    Element.Part<V_ArraySlice>(
                        baseArray,
                        index[1] + 1,
                        new V_Number(Constants.MAX_ARRAY_LENGTH)
                    )
                );

                return SetVariable(null, baseArrayValue, targetPlayer, variable, false, index[0]);
            }

            if (builder == null) throw new ArgumentNullException("builder", "Can't set multidimensional array if builder is null.");

            List<Element> actions = new List<Element>();

            Element root = GetRoot(targetPlayer, variable);

            // index is 2 or greater
            int dimensions = index.Length - 1;

            // Get the last array in the index path and copy it to variable B.
            actions.AddRange(
                SetVariable(builder, ValueInArrayPath(root, index.Take(index.Length - 1).ToArray()), targetPlayer, builder.Constructor, false)
            );

            // Set the value in the array.
            actions.AddRange(
                SetVariable(builder, value, targetPlayer, builder.Constructor, false, index.Last())
            );

            // Reconstruct the multidimensional array.
            for (int i = 1; i < dimensions; i++)
            {
                // Copy the array to the C variable
                actions.AddRange(
                    builder.Store.SetVariable(GetRoot(targetPlayer, builder.Constructor), targetPlayer)
                );

                // Copy the next array dimension
                Element array = ValueInArrayPath(root, index.Take(dimensions - i).ToArray());

                actions.AddRange(
                    SetVariable(builder, array, targetPlayer, builder.Constructor, false)
                );

                // Copy back the variable at C to the correct index
                actions.AddRange(
                    SetVariable(builder, builder.Store.GetVariable(targetPlayer), targetPlayer, builder.Constructor, false, index[i])
                );
            }
            // Set the final variable using Set At Index.
            actions.AddRange(
                SetVariable(builder, GetRoot(targetPlayer, builder.Constructor), targetPlayer, variable, false, index[0])
            );
            return actions.ToArray();
        }

        public static Element GetVariable(Element targetPlayer, WorkshopVariable variable, params Element[] index)
        {
            Element element = GetRoot(targetPlayer, variable);
            if (index != null)
                for (int i = 0; i < index.Length; i++)
                    element = Element.Part<V_ValueInArray>(element, index[i]);
            return element;
        }

        private static Element ValueInArrayPath(Element array, Element[] index)
        {
            if (index.Length == 0)
                return array;
            
            if (index.Length == 1)
                return Element.Part<V_ValueInArray>(array, index[0]);
            
            return Element.Part<V_ValueInArray>(ValueInArrayPath(array, index.Take(index.Length - 1).ToArray()), index.Last());
        }
        
        private static Element GetRoot(Element targetPlayer, WorkshopVariable variable)
        {
            if (variable.IsGlobal)
                return Element.Part<V_GlobalVariable>(variable);
            else
                return Element.Part<V_PlayerVariable>(targetPlayer, variable);
        }
    
        public static Element[] ModifyVariable(WorkshopArrayBuilder builder, Operation operation, Element value, Element targetPlayer, WorkshopVariable variable, params Element[] index)
        {
            if (index == null || index.Length == 0)
            {
                if (variable.IsGlobal)
                    return new Element[] { Element.Part<A_ModifyGlobalVariable>(              variable, EnumData.GetEnumValue(operation), value) };
                else
                    return new Element[] { Element.Part<A_ModifyPlayerVariable>(targetPlayer, variable, EnumData.GetEnumValue(operation), value) };
            }

            if (index.Length == 1)
            {
                if (variable.IsGlobal)
                    return new Element[] { Element.Part<A_ModifyGlobalVariableAtIndex>(              variable, index[0], EnumData.GetEnumValue(operation), value) };
                else
                    return new Element[] { Element.Part<A_ModifyPlayerVariableAtIndex>(targetPlayer, variable, index[0], EnumData.GetEnumValue(operation), value) };
            }

            if (builder == null) throw new ArgumentNullException("builder", "Can't modify multidimensional array if builder is null.");

            List<Element> actions = new List<Element>();

            Element root = GetRoot(targetPlayer, variable);

            // index is 2 or greater
            int dimensions = index.Length - 1;

            // Get the last array in the index path and copy it to variable B.
            actions.AddRange(
                SetVariable(builder, ValueInArrayPath(root, index.Take(index.Length - 1).ToArray()), targetPlayer, builder.Constructor, false)
            );

            // Modify the value in the array.
            actions.AddRange(
                ModifyVariable(builder, operation, value, targetPlayer, builder.Constructor, index.Last())
            );

            // Reconstruct the multidimensional array.
            for (int i = 1; i < dimensions; i++)
            {
                // Copy the array to the C variable
                actions.AddRange(
                    builder.Store.SetVariable(GetRoot(targetPlayer, builder.Constructor), targetPlayer)
                );

                // Copy the next array dimension
                Element array = ValueInArrayPath(root, index.Take(dimensions - i).ToArray());

                actions.AddRange(
                    SetVariable(builder, array, targetPlayer, builder.Constructor, false)
                );

                // Copy back the variable at C to the correct index
                actions.AddRange(
                    SetVariable(builder, builder.Store.GetVariable(targetPlayer), targetPlayer, builder.Constructor, false, index[i])
                );
            }
            // Set the final variable using Set At Index.
            actions.AddRange(
                SetVariable(builder, GetRoot(targetPlayer, builder.Constructor), targetPlayer, variable, false, index[0])
            );
            return actions.ToArray();
        }
    }
}