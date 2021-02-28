using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class TypeFromContext
    {
        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, Scope scope, IParseType typeContext)
        {
            if (typeContext is ITypeContextHandler tch) return GetCodeTypeFromContext(parseInfo, scope, tch);
            else if (typeContext is LambdaType lambda) return GetCodeTypeFromContext(parseInfo, scope, lambda);
            else if (typeContext is GroupType groupType) return GetCodeTypeFromContext(parseInfo, scope, groupType);
            else if (typeContext is PipeTypeContext pipeType) return GetCodeTypeFromContext(parseInfo, scope, pipeType);
            else throw new NotImplementedException(typeContext.GetType().Name);
        }

        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, Scope scope, ITypeContextHandler typeContext) => GetCodeTypeFromContext(new DefaultTypeContextError(parseInfo, typeContext, true), parseInfo, scope, typeContext);

        public static CodeType GetCodeTypeFromContext(ITypeContextError errorHandler, ParseInfo parseInfo, Scope scope, ITypeContextHandler typeContext)
        {
            if (typeContext == null) return parseInfo.TranslateInfo.Types.Any();

            // Get the type arguments.
            var typeArgs = new CodeType[typeContext.TypeArgs?.Count ?? 0];
            for (int i = 0; i < typeArgs.Length; i++)
                typeArgs[i] = GetCodeTypeFromContext(parseInfo, scope, typeContext.TypeArgs[i]);
            
            var instanceInfo = new GetInstanceInfo(typeArgs);

            CodeType type;
            if (typeContext.IsDefault)
            {
                if (typeContext.Infer)
                    parseInfo.Script.Diagnostics.Hint("Unable to infer type", typeContext.Identifier.Range);

                type = parseInfo.TranslateInfo.Types.Any();
            }
            else
            {
                var providers = scope.TypesFromName(typeContext.Identifier.GetText());

                // No types found.
                if (providers.Length == 0)
                {
                    errorHandler.Nonexistent();
                    return parseInfo.TranslateInfo.Types.GetInstance<AnyType>();
                }

                var fallback = providers[0]; // Used when no types match.

                // Match generics.
                providers = providers.Where(t => t.GenericsCount == typeContext.TypeArgs.Count).ToArray();
                if (providers.Length == 0) // No types match the generics count.
                {
                    // Add the error.
                    errorHandler.IncorrectTypeArgsCount(fallback);
                    
                    // Return the fallback.
                    return fallback.GetInstance();
                }

                // TODO: Check ambiguities
                // Get the type instance.
                parseInfo.TranslateInfo.GetComponent<TypeTrackerComponent>().Track(providers[0], typeArgs.Select(t => t.GenericUsage).ToArray());
                type = providers[0].GetInstance(instanceInfo);
                type.Call(parseInfo, typeContext.Identifier.Range);
            }
            
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

            if (left.IsConstant()) parseInfo.Script.Diagnostics.Error("Types used in unions cannot be constant", type.Left.Range);
            if (right.IsConstant()) parseInfo.Script.Diagnostics.Error("Types used in unions cannot be constant", type.Right.Range);

            return new PipeType(left, right);
        }
    }

    public interface ITypeContextError
    {
        void Nonexistent();
        void IncorrectTypeArgsCount(ICodeTypeInitializer fallback);
        void ApplyErrors();
    }

    class DefaultTypeContextError : ITypeContextError
    {
        public bool Exists { get; private set; } = true;
        private readonly ParseInfo _parseInfo;
        private readonly ITypeContextHandler _context;
        private readonly bool _autoApplyErrors;
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public DefaultTypeContextError(ParseInfo parseInfo, ITypeContextHandler context, bool autoApplyErrors)
        {
            _parseInfo = parseInfo;
            _context = context;
            _autoApplyErrors = autoApplyErrors;
        }

        public void Nonexistent() 
        {
            AddError("No types by the name of '" + _context.Identifier.Text + "' exists in the current context", _context.Identifier.Range);
            Exists = false;
        }
        
        public void IncorrectTypeArgsCount(ICodeTypeInitializer fallback)
        {
            // Add the error.
            if (fallback.GenericsCount == 0)
                AddError("The type '" + fallback.Name + "' cannot be used with type arguments", _context.Identifier.Range);
            else
                AddError("Type type '" + fallback.Name + "' requires " + fallback.GenericsCount + " type arguments", _context.Identifier.Range);
        }

        private void AddError(string message, DocRange range)
        {
            var diagnostic = new Diagnostic(message, range, Diagnostic.Error);
            _diagnostics.Add(diagnostic);

            if (_autoApplyErrors)
                _parseInfo.Script.Diagnostics.AddDiagnostic(diagnostic);
        }

        public void ApplyErrors()
        {
            if (_autoApplyErrors) return;

            foreach (var error in _diagnostics)
                _parseInfo.Script.Diagnostics.AddDiagnostics(_diagnostics.ToArray());
        }
    }
}