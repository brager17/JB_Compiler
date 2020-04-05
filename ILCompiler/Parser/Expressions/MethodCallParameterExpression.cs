using System.Reflection;

namespace Parser.Parser.Expressions
{
    public class MethodCallParameterExpression : IExpression
    {
        public readonly ParameterInfo ParameterInfo;
        public ExpressionType ExpressionType { get; } = ExpressionType.MethodCallParameter;
        public readonly IExpression Expression;
        public CompilerType ReturnType { get; }

        public MethodCallParameterExpression(IExpression expression,ParameterInfo parameterInfo)
        {
            ParameterInfo = parameterInfo;
            Expression = expression;
            ReturnType = expression.ReturnType;
        }
    }
}