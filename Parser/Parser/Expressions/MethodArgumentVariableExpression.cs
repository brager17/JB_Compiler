namespace Parser
{
    public class MethodArgumentVariableExpression : VariableExpression
    {
        public MethodArgumentVariableExpression(string name, CompilerType compilerType, int index,
            bool byReference = false) :
            base(name, compilerType, byReference, ExpressionType.MethodArgVariable)
        {
            Index = index;
        }

        public readonly int Index;
    }
}