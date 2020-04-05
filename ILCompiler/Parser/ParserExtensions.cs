using System;
using Parser.Lexer;
using Parser.Parser.Exceptions;
using Parser.Parser.Expressions;
using Parser.Utils;

namespace Parser.Parser
{
    public static class ParserExtensions
    {
        internal static bool CheckCannotImplicitConversion(this VariableExpression left, IExpression right)
        {
            // check it : long l = int.MaxValue+1;  int i = l;
            if (left.ReturnType == CompilerType.Long) return false;

            if (right.TryCast<PrimaryExpression>(out var primary) && primary.ReturnType == CompilerType.Long)
                return true;

            if (right.TryCast<UnaryExpression>(out var unaryExpression) &&
                unaryExpression.Expression.TryCast(out primary) && primary.ReturnType == CompilerType.Long)
                return true;

            if (right.TryCast<LocalVariableExpression>(out var arg) && arg.ReturnType == CompilerType.Long)
                return true;

            if (right.TryCast<FieldVariableExpression>(out var field) && field.ReturnType == CompilerType.Long)
                return true;

            if (right.TryCast<MethodCallExpression>(out var call) && call.ReturnType == CompilerType.Long)
                return true;

            return false;
        }

        internal static IExpression TryOperationWithCheckOverflow(
            string leftValue,
            CompilerType leftType,
            string rightValue,
            CompilerType rightType,
            TokenType operationType)
        {
            try
            {
                IExpression ReturnExpression(string num, CompilerType type)
                {
                    if (num[0] == '-')
                        return new UnaryExpression(new PrimaryExpression(num[1..], type), UnaryType.Negative);
                    return new PrimaryExpression(num, type);
                }

                checked
                {
                    var maxCompilerType = leftType == CompilerType.Int && rightType == CompilerType.Int
                        ? CompilerType.Int
                        : CompilerType.Long;

                    switch (maxCompilerType)
                    {
                        case CompilerType.Int:
                            var iLeft = int.Parse(leftValue);
                            var iRight = int.Parse(rightValue);
                            int intResult = operationType switch
                            {
                                TokenType.Plus => (iLeft + iRight),
                                TokenType.Minus => (iLeft - iRight),
                                TokenType.Star => (iLeft * iRight),
                                TokenType.Slash => (iLeft / iRight),
                            };
                            return ReturnExpression(intResult.ToString(), CompilerType.Int);
                        case CompilerType.Long:
                            var lLeft = long.Parse(leftValue);
                            var lRight = long.Parse(rightValue);
                            long longResult = operationType switch
                            {
                                TokenType.Plus => (lLeft + lRight),
                                TokenType.Minus => (lLeft - lRight),
                                TokenType.Star => (lLeft * lRight),
                                TokenType.Slash => (lLeft / lRight),
                            };
                            return ReturnExpression(longResult.ToString(), CompilerType.Long);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CompileException("The operation is overflow in compile mode", ex);
            }
        }
    }
}