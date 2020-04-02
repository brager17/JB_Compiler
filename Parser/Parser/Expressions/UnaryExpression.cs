namespace Parser
{
    public enum UnaryType
    {
        Positive,
        Negative
    }

    public class UnaryExpression : IExpression
    {
        public readonly IExpression Expression;
        public readonly UnaryType UnaryType;

        public UnaryExpression(IExpression expression)
        {
            Expression = expression;
            UnaryType = UnaryType.Negative;
            ReturnType = expression.ReturnType;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Unary;
        public CompilerType ReturnType { get; }
    }
}