namespace Parser
{
    public enum LogicalOperator
    {
        // priority 2
        Less,
        LessOrEq,
        Greater,
        GreaterOrEq,

        //priority 1
        Eq,
        NoEq
    }

    public class LogicalBinaryExpression : IExpression
    {
        public readonly IExpression Left;
        public readonly IExpression Right;
        public readonly LogicalOperator Operator;

        public LogicalBinaryExpression(IExpression left, IExpression right, LogicalOperator @operator)
        {
            Left = left;
            Right = right;
            Operator = @operator;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Logical;
        public CompilerType ReturnType { get; } = CompilerType.Bool;
    }

    public class IfElseStatement : IStatement
    {
        public LogicalBinaryExpression Test;
        public readonly IStatement IfTrue;
        public readonly IStatement IfFalse;

        public IfElseStatement(LogicalBinaryExpression test, IStatement ifTrue, IStatement ifFalse)
        {
            Test = test;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }
        
        public IfElseStatement(LogicalBinaryExpression test, IStatement ifTrue)
        {
            Test = test;
            IfTrue = ifTrue;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.IfElse;
    }
}