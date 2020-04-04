using System;
using System.Reflection.Emit;
using Parser;

namespace Compiler
{
    public partial class CompileExpressionVisitor
    {
        public void Visit(LogicalBinaryExpression e, Label ifTrue, Label ifFalse, bool isNeedReview)
        {
            if (e.Left.ExpressionType != ExpressionType.Logical && e.Right.ExpressionType != ExpressionType.Logical)
            {
                VisitExpression(e.Left);
                VisitExpression(e.Right);
                if (isNeedReview)
                {
                    _ilGenerator.Emit(OpCodesDic[Revert[e.Operator]], ifFalse);
                }

                else
                {
                    _ilGenerator.Emit(OpCodesDic[e.Operator], ifTrue);
                }
            }

            // (x == 1 and x == 2) or (x != 3) = (x!=1 or x!=2) and (x==3) = 
            else
            {
                var left = (LogicalBinaryExpression) e.Left;
                var right = (LogicalBinaryExpression) e.Right;
                switch (e.Operator)
                {
                    case LogicalOperator.And:
                        if (e.Operator == LogicalOperator.Or)
                        {
                            ifFalse = _ilGenerator.DefineLabel();
                        }
                        Visit(left, ifTrue, ifFalse, true);
                        if (e.Operator == LogicalOperator.Or)
                        {
                            _ilGenerator.MarkLabel(ifFalse);
                        }
                        Visit(right, ifTrue, ifFalse, isNeedReview);
                        break;
                    case LogicalOperator.Or:
                        
                        Visit(left, ifTrue, ifFalse, false);
                        Visit(right, ifTrue, ifFalse, isNeedReview);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}