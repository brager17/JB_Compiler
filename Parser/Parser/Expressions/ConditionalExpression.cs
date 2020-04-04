namespace Parser
{
    public enum LogicalOperator
    {
        Less,
        Eq,
        And,
        LessOrEq,
        Greater,
        GreaterOrEq,

        NoEq,
        
        Or
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
        public readonly Statement IfTrue;
        public readonly Statement Else;

        public IfElseStatement(LogicalBinaryExpression test, Statement ifTrue, Statement @else)
        {
            Test = test;
            IfTrue = ifTrue;
            Else = @else;
        }
        
        public IfElseStatement(LogicalBinaryExpression test, Statement ifTrue)
        {
            Test = test;
            IfTrue = ifTrue;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.IfElse;
    }
}