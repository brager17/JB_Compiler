using System.Collections.Generic;
using System.Reflection;

namespace Parser.Parser.Expressions
{
    public class MethodCallExpression : IExpression
    {
        public string Name;

        public IReadOnlyList<MethodCallParameterExpression> Parameters;

        public MethodCallExpression(string name, MethodInfo methodInfo,IReadOnlyList<MethodCallParameterExpression> parameters)
        {
            Name = name;
            MethodInfo = methodInfo;
            Parameters = parameters;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.MethodCallExpression;

        public readonly MethodInfo MethodInfo;
        public CompilerType ReturnType { get; } = CompilerType.Long;
    }
}