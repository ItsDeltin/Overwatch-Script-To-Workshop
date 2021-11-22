using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.SyntaxTree
{
    public class TypeSyntax : Node, IParseType
    {
        public TypeNamePart[] Parts { get; }

        // IParseType
        public bool IsVoid { get; }
        public bool LookaheadValid => Parts[0].Identifier;
        public bool Infer => Parts[0].Identifier && Parts[0].Identifier.TokenType == TokenType.Define;
        public bool DefinitelyType => IsVoid;
        Token IParseType.GenericToken => Parts[0].Identifier;
        public bool Valid => valid;

        readonly bool valid;


        public TypeSyntax(TypeNamePart[] parts)
        {
            Parts = parts;
            IsVoid = false;

            valid = Parts[0].Identifier;
        }

        public TypeSyntax(Token @void)
        {
            IsVoid = true;
        }


        public class TypeNamePart : INamedType
        {
            public Token Identifier { get; }
            public List<IParseType> TypeArgs { get; }


            // INamedType
            public int ArrayCount { get; }
            public bool IsDefault { get; }
            public bool Infer { get; }

            public TypeNamePart(Token identifier, List<IParseType> typeArgs)
            {
                Identifier = identifier;
                TypeArgs = typeArgs;
            }
        }
    }

    public class LambdaType : Node, IParseType
    {
        public Token ArrowToken { get; }
        public Token Const { get; }
        public List<IParseType> Parameters { get; }
        public IParseType ReturnType { get; }

        public LambdaType(IParseType singleParameter, Token const_, IParseType returnType, Token arrowToken)
        {
            ArrowToken = arrowToken;
            Const = const_;
            Parameters = new List<IParseType> { singleParameter };
            ReturnType = returnType;
        }

        public LambdaType(List<IParseType> parameters, Token const_, IParseType returnType, Token arrowToken)
        {
            ArrowToken = arrowToken;
            Const = const_;
            Parameters = parameters;
            ReturnType = returnType;
        }

        public bool LookaheadValid => ArrowToken && ReturnType.LookaheadValid;
        public bool IsVoid => false;
        public bool DefinitelyType => true;
        public bool Valid => ArrowToken;
        Token IParseType.GenericToken => null;
    }

    public class GroupType : Node, IParseType
    {
        public IParseType Type { get; }
        public int ArrayCount { get; }

        public GroupType(IParseType type, int arrayCount)
        {
            Type = type;
            ArrayCount = arrayCount;
        }

        public bool LookaheadValid => Type.LookaheadValid;
        public bool IsVoid => Type.IsVoid;
        public bool DefinitelyType => Type.DefinitelyType;
        Token IParseType.GenericToken => Type.GenericToken;
        public bool Valid => Type.Valid;
    }

    public class PipeTypeContext : Node, IParseType
    {
        public IParseType Left { get; }
        public IParseType Right { get; }

        public PipeTypeContext(IParseType left, IParseType right)
        {
            Left = left;
            Right = right;
        }

        public Token GenericToken => throw new NotImplementedException();
        public bool LookaheadValid => true;
        public bool IsVoid => false;
        public bool DefinitelyType => true;
        public bool Valid => true;
    }

}