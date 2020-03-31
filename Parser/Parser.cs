using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Compiler;

namespace Parser
{
    public static class Constants
    {
        public static Dictionary<string, long> Dictionary = new Dictionary<string, long>()
        {
            {"int.MaxValue", int.MaxValue},
            {"int.MinValue", int.MinValue},
            {"long.MaxValue", long.MaxValue},
            {"long.MinValue", long.MinValue},
        };
    }

    public enum ExpressionType
    {
        Variable,
        Primary,
        Binary,
        Unary,
        MethodCall
    }

    public interface IExpression
    {
        public ExpressionType ExpressionType { get; }
    }

    public class VariableExpression : IExpression
    {
        public string Name;

        public VariableExpression(string name)
        {
            Name = name;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Variable;
    }

    public enum PrimaryType
    {
        Long = 1,
        Int
    }

    public class PrimaryExpression : IExpression
    {
        public static PrimaryExpression FoldedAfterMul0 = new PrimaryExpression("0") {IsFoldedAfterMul0 = true};

        public static PrimaryType GetPrimaryType(string number)
        {
            var l = long.Parse(number);
            return GetPrimaryType(l);
        }

        public static PrimaryType GetPrimaryType(long number)
        {
            return number >= int.MinValue && number <= int.MaxValue ? PrimaryType.Int : PrimaryType.Long;
        }

        public PrimaryExpression(string value, PrimaryType primaryType)
        {
            Value = value;
            PrimaryType = primaryType;
        }

        public PrimaryExpression(string value)
        {
            Value = value;
            PrimaryType = GetPrimaryType(value);
        }

        public long LongValue => long.Parse(Value);

        public readonly string Value;
        public readonly PrimaryType PrimaryType;
        public ExpressionType ExpressionType { get; } = ExpressionType.Primary;

        // (x*12*14)*0 = 0; needs for example : (1/0*x) - no divide by null compile time exception, (1/0) - divide by null compile time exception
        public bool IsFoldedAfterMul0;
    }

    public class BinaryExpression : IExpression
    {
        public IExpression Left;
        public IExpression Right;
        public TokenType TokenType;

        public BinaryExpression(IExpression left, IExpression right, TokenType tokenType)
        {
            Left = left;
            Right = right;
            TokenType = tokenType;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Binary;
    }

    public class MethodCallExpression : IExpression
    {
        public string Name;

        public IReadOnlyList<IExpression> Parameters;

        public MethodCallExpression(string name, IReadOnlyList<IExpression> parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.MethodCall;
    }

    public enum UnaryType
    {
        Positive,
        Negative
    }

    public class UnaryExpression : IExpression
    {
        public readonly IExpression Expression;
        public readonly UnaryType UnaryType;

        public UnaryExpression(IExpression expression)
        {
            Expression = expression;
            UnaryType = UnaryType.Negative;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Unary;
    }

    public class Parser
    {
        private readonly IReadOnlyList<Token> _tokens;
        private readonly bool _constantFolding;
        private Expression _expression;

        public Parser(IReadOnlyList<Token> tokens, bool constantFolding = true)
        {
            _tokens = tokens;
            _constantFolding = constantFolding;
        }

        public IExpression[] Parse()
        {
            List<IExpression> list = new List<IExpression>();
            while (Index < _tokens.Count)
            {
                list.Add(Expression());
            }

            return list.ToArray();
        }

        public IExpression Expression()
        {
            var expression = Additive();
            return expression;
        }


        public IExpression Additive()
        {
            var result = Multiplicative();
            // x + y + (x*y+x) = (x+y)+(x*y+x), not x+(y+(x*y+x)), it is important, because c# works this way
            while (true)
            {
                if (Current?.Type == TokenType.Plus)
                {
                    Index++;
                    var right = Multiplicative();
                    if (TryFold(result, out var leftValue, out var leftType) &&
                        TryFold(right, out var rightValue, out var rightType))
                    {
                        result = ConstantFold(leftValue, leftType, rightValue, rightType, TokenType.Plus);
                    }
                    else
                    {
                        result = new BinaryExpression(result, right, TokenType.Plus);
                    }

                    continue;
                }

                if (Current?.Type == TokenType.Minus)
                {
                    Index++;
                    var right = Multiplicative();
                    if (TryFold(result, out var leftValue, out var leftType) &&
                        TryFold(right, out var rightValue, out var rightType))
                    {
                        result = ConstantFold(leftValue, leftType, rightValue, rightType, TokenType.Minus);
                    }
                    else
                    {
                        result = new BinaryExpression(result, right, TokenType.Minus);
                    }

                    continue;
                }

                break;
            }

            return result;
        }


