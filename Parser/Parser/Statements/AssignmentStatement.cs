namespace Parser
{
    public interface IStatement
    {
        public ExpressionType ExpressionType { get; }
    }

    public class AssignmentStatement : IStatement
    {
        public AssignmentStatement(VariableExpression left, IExpression right)
        {
            Left = left;
            Right = right;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Assignment;


        public readonly VariableExpression Left;
        public readonly IExpression Right;
    }
}