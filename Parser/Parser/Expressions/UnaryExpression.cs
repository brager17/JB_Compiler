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
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Unary;
    }
}