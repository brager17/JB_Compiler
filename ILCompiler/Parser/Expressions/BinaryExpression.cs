using Parser.Lexer;

namespace Parser.Parser.Expressions
{
    public class BinaryExpression : IExpression
    {
        public IExpression Left;
        public IExpression Right;
        public TokenType TokenType;

        public BinaryExpression(IExpression left, IExpression right, TokenType tokenType)
        {
            Left = left;
            Right = right;
            TokenType = tokenType;
            ReturnType = left.ReturnType > right.ReturnType ? left.ReturnType : right.ReturnType;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Binary;
        public CompilerType ReturnType { get; }
    }
}