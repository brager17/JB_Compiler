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

    public class PrimaryExpression : IExpression
    {
        public static PrimaryExpression FoldedAfterMul0 = new PrimaryExpression(0) {IsFoldedAfterMul0 = true};

        public PrimaryExpression(long value)
        {
            Value = value;
        }

        public readonly long Value;
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
                    if (TryFold(result, out var leftValue) &&
                        TryFold(right, out var rightValue))
                    {
                        result = ConstantFold(leftValue, rightValue, TokenType.Plus);
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
                    if (TryFold(result, out var leftValue) &&
                        TryFold(right, out var rightValue))
                    {
                        result = ConstantFold(leftValue, rightValue, TokenType.Minus);
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

        private bool CheckOverflow(long left, long right, TokenType type)
        {
            if (type == TokenType.Plus)
            {
                if ((left > int.MaxValue || right > int.MaxValue) && right + left < 0)
                    throw new Exception("The operation overflows at compile time in checked mode");

                if ((left < int.MinValue || right < int.MinValue) && right + left > 0)
                    throw new Exception("The operation overflows at compile time in checked mode");

                if (left <= int.MaxValue && right <= int.MaxValue && left + right > int.MaxValue)
                    throw new Exception("The operation overflows at compile time in checked mode");
            }
            else if (type == TokenType.Minus)
            {
            }
            else if (type == TokenType.Star)
            {
            }

            return true;
        }

        private IExpression ConstantFold(long left, long right, TokenType type)
        {
            // todo check overflow
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

            return result >= 0
                ? (IExpression) new PrimaryExpression(result)
                : new UnaryExpression(new PrimaryExpression(-result));
        }

        private bool TryFold(IExpression expression, out long value)
        {
            value = default;
            if (!_constantFolding) return false;
            if (expression.TryCast<PrimaryExpression>(out var primaryExpression))
            {
                if (primaryExpression.IsFoldedAfterMul0)
                    return false;

                value = primaryExpression.Value;
                return true;
            }

            if (expression.TryCast<UnaryExpression>(out var unary) &&
                unary.Expression.TryCast(out primaryExpression))
            {
                value = -primaryExpression.Value;
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
                    if (TryFold(result, out var leftValue) && TryFold(right, out var rightValue))
                    {
                        // (3*4) = 12; 
                        result = ConstantFold(leftValue, rightValue, TokenType.Star);
                    }
                    else if (TryFold(result, out leftValue) && leftValue == 1)
                    {
                        // 1*(x+y-12) = x+y-12; 
                        result = right;
                    }
                    else if (TryFold(result, out leftValue) && leftValue == 0)
                    {
                        //0*(x+y-12) = 0
                        result = PrimaryExpression.FoldedAfterMul0;
                    }
                    else if (TryFold(right, out rightValue) && rightValue == 0)
                    {
                        //(x+y-12)*0 = 0
                        result = PrimaryExpression.FoldedAfterMul0;
                    }
                    else if (TryFold(right, out rightValue) && rightValue == 1)
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
                    if (TryFold(result, out var leftValue) && TryFold(right, out var rightValue))
                    {
                        //12/0 
                        if (rightValue == 0)
                            throw new DivideByZeroException();

                        result = ConstantFold(leftValue, rightValue, TokenType.Slash);
                    }
                    else if (TryFold(right, out rightValue) && rightValue == 1)
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
                Index++;
                return new PrimaryExpression(value);
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


                var @params = new List<IExpression>();
                while (Current?.Type != TokenType.ClosingBracket)
                {
                    Index++;
                    @params.Add(Expression());

                    if (Current.Type != TokenType.Comma && Current.Type != TokenType.ClosingBracket)
                    {
                        throw new Exception("Method parameters must be separated by comma");
                    }
                }

                if (Current == null) throw new Exception("Method must end with the closing bracket");
                Index++;
                return new MethodCallExpression(methodName, @params);
            }

            throw new ArgumentException();
        }


        private Token Current => Index < _tokens.Count ? _tokens[Index] : null;
        private int Index = 0;
        private Token Next => _tokens[Index + 1];
    }
}