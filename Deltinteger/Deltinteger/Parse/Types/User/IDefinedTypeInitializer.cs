using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IDefinedTypeInitializer : ICodeTypeInitializer, IScopeHandler
    {
        CodeType WorkingInstance { get; }
        void AddVariable(IVariable var);
        void AddMacro(MacroVarProvider macro);

        public static ICodeTypeInitializer GetInitializer(ParseInfo parseInfo, Scope scope, ClassContext typeContext)
        {
            // Class
            if (typeContext.DeclaringToken.TokenType == Deltin.Deltinteger.Compiler.TokenType.Class)
                return new DefinedClassInitializer(parseInfo, scope, typeContext);
            // Struct
            else if (typeContext.DeclaringToken.TokenType == Deltin.Deltinteger.Compiler.TokenType.Struct)
                return new DefinedStructInitializer(parseInfo, scope, typeContext);
            else throw new NotImplementedException();
        }

        public IElementProvider ApplyDeclaration(IDeclaration declaration, ParseInfo parseInfo)
        {
            IElementProvider result;

            // Function
            if (declaration is FunctionContext function)
                result = DefinedMethodProvider.GetDefinedMethod(parseInfo, this, function, this);
            
            // Macro
            else if (declaration is MacroFunctionContext macroFunction)
                result = parseInfo.GetMacro(this, macroFunction);

            // Variable
            else if (declaration is VariableDeclaration variable)
                result = new ClassVariable(this, new DefineContextHandler(parseInfo, variable))
                    .GetVar(var => AddVariable(var), macroVarProvider => AddMacro(macroVarProvider));

            else throw new NotImplementedException();

            result.AddDefaultInstance(this);
            return result;
        }
    }
}