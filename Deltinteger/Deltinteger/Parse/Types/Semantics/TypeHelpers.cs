using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Lambda;

#nullable enable

namespace Deltin.Deltinteger.Parse
{
    public static class CodeTypeHelpers
    {
        public static ICodeTypeInitializer[] TypesFromName(this Scope scope, string name)
        {
            var types = new List<ICodeTypeInitializer>();
            scope.IterateParents(scope =>
            {
                types.AddRange(scope.Types.Where(t => t.Name == name));
                return false;
            });
            return types.ToArray();
        }

        /// <summary>There should be a special CodeType for void rather than it being null.
        /// This is here to make it more clear when we are testing if a type is void.</summary>
        public static bool IsVoid(CodeType type) => type == null;

        /// <summary>Is the input type the language-defined 'Any' type?</summary>
        public static bool IsAny(CodeType type) => type is AnyType;

        /// <summary>Is the type `Any` or any dimensional array of `Any`?</summary>
        public static bool IsLikeAny(CodeType type)
        {
            while (type is not AnyType)
            {
                if (type is ArrayType arrayType)
                {
                    type = arrayType.ArrayOfType;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Is the input type the language-defined 'unknown' type?</summary>
        public static bool IsUnknown(CodeType type) => type is AnyType anyType && anyType.Unknown;

        /// <summary>Is the input type compatible with any data type?</summary>
        public static bool IsCompatibleWithAny(CodeType type)
        {
            return SpreadPipeType(type).All(t => !t.IsConstant() && !type.Attributes.IsStruct);
        }

        public static bool IsLikeNumber(CodeType type) => SpreadPipeType(type).Any(type => type is NumberType || type is BooleanType);

        public static bool IsBoolean(CodeType type) => SpreadPipeType(type).Any(type => type is BooleanType);

        /// <summary>Is 'type' a pipe type? (ex: 'Number | Vector')</summary>
        public static bool IsPipeType(CodeType type) => type is PipeType;

        /// <summary>Determines if the input type is narrowed down to an explicit type.</summary>
        public static bool IsTypeConfident(CodeType type) => !IsAny(type) && !IsPipeType(type);

        /// <summary>Determines if a type is valid for a parameter.</summary>
        public static bool IsParameterInputValid(CodeType? parameterType, CodeType? valueType) =>
            parameterType != null && valueType != null && DoesTypeImplement(parameterType, valueType);

        public static bool AreEqual(CodeType a, CodeType b) => AreFunctionallyEqual(a, b, EqualitySettings.Strict);

        /// <summary>Determines if 'otherType; implements 'baseType' with inheritance.</summary>
        public static bool DoesTypeImplement(CodeType baseType, CodeType otherType)
        {
            return SpreadPipeType(otherType).Any(otherType =>
            {
                if (SpreadPipeType(baseType).Any(baseType => AreFunctionallyEqual(baseType, otherType, EqualitySettings.AnyAndInheritanceOk)))
                {
                    return true;
                }
                else if (otherType.Extends != null)
                {
                    return DoesTypeImplement(baseType, otherType.Extends);
                }
                else
                {
                    return false;
                }
            });
        }

        private static bool AreFunctionallyEqual(CodeType a, CodeType b, EqualitySettings equalitySettings)
        {
            // Unknown lambda types are compatible with known lambda types.
            if ((a is PortableLambdaType || b is PortableLambdaType) && (a is UnknownLambdaType || b is UnknownLambdaType))
                return true;

            // Is either 'a' or 'b' unknown?
            if (IsUnknown(a) || IsUnknown(b))
                return true;

            // Unstrict compatibility
            if (equalitySettings == EqualitySettings.AnyAndInheritanceOk)
            {
                // Any compatibility
                if (OneEach(a, b, IsAny, IsCompatibleWithAny))
                    return true;

                // Boolean checking any value
                if (OneEach(a, b, IsBoolean, IsCompatibleWithAny))
                    return true;

                // Number compatibility
                if (IsLikeNumber(a) && IsLikeNumber(b))
                    return true;
            }

            // Ensure 'a' and 'b' are compatible.
            if (a is StructInstance ^ b is StructInstance ||
                a is DefinedClass ^ b is DefinedClass ||
                a is ArrayType ^ b is ArrayType ||
                a is PortableLambdaType ^ b is PortableLambdaType ||
                a is PipeType ^ b is PipeType)
                return false;

            // Struct comparison
            if (a is StructInstance aStr && b is StructInstance bStr)
            {
                return aStr.Variables.All(var =>
                {
                    var matchingVariable = bStr.Variables.FirstOrDefault(otherVar => var.Name == otherVar.Name);
                    return matchingVariable != null && Compare((CodeType)var.CodeType, (CodeType)matchingVariable.CodeType, equalitySettings);
                });
            }
            // Class comparison
            else if (a is DefinedClass aClass && b is DefinedClass bClass)
            {
                return aClass.Provider == bClass.Provider && DoGenericsMatch(a, b, equalitySettings);
            }
            // Array comparison
            else if (a is ArrayType aArray && b is ArrayType bArray)
            {
                return Compare(aArray.ArrayOfType, bArray.ArrayOfType, equalitySettings);
            }
            // Lambda comparison
            else if (a is PortableLambdaType aLambda && b is PortableLambdaType bLambda)
            {
                if (aLambda.Parameters.Length != bLambda.Parameters.Length)
                    return false;

                if (aLambda.ParameterTypesKnown)
                    // Make sure the parameters match.
                    for (int i = 0; i < aLambda.Parameters.Length; i++)
                    {
                        if (aLambda.Parameters[i] == null)
                        {
                            if (bLambda.Parameters[i] != null && bLambda.Parameters[i].IsConstant())
                                return false;
                        }
                        else if (!Compare(aLambda.Parameters[i], bLambda.Parameters[i], equalitySettings))
                            return false;
                    }

                // Make sure the return type matches.
                return aLambda.ReturnsValue == bLambda.ReturnsValue &&
                    (!aLambda.ReturnsValue || Compare(aLambda.ReturnType, bLambda.ReturnType, equalitySettings));
            }
            // Pipe type comparison
            else if (a is PipeType aPipe && b is PipeType bPipe)
            {
                return SpreadPipeType(aPipe).All(at => SpreadPipeType(bPipe).Any(bt => Compare(at, bt, equalitySettings)));
            }

            // Default
            else return a == b;
        }

        private static bool DoGenericsMatch(CodeType a, CodeType b, EqualitySettings equalitySettings)
        {
            if (a.Generics.Length != b.Generics.Length)
                return false;

            for (int i = 0; i < a.Generics.Length; i++)
                if (!Compare(a.Generics[i], b.Generics[i], equalitySettings))
                    return false;

            return true;
        }

        private static bool Compare(CodeType a, CodeType b, EqualitySettings equalitySettings)
        {
            return equalitySettings switch
            {
                EqualitySettings.Strict => AreFunctionallyEqual(a, b, equalitySettings),
                EqualitySettings.AnyAndInheritanceOk => DoesTypeImplement(a, b),
                _ => throw new NotImplementedException(),
            };
        }

        private static IEnumerable<CodeType> SpreadPipeType(CodeType type)
        {
            if (type is PipeType pipeType)
                foreach (var innerType in pipeType.IncludedTypes)
                    foreach (var pass in SpreadPipeType(innerType))
                        yield return pass;
            else
                yield return type;
        }

        private static bool OneEach(CodeType a, CodeType b, Func<CodeType, bool> conditionA, Func<CodeType, bool> conditionB)
        {
            return (conditionA(a) && conditionB(b)) || (conditionA(b) && conditionB(a));
        }

        /// <summary>
        /// Determines how strict type equality is.
        /// </summary>
        public enum EqualitySettings
        {
            /// <summary>Types need to be as close as possible.</summary>
            Strict,
            /// <summary>Type matching with any and inheritance is ok.</summary>
            AnyAndInheritanceOk
        }
    }
}