        private void CheckOverflow(long left, PrimaryType leftType, long right, PrimaryType rightType, TokenType type)
        {
            // it is not best way
            checked
            {
                if (leftType == PrimaryType.Int && rightType == PrimaryType.Int)
                {
                    var iLeft = (int) left;
                    var iRight = (int) right;
                    switch (type)
                    {
                        case TokenType.Plus:
                            _ = iLeft + iRight;
                            break;
                        case TokenType.Minus:
                            _ = iLeft - iRight;
                            break;
                        case TokenType.Star:
                            _ = iLeft * iRight;
                            break;
                    }
                }

                switch (type)
                {
                    case TokenType.Plus:
                        _ = left + right;
                        break;
                    case TokenType.Minus:
                        _ = left - right;
                        break;
                    case TokenType.Star:
                        _ = left * right;
                        break;
                }
            }


            //
            // if (type == TokenType.Plus)
            // {
            //     if (IsInt(left) && IsInt(right))
            //     {
            //         int iLeft = (int) left;
            //         int iRight = (int) right;
            //
            //         if (iLeft > 0 && iRight > 0 && iLeft + iRight < 0)
            //             return true;
            //         if (iLeft < 0 && iRight < 0 && iLeft + iRight > 0)
            //             return true;
            //     }
            //     else
            //     {
            //         if (left > 0 && right > 0 && left + right < 0)
            //             return true;
            //         if (left < 0 && right < 0 && left + right > 0)
            //             return true;
            //     }
            // }
            // else if (type == TokenType.Minus)
            // {
            //     if (IsInt(left) && IsInt(right))
            //     {
            //         int iLeft = (int) left;
            //         int iRight = (int) right;
            //
            //         if (iLeft < 0 && iRight > 0 && iLeft - iRight > 0)
            //             return true;
            //         if (iLeft > 0 && iRight < 0 && iLeft - iRight < 0)
            //             return true;
            //     }
            //
            //     else if (left < 0 && right > 0 && left - right > 0)
            //         return true;
            //     else if (left > 0 && left < 0 && left - left < 0)
            //         return true;
            // }
            // else if (type == TokenType.Star)
            // {
            //     if (IsInt(left) && IsInt(right))
            //     {
            //         int iLeft = (int) left;
            //         int iRight = (int) right;
            //
            //         if (iLeft < 0 && iRight < 0 && iLeft * iRight < 0) return true;
            //         if (iLeft > 0 && iRight < 0 && iLeft * iRight > 0) return true;
            //         if (iLeft < 0 && iRight > 0 && iLeft * iRight > 0) return true;
            //         if (iLeft > 0 && iRight > 0 && iLeft * iRight < 0) return true;
            //     }
            //
            //     else
            //     {
            //         if (left < 0 && right < 0 && left * right < 0) return true;
            //         if (left > 0 && right < 0 && left * right > 0) return true;
            //         if (left < 0 && right > 0 && left * right > 0) return true;
            //         if (left > 0 && right > 0 && left * right < 0) return true;
            //     }
            // }
            //
            // return false;
        }

        private IExpression ConstantFold(long left, PrimaryType leftType, long right, PrimaryType rightType,
            TokenType type)
        {
            CheckOverflow(left, leftType, right, rightType, type);
            long result;
            switch (type)
            {
                case TokenType.Plus:
                    result = left + right;
                    break;
                case TokenType.Minus:
                    result = left - right;
                    break;
                case TokenType.Star:
                    result = left * right;
                    break;
                case TokenType.Slash:
                    result = left / right;
                    break;
                default: throw new Exception();
            }

            var primaryType = leftType == PrimaryType.Int && rightType == PrimaryType.Int
                ? PrimaryType.Int
                : PrimaryType.Long;

            return result >= 0
                ? (IExpression) new PrimaryExpression(result.ToString(), primaryType)
                : new UnaryExpression(new PrimaryExpression((-result).ToString(), primaryType));
        }

        private bool TryFold(IExpression expression, out long value, out PrimaryType primaryType)
        {
            value = default;
            primaryType = default;
            if (!_constantFolding) return false;
            if (expression.TryCast<PrimaryExpression>(out var primaryExpression))
            {
                if (primaryExpression.IsFoldedAfterMul0)
                    return false;

                value = primaryExpression.LongValue;
                primaryType = primaryExpression.PrimaryType;
                return true;
            }

            if (expression.TryCast<UnaryExpression>(out var unary) &&
                unary.Expression.TryCast(out primaryExpression))
            {
                value = -primaryExpression.LongValue;
                primaryType = primaryExpression.PrimaryType;
                return true;
            }

            return false;
        }


