using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Compiler;

namespace Parser
{
    public enum ExpressionType
    {
        Variable = 1,
        Primary,
        Binary,
        Unary,
        MethodCall,
        Assignment,
        Return
    }

    public interface IExpression
    {
        public ExpressionType ExpressionType { get; }
    }

    public enum CompilerType
    {
        Long = 1,
        Int
    }

    public class Parser
    {
        private readonly IReadOnlyList<Token> _tokens;
        private readonly bool _constantFolding;
        private Expression _expression;
        private Dictionary<string, CompilerType> _variables = new Dictionary<string, CompilerType>();

        public Parser(
            IReadOnlyList<Token> tokens,
            Dictionary<string, CompilerType> variables = null,
            bool constantFolding = true)
        {
            _tokens = tokens;
            _variables = variables ?? new Dictionary<string, CompilerType>();
            _constantFolding = constantFolding;
        }

        public IStatement[] Parse()
        {
            List<IStatement> list = new List<IStatement>();
            while (Index < _tokens.Count)
            {
                list.Add(Statement());
            }

            return list.ToArray();
        }

        public IExpression ParseExpression()
        {
            return Expression();
        }


        public IStatement Statement()
        {
            if ((Current?.Type == TokenType.IntWord || Current?.Type == TokenType.LongWord) &&
                _tokens[Index + 2].Type == TokenType.Assignment)
            {
                var keywordType = Current.Type;
                Index++;

                var type = keywordType switch
                {
                    TokenType.IntWord => CompilerType.Int,
                    TokenType.LongWord => CompilerType.Long
                };

                var variable = new VariableExpression(Current.Value, type);
                _variables.Add(variable.Name, variable.CompilerType);
                Index += 2; // variable name + assignment sign
                var expression = Expression();

                if (Current?.Type != TokenType.Semicolon)
                {
                    throw new Exception("Statement must be ended by semicolon");
                }

                Index++;

                return new AssignmentStatement(variable, expression);
            }

            if (Current?.Type == TokenType.Return)
            {
                Index++;
                var expression = Expression();
                if (Current?.Type != TokenType.Semicolon)
                {
                    throw new Exception("Return must be ended by semicolon");
                }
                Index++;
                return new ReturnStatement(expression);
            }

            throw new Exception("дописать остальные виды statements");
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


        private void CheckOverflow(long left, CompilerType leftCompilerType, long right, CompilerType rightCompilerType,
            TokenType type)
        {
            // it is not best way
            checked
            {
                if (leftCompilerType == CompilerType.Int && rightCompilerType == CompilerType.Int)
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

        private IExpression ConstantFold(long left, CompilerType leftCompilerType, long right,
            CompilerType rightCompilerType,
            TokenType type)
        {
            CheckOverflow(left, leftCompilerType, right, rightCompilerType, type);
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

            var primaryType = leftCompilerType == CompilerType.Int && rightCompilerType == CompilerType.Int
                ? CompilerType.Int
                : CompilerType.Long;

            return result >= 0
                ? (IExpression) new PrimaryExpression(result.ToString(), primaryType)
                : new UnaryExpression(new PrimaryExpression((-result).ToString(), primaryType));
        }

        private bool TryFold(IExpression expression, out long value, out CompilerType compilerType)
        {
            value = default;
            compilerType = default;
            if (!_constantFolding) return false;
            if (expression.TryCast<PrimaryExpression>(out var primaryExpression))
            {
                if (primaryExpression.IsFoldedAfterMul0)
                    return false;

                value = primaryExpression.LongValue;
                compilerType = primaryExpression.CompilerType;
                return true;
            }

            if (expression.TryCast<UnaryExpression>(out var unary) &&
                unary.Expression.TryCast(out primaryExpression))
            {
                value = -primaryExpression.LongValue;
                compilerType = primaryExpression.CompilerType;
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

                if (_variables.TryGetValue(varToken.Value, out var variableType))
                    return new VariableExpression(varToken.Value, variableType);
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