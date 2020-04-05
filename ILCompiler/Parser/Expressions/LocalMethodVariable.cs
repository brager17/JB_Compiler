namespace Parser.Parser.Expressions
{
    public class LocalVariableExpression : VariableExpression
    {
        public LocalVariableExpression(string name, CompilerType compilerType, int index, bool byReference = false) :
            base(
                name, compilerType, byReference, ExpressionType.LocalVariable)
        {
            Index = index;
        }

        public readonly int Index;
    }
}