using System.Reflection;

namespace Parser.Parser.Expressions
{
    public class MethodCallParameterExpression : IExpression
    {
        public readonly string Name;

        public readonly ParameterInfo ParameterInfo;
        public ExpressionType ExpressionType { get; } = ExpressionType.MethodCallParameter;
        public CompilerType ReturnType { get; }

        public MethodCallParameterExpression(string name, ParameterInfo parameterInfo, CompilerType returnType)
        {
            Name = name;
            ParameterInfo = parameterInfo;
            ReturnType = returnType;
        }
    }
}