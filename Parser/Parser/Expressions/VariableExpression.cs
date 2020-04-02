namespace Parser
{
    public class VariableExpression : IExpression
    {
        public string Name;

        public VariableExpression(string name, CompilerType compilerType)
        {
            Name = name;
            ReturnType = compilerType;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Variable;
        public CompilerType ReturnType { get; }
    }
}