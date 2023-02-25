using System;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IDefinedTypeInitializer : ICodeTypeInitializer, IScopeHandler
    {
        CodeType WorkingInstance { get; }
        ParsedMetaComment Doc { get; }

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

            builder.EndCodeLine();

            if (provider.Doc != null)
                builder.NewSection().Add(provider.Doc.Description);

            return builder;
        }

        public IElementProvider ApplyDeclaration(IDeclaration declaration, ParseInfo parseInfo)
        {
            // Function
            if (declaration is FunctionContext function)
                return DefinedMethodProvider.GetDefinedMethod(parseInfo, this, function, this);

            // Variable
            else if (declaration is VariableDeclaration variable)
                return new ClassVariable(this, new DefineContextHandler(parseInfo.SetThisType(this), variable)).GetVar();

            throw new NotImplementedException();
        }
    }
}