        public IExpression Multiplicative()
        {
            var result = Unary();
            // x*y*z = ((x*y)*z) not (x*(y*z)) , it is important, because c# works this way
            while (true)
            {
                if (Current?.Type == TokenType.Star)
                {
                    Index++;
                    var right = Unary();
                    if (TryFold(result, out var leftValue, out var leftType) &&
                        TryFold(right, out var rightValue, out var rightType))
                    {
                        // (3*4) = 12; 
                        result = ConstantFold(leftValue, leftType, rightValue, rightType, TokenType.Star);
                    }
                    else if (TryFold(result, out leftValue, out _) && leftValue == 1)
                    {
                        // 1*(x+y-12) = x+y-12; 
                        result = right;
                    }
                    // see notes.txt, нужно сделать также как компилятор, будет возможно после реализации statements
                    // else if (TryFold(result, out leftValue) && leftValue == 0)
                    // {
                    // 0*(x+y-12) = 0
                    // result = PrimaryExpression.FoldedAfterMul0;
                    // }
                    // else if (TryFold(right, out rightValue) && rightValue == 0)
                    // {
                    // (x+y-12)*0 = 0
                    // result = PrimaryExpression.FoldedAfterMul0;
                    // }
                    else if (TryFold(right, out rightValue, out _) && rightValue == 1)
                    {
                        // (x+y-12)*1 = x+y-12; 
                        result = result;
                    }
                    else
                    {
                        result = new BinaryExpression(result, right, TokenType.Star);
                    }

                    continue;
                }

                if (Current?.Type == TokenType.Slash)
                {
                    Index++;
                    var right = Unary();
                    if (TryFold(result, out var leftValue, out var leftType) &&
                        TryFold(right, out var rightValue, out var rightType))
                    {
                        //12/0 
                        if (rightValue == 0)
                            throw new DivideByZeroException();

                        result = ConstantFold(leftValue, leftType, rightValue, rightType, TokenType.Slash);
                    }
                    else if (TryFold(right, out rightValue, out _) && rightValue == 1)
                    {
                        // (x+y-12)/1 = (x+y-12)
                        result = result;
                    }
                    // x/0 = x/0, no optimization
                    // else if (TryFold(right, out rightValue) && rightValue == 0)
                    // {
                    //     //(x+y-12)/0 
                    //     throw new DivideByZeroException();
                    // }
                    else result = new BinaryExpression(result, right, TokenType.Slash);

                    continue;
                }

                break;
            }

            return result;
        }

        public IExpression Unary()
        {
            if (Current.Type == TokenType.Minus)
            {
                Index++;
                var expression = Primary();
                if (_constantFolding && expression.TryCast<UnaryExpression>(out var unary) &&
                    unary.UnaryType == UnaryType.Negative &&
                    unary.Expression.TryCast<PrimaryExpression>(out var primary))
                {
                    return primary;
                }

                return new UnaryExpression(expression);
            }

            return Primary();
        }

        public IExpression Primary()
        {
            if (long.TryParse(Current.Value, out var value))
            {
                var stringValue = Current.Value;
                Index++;
                return new PrimaryExpression(stringValue);
            }

            if (Current?.Type == TokenType.Variable)
            {
                var varToken = Current;
                Index++;
                return new VariableExpression(varToken.Value);
            }

            if (Current?.Type == TokenType.OpeningBracket)
            {
                Index++;
                var expression = Expression();
                if (Current?.Type != TokenType.ClosingBracket)
                {
                    throw new Exception("Count of opening brackets must be equals count of closing brackets");
                }

                Index++;
                return expression;
            }

            if (Current?.Type == TokenType.ClosingBracket)
            {
                throw new Exception("Amount of opening brackets have to equals amount of closing brackets");
            }

            if (Current?.Type == TokenType.Word)
            {
                var methodName = Current.Value;
                Index++;
                if (Current.Type != TokenType.OpeningBracket)
                {
                    throw new Exception("Opening bracket must be after method name");
                }

                Index++;
                var @params = new List<IExpression>();
                while (Current?.Type != TokenType.ClosingBracket)
                {
                    @params.Add(Expression());

                    if (Current.Type != TokenType.Comma && Current.Type != TokenType.ClosingBracket)
                    {
                        throw new Exception("Method parameters must be separated by comma");
                    }

                    if (Current.Type == TokenType.Comma)
                    {
                        Index++;
                    }
                }


                if (Current == null) throw new Exception("Method must end with the closing bracket");
                Index++;
                return new MethodCallExpression(methodName, @params);
            }

            if (Current?.Type == TokenType.Constant)
            {
                if (Constants.Dictionary.TryGetValue(Current.Value, out var constantValue))
                {
                    Index++;
                    var primaryType = PrimaryExpression.GetPrimaryType(constantValue);
                    if (constantValue >= 0) return new PrimaryExpression(constantValue.ToString(), primaryType);
                    return new UnaryExpression(new PrimaryExpression((-constantValue).ToString(), primaryType));
                }

                throw new Exception("Unknown constant");
            }

            throw new ArgumentException();
        }


        private Token Current => Index < _tokens.Count ? _tokens[Index] : null;
        private int Index = 0;
        private Token Next => _tokens[Index + 1];
    }
}