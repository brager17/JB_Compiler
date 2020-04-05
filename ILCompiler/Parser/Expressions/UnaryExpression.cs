namespace Parser.Parser.Expressions
{
    public enum UnaryType
    {
        Negative,
        Not
    }

    public class UnaryExpression : IExpression
    {
        public readonly IExpression Expression;
        public readonly UnaryType UnaryType;

        public UnaryExpression(IExpression expression,UnaryType unaryType)
        {
            Expression = expression;
            UnaryType = unaryType;
            ReturnType = expression.ReturnType;
        }


        public ExpressionType ExpressionType { get; } = ExpressionType.Unary;
        public CompilerType ReturnType { get; }
    }
}