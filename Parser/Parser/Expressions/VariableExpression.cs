namespace Parser
{
    public class VariableExpression : IExpression
    {
        public string Name;

        public VariableExpression(string name, CompilerType compilerType, bool byReference = false)
        {
            Name = name;
            ReturnType = compilerType;
            ByReference = byReference;
        }

        public readonly bool ByReference;
        public ExpressionType ExpressionType { get; } = ExpressionType.Variable;
        public CompilerType ReturnType { get; }
    }
}