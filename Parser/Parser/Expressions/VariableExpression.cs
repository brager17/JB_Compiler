namespace Parser
{
    public class VariableExpression : IExpression
    {
        public string Name;
        public readonly CompilerType CompilerType;

        public VariableExpression(string name, CompilerType compilerType)
        {
            Name = name;
            CompilerType = compilerType;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Variable;
    }
}