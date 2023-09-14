using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    static class StructHelper
    {
        public static IWorkshopTree ExtractArbritraryValue(IWorkshopTree workshopValue)
        {
            IWorkshopTree current = workshopValue;
            while (current is IStructValue step)
                current = step.GetArbritraryValue();

            return current;
        }

        public static IWorkshopTree ValueInArray(IWorkshopTree array, IWorkshopTree index)
        {
            if (array is IStructValue structArray)
                return new ValueInStructArray(structArray, index);

            return Element.ValueInArray(array, index);
        }

        public static IWorkshopTree CreateArray(IWorkshopTree[] elements)
        {
            // Struct array
            if (elements.Any(value => value is IStructValue))
            {
                // Ensure that all the values are structs.
                if (!elements.All(value => value is IStructValue))
                    throw new Exception("Cannot mix normal and struct values in an array");

                return new StructArray(Array.ConvertAll(elements, item => (IStructValue)item));
            }

            // Normal array
            return Element.CreateArray(elements);
        }

        public static IWorkshopTree BridgeIfRequired(IWorkshopTree value, Func<IWorkshopTree, IWorkshopTree> converter)
        {
            if (value is IStructValue structValue)
                return structValue.Bridge(args => converter(args.Value));

            return converter(value);
        }

        public static IStructValue ExtractStructValue(IWorkshopTree value)
        {
            // Struct value.
            if (value is IStructValue structValue) return structValue;

            // Empty array.
            var emptyArray = MakeEmptyArray(value);
            if (emptyArray != null) return emptyArray;

            // Unknown
            throw new Exception(value.ToString() + " is not a valid struct value.");
        }

        /// <summary>Generates a StructArray from a workshop value.</summary>
        static StructArray MakeEmptyArray(IWorkshopTree value)
        {
            if (value is Element element)
            {
                if (element.Function.Name == "Empty Array" || element.Function.Name == "Null")
                    return new StructArray(new IStructValue[0]);

                else if (element.Function.Name == "Array")
                {
                    var arr = new IStructValue[element.ParameterValues.Length];
                    for (int i = 0; i < element.ParameterValues.Length; i++)
                    {
                        arr[i] = MakeEmptyArray(element.ParameterValues[i]);
                        if (arr[i] == null) return null;
                    }

                    return new StructArray(arr);
                }
            }
            return null;
        }

        /// <summary>Flattens a struct value into an array of workshop values.</summary>
        public static IWorkshopTree[] Flatten(IWorkshopTree value)
        {
            if (value is IStructValue structValue)
                return structValue.GetAllValues();
            else
                return new[] { value };
        }

        /// <summary>Extracts all final variable paths in a struct. Useful for knowing the workshop variables
        /// a struct will create.</summary>
        /// <returns>An input value of `{ w: 0, x: { y: 1, z: 2 } }` will return ['w', 'x_y', 'x_z'].</returns>
        public static StructPath[] ExtractAllPaths(IStructValue value)
        {
            var paths = new List<StructPath>();
            void RecursiveUnfold(IStructValue value, IEnumerable<string> currentPath)
            {
                var topNames = value.GetNames();
                foreach (var name in topNames)
                {
                    var innerValue = value.GetValue(name);
                    if (innerValue is IStructValue innerStructValue)
                        RecursiveUnfold(innerStructValue, currentPath.Append(name));
                    else
                        paths.Add(new StructPath(currentPath.Append(name)));
                }
            }
            RecursiveUnfold(value, Enumerable.Empty<string>());
            return paths.ToArray();
        }

        /// <summary>
        /// Locate and replace patterns in a workshop value. Used for some advanced optimizations.
        /// </summary>
        public static PatternTemplate Template(IWorkshopTree inputValue, Func<IWorkshopTree, StructPath?> patternMatch)
        {
            var patternOccurrences = new Dictionary<StructPath, StructVariableOccurences>();
            void RecursiveTemplate(IWorkshopTree currentValue, Action<IWorkshopTree> replaceCurrentValue)
            {
                // Check if the current workshop value matches a pattern.
                var current = patternMatch(currentValue);

                // If a pattern was found, add it to the list of occurrences.
                if (current is not null && replaceCurrentValue is not null)
                {
                    if (!patternOccurrences.ContainsKey(current.Value))
                        // This pattern was found for the first time, add it to the dictionary.
                        patternOccurrences.Add(current.Value, new StructVariableOccurences(new List<PatternOccurence>()));

                    patternOccurrences[current.Value].Occurences.Add(new PatternOccurence(replaceCurrentValue));
                }
                // Try to recusively find the pattern in the parameter values of a workshop value.
                else if (currentValue is Element element)
                {
                    for (int i = 0; i < element.ParameterValues.Length; i++)
                    {
                        if (element.ParameterValues[i] != null)
                        {
                            int x = i;
                            RecursiveTemplate(element.ParameterValues[i], newValue => element.ParameterValues[x] = newValue);
                        }
                    }
                }
            }
            RecursiveTemplate(inputValue, null);
            return new PatternTemplate(patternOccurrences);
        }

        /// <summary>
        /// Applies <c>StructHelper.Template</c> to each value in a workshop struct.
        /// </summary>
        public static NamedPatternTemplate[] SpreadTemplates(IWorkshopTree topValue, Func<IWorkshopTree, StructPath?> patternMatch)
        {
            var templates = new List<NamedPatternTemplate>();
            void RecursiveSpread(IWorkshopTree topValue, IEnumerable<string> currentPath)
            {
                // Do not attempt to template structs, instead template each value in the struct.
                if (topValue is IStructValue structValue)
                {
                    foreach (var (name, value) in structValue.GetNames().Zip(structValue.GetAllValues()))
                        RecursiveSpread(value, currentPath.Append(name));
                }
                else
                {
                    templates.Add(new(new StructPath(currentPath), Template(topValue, patternMatch)));
                }
            }
            RecursiveSpread(topValue, Enumerable.Empty<string>());
            return templates.ToArray();
        }
    }

    /// <summary>Denotes a path to a value in a struct.</summary>
    record struct StructPath(IEnumerable<string> Steps)
    {
        public override readonly string ToString() => $"[{string.Join(", ", Steps)}]";
    }

    /// <summary>Contains a <c>PatternTemplate</c> linked to a struct path.</summary>
    record struct NamedPatternTemplate(StructPath Path, PatternTemplate PatternTemplate)
    {
        public override readonly string ToString() => $"Path: {Path}, Count: {PatternTemplate.Patterns.Count}";
    }

    /// <summary>Contains the patterns found after a template.</summary>
    record struct PatternTemplate(Dictionary<StructPath, StructVariableOccurences> Patterns);

    /// <summary>Contains the list of pattern occurrences from a template.</summary>
    record struct StructVariableOccurences(List<PatternOccurence> Occurences)
    {
        public readonly int Count() => Occurences.Count();

        /// <summary>Replaces each occurence of the pattern with a new value.</summary>
        public readonly void ReplaceWith(IWorkshopTree withValue)
        {
            foreach (var occurence in Occurences)
                occurence.ReplaceValue(withValue);
        }
    }

    /// <summary>Contains a function that can be used to replace a pattern occurrence.</summary>
    record struct PatternOccurence(Action<IWorkshopTree> ReplaceValue);
}