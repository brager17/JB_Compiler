using System;
using System.Collections.Generic;
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
        Logical,
        Return,
        IfElse
    }

    public interface IExpression
    {
        public ExpressionType ExpressionType { get; }
        public CompilerType ReturnType { get; }
    }

    public enum CompilerType
    {
        Int = 1,

        // UInt,
        Long,
        Bool,
    }

    public class Parser
    {
        private readonly IReadOnlyList<Token> _tokens;
        private readonly Dictionary<string, CompilerType> _closedFields;
        private readonly bool _constantFolding;
        private Dictionary<string, CompilerType> _parameters;
        private Dictionary<string, CompilerType> _localVariables = new Dictionary<string, CompilerType>();
        private TokenSequence _tokenSequence;
        public Parser(
            IReadOnlyList<Token> tokens,
            Dictionary<string, CompilerType> parameters = null,
            Dictionary<string, CompilerType> closedFields = null,
            bool constantFolding = true)
        {
            _tokens = tokens;
            _tokenSequence = new TokenSequence(_tokens);
            _parameters = parameters ?? new Dictionary<string, CompilerType>();
            _closedFields = closedFields ?? new Dictionary<string, CompilerType>();
            _constantFolding = constantFolding;
        }

        public IStatement[] Parse()
        {
            var list = new List<IStatement>();
            while (Index < _tokens.Count)
            {
                list.Add(Statement());
            }

            return list.ToArray();
        }

        public IExpression ParseExpression()
        {
            var expression = Expression();
            // todo можно улучшить, потому что не конкретизируем ошибку
            if (Index <= _tokens.Count - 1)
            {
                throw new Exception("Expression is incorrect");
            }

            return expression;
        }

        public bool CheckCannotImplicitConversion(VariableExpression left, IExpression right)
        {
            if (left.ReturnType == CompilerType.Long) return false;

            if (right.TryCast<PrimaryExpression>(out var primary) && primary.ReturnType == CompilerType.Long)
                return true;

            if (right.TryCast<UnaryExpression>(out var unaryExpression) &&
                unaryExpression.Expression.TryCast(out primary) && primary.ReturnType == CompilerType.Long)
                return true;

            if (right.TryCast<VariableExpression>(out var variable) && variable.ReturnType == CompilerType.Long)
                return true;


            return false;
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
                    TokenType.LongWord => CompilerType.Long,
                };

                if (_parameters.ContainsKey(Current.Value))
                {
                    throw new Exception(
                        $"A local or parameter named '{Current.Value}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter");
                }

                var variable = new VariableExpression(Current.Value, type);
                _localVariables.Add(variable.Name, variable.ReturnType);
                Index += 2; // variable name + assignment sign
                var expression = Expression();

                if (CheckCannotImplicitConversion(variable, expression))
                {
                    throw new Exception("Cannot implicitly convert type 'long ' to int");
                }

                if (Current?.Type != TokenType.Semicolon)
                {
                    throw new Exception("Statement must be ended by semicolon");
                }

                Index++;

                return new AssignmentStatement(variable, expression);
            }

            if (Current?.Type == TokenType.ReturnWord)
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

            if (Current?.Type == TokenType.IfWord)
            {
                Index++;
                if (Current.Type != TokenType.LeftParent)
                {
                    throw new Exception("Missing Left Parent");
                }

                Index++;
                var expression = Logical();

                if (Current.Type != TokenType.RightParent)
                {
                    throw new Exception("Missing Right Parent");
                }

                Index++;
                if (Current.Type != TokenType.LeftBrace)
                {
                    throw new Exception("Missing left brace");
                }

                Index++;
                var ifStatement = Statement();

                if (Current.Type != TokenType.RightBrace)
                {
                    throw new Exception("Missing right brace");
                }

                if (Index == _tokens.Count - 1 && _tokens[Index + 1]?.Type != TokenType.ElseWord)
                {
                    return new IfElseStatement((LogicalBinaryExpression) expression, ifStatement);
                }

                Index++;
                if (Current.Type != TokenType.LeftBrace)
                {
                    throw new Exception("Missing left brace");
                }

                Index++;
                var elseStatement = Statement();

                if (Current.Type != TokenType.RightBrace)
                {
                    throw new Exception("Missing right brace");
                }

                return new IfElseStatement((LogicalBinaryExpression) expression, ifStatement, elseStatement);
            }

            throw new Exception("дописать остальные виды statements");
        }

        public IExpression Logical()
        {
            var left = Expression();
            switch (Current.Type)
            {
                case TokenType.LessThan:
                    Index++;
                    return new LogicalBinaryExpression(left, Expression(), LogicalOperator.Less);
                case TokenType.LessThanOrEquals:
                    Index++;
                    return new LogicalBinaryExpression(left, Expression(), LogicalOperator.LessOrEq);
                case TokenType.GreaterThan:
                    Index++;
                    return new LogicalBinaryExpression(left, Expression(), LogicalOperator.Greater);
                case TokenType.GreaterThanOrEquals:
                    Index++;
                    return new LogicalBinaryExpression(left, Expression(), LogicalOperator.GreaterOrEq);
                case TokenType.EqualTo:
                    Index++;
                    return new LogicalBinaryExpression(left, Expression(), LogicalOperator.Eq);
                case TokenType.NotEqualTo:
                    Index++;
                    return new LogicalBinaryExpression(left, Expression(), LogicalOperator.NoEq);
                default:
                    return left;
            }
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


        private IExpression TryOperationWithCheckOverflow(
            string leftValue,
            CompilerType leftType,
            string rightValue,
            CompilerType rightType,
            TokenType operationType)
        {
            // it is not best way
            try
            {
                IExpression ReturnExpression(string num, CompilerType type)
                {
                    if (num[0] == '-')
                        return new UnaryExpression(new PrimaryExpression(num[1..], type));
                    return new PrimaryExpression(num, type);
                }

                checked
                {
                    var maxCompilerType = leftType < rightType ? rightType : leftType;

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
                throw new Exception("The operation is overflow in compile mode", ex);
            }
        }

        private IExpression ConstantFold(string leftValue,
            CompilerType leftType,
            string rightValue,
            CompilerType rightType,
            TokenType operationType)
        {
            return TryOperationWithCheckOverflow(leftValue, leftType, rightValue, rightType, operationType);
        }

        private bool TryFold(IExpression expression, out string value, out CompilerType compilerType)
        {
            value = default;
            compilerType = default;
            if (!_constantFolding) return false;
            if (expression.TryCast<PrimaryExpression>(out var primaryExpression))
            {
                if (primaryExpression.IsFoldedAfterMul0)
                    return false;
                value = primaryExpression.Value;
                compilerType = primaryExpression.ReturnType;
                return true;
            }

            if (expression.TryCast<UnaryExpression>(out var unary) &&
                unary.Expression.TryCast(out primaryExpression))
            {
                value = '-' + primaryExpression.Value;
                compilerType = primaryExpression.ReturnType;
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
                    else if (TryFold(result, out leftValue, out _) && leftValue == "1")
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
                    else if (TryFold(right, out rightValue, out _) && rightValue == "1")
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
                        if (rightValue == "0")
                            throw new DivideByZeroException();

                        result = ConstantFold(leftValue, leftType, rightValue, rightType, TokenType.Slash);
                    }
                    else if (TryFold(right, out rightValue, out _) && rightValue == "1")
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
                if (_tokens[Index + 1].Type == TokenType.Num)
                {
                    return new UnaryExpression(ParseNumber());
                }

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

        private IExpression ParseNumber()
        {
            if (Current.Type == TokenType.Minus)
            {
                Index++;
                var stringNumber = Current.Value;
                if (PrimaryExpression.GetPrimaryType('-' + Current.Value, out var compilerType))
                {
                    Index++;
                    return new PrimaryExpression(stringNumber, compilerType);
                }
            }
            else
            {
                var stringNumber = Current.Value;
                if (PrimaryExpression.GetPrimaryType(Current.Value, out var compilerType))
                {
                    Index++;
                    return new PrimaryExpression(stringNumber, compilerType);
                }
            }

            throw new Exception("Integral constant is too large");
        }

        public IExpression Primary()
        {
            if (Current.Type == TokenType.Num)
            {
                return ParseNumber();
            }

            if (Current.Type == TokenType.Num)
            {
                var stringNumber = Current.Value;
                if (PrimaryExpression.GetPrimaryType(Current.Value, out var compilerType))
                {
                    Index++;
                    return new PrimaryExpression(stringNumber, compilerType);
                }

                throw new Exception("Integral constant is too large");
            }

            if (Current?.Type == TokenType.Variable)
            {
                var varToken = Current;
                Index++;

                if (_parameters.TryGetValue(varToken.Value, out var variableType) ||
                    _closedFields.TryGetValue(varToken.Value, out variableType) ||
                    _localVariables.TryGetValue(varToken.Value, out variableType))
                    return new VariableExpression(varToken.Value, variableType);
            }

            if (Current?.Type == TokenType.LeftParent)
            {
                Index++;
                var expression = Expression();
                if (Current?.Type != TokenType.RightParent)
                {
                    throw new Exception("Count of opening brackets must be equals count of closing brackets");
                }

                Index++;
                return expression;
            }

            if (Current?.Type == TokenType.RightParent)
            {
                throw new Exception("Amount of opening brackets have to equals amount of closing brackets");
            }

            if (Current?.Type == TokenType.Word)
            {
                var methodName = Current.Value;
                Index++;
                if (Current.Type != TokenType.LeftParent)
                {
                    throw new Exception("Opening bracket must be after method name");
                }

                Index++;
                var @params = new List<IExpression>();
                while (Current?.Type != TokenType.RightParent)
                {
                    @params.Add(Expression());

                    if (Current.Type != TokenType.Comma && Current.Type != TokenType.RightParent)
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
                if (Constants.Dictionary.TryGetValue(Current.Value, out var item))
                {
                    Index++;
                    if (item.Item1[0] != '-') return new PrimaryExpression(item.Item1, item.Item2);
                    return new UnaryExpression(new PrimaryExpression(item.Item1[1..], item.Item2));
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