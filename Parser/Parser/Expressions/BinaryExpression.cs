namespace Parser
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
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Binary;
    }
}