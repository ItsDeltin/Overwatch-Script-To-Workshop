using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class WorkshopArrayBuilder
    {
        private Variable Constructor { get; }
        IndexedVar Store;

        public WorkshopArrayBuilder(Variable constructor, IndexedVar store)
        {
            Constructor = constructor;
            Store = store;
        }

        public static Element[] SetVariable(WorkshopArrayBuilder builder, Element value, bool isGlobal, Element targetPlayer, Variable variable, bool optimize2ndDim, params Element[] index)
        {
            if (index == null || index.Length == 0)
            {
                if (isGlobal)
                    return new Element[] { Element.Part<A_SetGlobalVariable>(              EnumData.GetEnumValue(variable), value) };
                else
                    return new Element[] { Element.Part<A_SetPlayerVariable>(targetPlayer, EnumData.GetEnumValue(variable), value) };
            }

            if (index.Length == 1)
            {
                if (isGlobal)
                    return new Element[] { Element.Part<A_SetGlobalVariableAtIndex>(              EnumData.GetEnumValue(variable), index[0], value) };
                else
                    return new Element[] { Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, EnumData.GetEnumValue(variable), index[0], value) };
            }

            if (optimize2ndDim && index.Length > 2) throw new ArgumentOutOfRangeException("index", "Can't set more than 2 dimensions if optimizeIndexSet is true.");

            if (index.Length == 2 && optimize2ndDim)
            {
                Element baseArray = GetVariable(isGlobal, targetPlayer, variable, index[0]);
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

                return SetVariable(null, baseArrayValue, isGlobal, targetPlayer, variable, false, index[0]);
            }

            if (builder == null) throw new ArgumentNullException("builder", "Can't set multidimensional array if builder is null.");

            List<Element> actions = new List<Element>();

            Element root = GetRoot(isGlobal, targetPlayer, variable);

            // index is 2 or greater
            int dimensions = index.Length - 1;

            // Get the last array in the index path and copy it to variable B.
            actions.AddRange(
                SetVariable(builder, ValueInArrayPath(root, index.Take(index.Length - 1).ToArray()), isGlobal, targetPlayer, builder.Constructor, false)
            );

            // Set the value in the array.
            actions.AddRange(
                SetVariable(builder, value, isGlobal, targetPlayer, builder.Constructor, false, index.Last())
            );

            // Reconstruct the multidimensional array.
            for (int i = 1; i < dimensions; i++)
            {
                // Copy the array to the C variable
                actions.AddRange(
                    builder.Store.SetVariable(GetRoot(isGlobal, targetPlayer, builder.Constructor), targetPlayer)
                );

                // Copy the next array dimension
                Element array = ValueInArrayPath(root, index.Take(dimensions - i).ToArray());

                actions.AddRange(
                    SetVariable(builder, array, isGlobal, targetPlayer, builder.Constructor, false)
                );

                // Copy back the variable at C to the correct index
                actions.AddRange(
                    SetVariable(builder, builder.Store.GetVariable(targetPlayer), isGlobal, targetPlayer, builder.Constructor, false, index[i])
                );
            }
            // Set the final variable using Set At Index.
            actions.AddRange(
                SetVariable(builder, GetRoot(isGlobal, targetPlayer, builder.Constructor), isGlobal, targetPlayer, variable, false, index[0])
            );
            return actions.ToArray();
        }

        public static Element GetVariable(bool isGlobal, Element targetPlayer, Variable variable, params Element[] index)
        {
            Element element = GetRoot(isGlobal, targetPlayer, variable);
            //for (int i = index.Length - 1; i >= 0; i--)
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
        
        private static Element GetRoot(bool isGlobal, Element targetPlayer, Variable variable)
        {
            if (isGlobal)
                return Element.Part<V_GlobalVariable>(EnumData.GetEnumValue(variable));
            else
                return Element.Part<V_PlayerVariable>(targetPlayer, EnumData.GetEnumValue(variable));
        }
    
        public static Element[] ModifyVariable(WorkshopArrayBuilder builder, Operation operation, Element value, bool isGlobal, Element targetPlayer, Variable variable, params Element[] index)
        {
            if (index == null || index.Length == 0)
            {
                if (isGlobal)
                    return new Element[] { Element.Part<A_ModifyGlobalVariable>(              EnumData.GetEnumValue(variable), EnumData.GetEnumValue(operation), value) };
                else
                    return new Element[] { Element.Part<A_ModifyPlayerVariable>(targetPlayer, EnumData.GetEnumValue(variable), EnumData.GetEnumValue(operation), value) };
            }

            if (index.Length == 1)
            {
                if (isGlobal)
                    return new Element[] { Element.Part<A_ModifyGlobalVariableAtIndex>(              EnumData.GetEnumValue(variable), index[0], EnumData.GetEnumValue(operation), value) };
                else
                    return new Element[] { Element.Part<A_ModifyPlayerVariableAtIndex>(targetPlayer, EnumData.GetEnumValue(variable), index[0], EnumData.GetEnumValue(operation), value) };
            }

            if (builder == null) throw new ArgumentNullException("builder", "Can't modify multidimensional array if builder is null.");

            List<Element> actions = new List<Element>();

            Element root = GetRoot(isGlobal, targetPlayer, variable);

            // index is 2 or greater
            int dimensions = index.Length - 1;

            // Get the last array in the index path and copy it to variable B.
            actions.AddRange(
                SetVariable(builder, ValueInArrayPath(root, index.Take(index.Length - 1).ToArray()), isGlobal, targetPlayer, builder.Constructor, false)
            );

            // Modify the value in the array.
            actions.AddRange(
                ModifyVariable(builder, operation, value, isGlobal, targetPlayer, builder.Constructor, index.Last())
            );

            // Reconstruct the multidimensional array.
            for (int i = 1; i < dimensions; i++)
            {
                // Copy the array to the C variable
                actions.AddRange(
                    builder.Store.SetVariable(GetRoot(isGlobal, targetPlayer, builder.Constructor), targetPlayer)
                );

                // Copy the next array dimension
                Element array = ValueInArrayPath(root, index.Take(dimensions - i).ToArray());

                actions.AddRange(
                    SetVariable(builder, array, isGlobal, targetPlayer, builder.Constructor, false)
                );

                // Copy back the variable at C to the correct index
                actions.AddRange(
                    SetVariable(builder, builder.Store.GetVariable(targetPlayer), isGlobal, targetPlayer, builder.Constructor, false, index[i])
                );
            }
            // Set the final variable using Set At Index.
            actions.AddRange(
                SetVariable(builder, GetRoot(isGlobal, targetPlayer, builder.Constructor), isGlobal, targetPlayer, variable, false, index[0])
            );
            return actions.ToArray();
        }
    }
}