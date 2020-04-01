using System.Collections.Generic;

namespace Parser
{
    public class MethodCallExpression : IExpression
    {
        public string Name;

        public IReadOnlyList<IExpression> Parameters;

        public MethodCallExpression(string name, IReadOnlyList<IExpression> parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.MethodCall;
    }
}