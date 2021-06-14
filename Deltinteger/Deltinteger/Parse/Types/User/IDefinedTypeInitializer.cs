using System;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IDefinedTypeInitializer : ICodeTypeInitializer, IScopeHandler
    {
        CodeType WorkingInstance { get; }

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

        static MarkupBuilder Hover(string tag, IDefinedTypeInitializer provider)
        {
            var builder = new MarkupBuilder().StartCodeLine().Add(tag + " " + provider.Name);

            if (provider.GenericsCount != 0)
                builder.Add("<" + string.Join(", ", provider.GenericTypes.Select(g => g.GetDeclarationName())) + ">");

            return builder.EndCodeLine();
        }

        public IElementProvider ApplyDeclaration(IDeclaration declaration, ParseInfo parseInfo)
        {
            IElementProvider result;

            // Function
            if (declaration is FunctionContext function)
                result = DefinedMethodProvider.GetDefinedMethod(parseInfo, this, function, this);
            
            // Variable
            else if (declaration is VariableDeclaration variable)
                result = new ClassVariable(this, new DefineContextHandler(parseInfo, variable)).GetVar();

            else throw new NotImplementedException();

            // result.AddDefaultInstance(this);
            return result;
        }
    }
}