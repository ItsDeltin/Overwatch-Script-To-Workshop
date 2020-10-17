using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class TypeFromContext
    {
        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, Scope scope, IParseType typeContext) => GetCodeTypeFromContext(parseInfo, scope, (dynamic)typeContext);

        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, Scope scope, ParseType typeContext)
        {
            if (typeContext == null) return null;
            if (typeContext.IsDefault) return parseInfo.TranslateInfo.Types.GetInstance<DynamicType>();
            
            // Get the type arguments.
            var typeArgs = new CodeType[typeContext.TypeArgs.Count];
            for (int i = 0; i < typeArgs.Length; i++)
                typeArgs[i] = GetCodeTypeFromContext(parseInfo, scope, typeContext.TypeArgs[i]);
            
            var instanceInfo = new GetInstanceInfo(typeArgs);
            
            ICodeTypeInitializer[] types = scope.TypesFromName(typeContext.Identifier.Text);

            if (types.Length == 0)
            {
                parseInfo.Script.Diagnostics.Error("No types by the name of '" + typeContext.Identifier.Text + "' exists in the current context", typeContext.Identifier.Range);
                return parseInfo.TranslateInfo.Types.GetInstance<DynamicType>();
            }
            
            var fallback = types[0];

            types = types.Where(t => t.GenericsCount == typeContext.TypeArgs.Count).ToArray();
            if (types.Length == 0) // No types match the generics count.
            {
                // Add the error.
                if (fallback.GenericsCount == 0)
                    parseInfo.Script.Diagnostics.Error("The type '" + fallback.Name + "' cannot be used with type arguments", typeContext.Identifier.Range);
                else
                    parseInfo.Script.Diagnostics.Error("Type type '" + fallback.Name + "' requires " + fallback.GenericsCount + " type arguments", typeContext.Identifier.Range);
                
                // Return the fallback.
                return fallback.GetInstance();
            }

            // TODO: Check ambiguities

            CodeType type = types[0].GetInstance(instanceInfo);
            type.Call(parseInfo, typeContext.Identifier.Range);

            for (int i = 0; i < typeContext.ArrayCount; i++)
                type = new ArrayType(parseInfo.TranslateInfo.Types, type);
            
            return type;
        }

        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, Scope scope, LambdaType type)
        {
            // Get the lambda type's parameters.
            var parameters = new CodeType[type.Parameters.Count];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = GetCodeTypeFromContext(parseInfo, scope, type.Parameters[i]);

                // Constant types are not allowed.
                if (parameters[i] != null && parameters[i].IsConstant())
                    parseInfo.Script.Diagnostics.Error("The constant type '" + parameters[i].GetName() + "' cannot be used in method types", type.Parameters[i].Range);
            }

            // Get the return type.
            CodeType returnType = null;
            bool returnsValue = false;
            
            if (!type.ReturnType.IsVoid)
            {
                returnType = GetCodeTypeFromContext(parseInfo, scope, type.ReturnType);
                returnsValue = true;
            }
            
            return new PortableLambdaType(LambdaKind.Portable, parameters, returnsValue, returnType, true);
        }

        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, Scope scope, GroupType type)
        {
            // Get the contained type.
            var result = GetCodeTypeFromContext(parseInfo, scope, type.Type);
            // Get the array type.
            for (int i = 0; i < type.ArrayCount; i++) result = new ArrayType(parseInfo.TranslateInfo.Types, result);
            // Done.
            return result;
        }

        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, Scope scope, PipeTypeContext type)
        {
            var left = GetCodeTypeFromContext(parseInfo, scope, type.Left);
            var right = GetCodeTypeFromContext(parseInfo, scope, type.Right);
            return new PipeType(left, right);
        }
    }
}