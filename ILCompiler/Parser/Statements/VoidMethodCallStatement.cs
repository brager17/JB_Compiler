using Parser.Parser.Expressions;

namespace Parser.Parser.Statements
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