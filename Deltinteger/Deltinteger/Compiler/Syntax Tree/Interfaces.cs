using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.SyntaxTree
{
    // Interfaces
    public interface INodeRange
    {
        DocRange Range { get; set; }
    }
    public interface IParseExpression : INodeRange { }
    public interface IParseStatement : INodeRange
    {
        MetaComment Comment { get; set; }
    }
    public interface IDeclaration
    {
        AttributeTokens Attributes { get; }
        IParseType Type { get; }
        Token Identifier { get; }
    }
    public interface IListComma
    {
        Token NextComma { get; set; }
    }
    public interface IParseType : INodeRange
    {
        Token GenericToken { get; }
        bool LookaheadValid { get; }
        bool IsVoid { get; }
        bool DefinitelyType { get; }
        bool Infer => false;
        bool Valid { get; }
    }
    public interface ITypeContextHandler
    {
        Token Identifier { get; }
        List<IParseType> TypeArgs { get; }
        int ArrayCount { get; }
        bool IsDefault { get; }
        bool Infer { get; }
    }
}