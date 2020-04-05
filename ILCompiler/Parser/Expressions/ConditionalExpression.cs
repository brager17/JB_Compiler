using System;
using Parser.Parser.Statements;

namespace Parser.Parser.Expressions
{
    [Flags]
    public enum Operator
    {
        Less = 1,
        LessOrEq = 1 << 1,
        Greater = 1 << 2,
        GreaterOrEq = 1 << 3,
        Eq = 1 << 4,
        NoEq = 1 << 5,
        Or = 1 << 6,
        And = 1 << 7,

        Arithmetic = Less | LessOrEq | Greater | GreaterOrEq | Eq | NoEq,
        Logical = Or | And
    }

    public class LogicalBinaryExpression : IExpression
    {
        public readonly IExpression Left;
        public readonly IExpression Right;
        public readonly Operator Operator;

        public LogicalBinaryExpression(IExpression left, IExpression right, Operator @operator)
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
        public IExpression Test;
        public readonly Statement IfTrue;
        public readonly Statement Else;

        public IfElseStatement(IExpression test, Statement ifTrue, Statement @else)
        {
            Test = test;
            IfTrue = ifTrue;
            Else = @else;
        }

        public IfElseStatement(IExpression test, Statement ifTrue)
        {
            Test = test;
            IfTrue = ifTrue;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.IfElse;
    }
}