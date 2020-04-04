namespace Parser
{
    public class VoidMethodCallStatement : IStatement
    {
        public VoidMethodCallStatement(MethodCallExpression method)
        {
            Method = method;
        }
        public ExpressionType ExpressionType { get; } = ExpressionType.VoidMethodCallStatement;

        public readonly MethodCallExpression Method;
    }
}