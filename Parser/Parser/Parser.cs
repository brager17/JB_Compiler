using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Compiler;

namespace Parser
{
    // todo разделить для Expression'ов и для Statement'ов
    public enum ExpressionType
    {
        Variable = 1,
        Primary,
        Binary,
        Unary,
        MethodCallExpression,
        VoidMethodCallStatement,
        Assignment,
        Logical,
        Return,
        IfElse,
        Statement
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
            _tokenSequence = new TokenSequence(tokens);
            _parameters = parameters ?? new Dictionary<string, CompilerType>();
            _closedFields = closedFields ?? new Dictionary<string, CompilerType>();
            _constantFolding = constantFolding;
        }

        public IStatement[] Parse()
        {
            var list = new List<IStatement>();
            while (!_tokenSequence.IsEmpty)
            {
                list.Add(Statement());
            }

            return list.ToArray();
        }

        public IExpression ParseExpression()
        {
            var expression = Expression();
            // todo можно улучшить, потому что не конкретизируем ошибку
            if (!_tokenSequence.IsEmpty)
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

        private IStatement AssignmentStatement()
        {
            VariableExpression variableExpression = null;
            if (_tokenSequence.Current?.Type == TokenType.IntWord || _tokenSequence.Current?.Type == TokenType.LongWord)
            {
                var keywordType = _tokenSequence.Current.Type;
                _tokenSequence.Step();

                var type = keywordType switch
                {
                    TokenType.IntWord => CompilerType.Int,
                    TokenType.LongWord => CompilerType.Long,
                };

                if (_localVariables.TryGetValue(_tokenSequence.Current.Value, out _))
                {
                    throw new Exception($"Variable with name '{_tokenSequence.Current.Value}' is already declared");
                }

                if (_parameters.ContainsKey(_tokenSequence.Current.Value))
                {
                    throw new Exception(
                        $"A local or parameter named '{_tokenSequence.Current.Value}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter");
                }

                variableExpression = new VariableExpression(_tokenSequence.Current.Value, type);
                _localVariables.Add(variableExpression.Name, variableExpression.ReturnType);
                _tokenSequence.Step(); // variable name
            }
            else if (_tokenSequence.Current?.Type == TokenType.Variable)
            {
                if (_localVariables.TryGetValue(_tokenSequence.Current.Value, out var compilerType))
                {
                    variableExpression = new VariableExpression(_tokenSequence.Current.Value, compilerType);
                    _tokenSequence.Step(); // assignment sign
                }
                else
                {
                    throw new Exception($"Variable with name '{_tokenSequence.Current.Value}' is not declared");
                }
            }

            _tokenSequence.Step(); // assignment sign
            var expression = Expression();

            if (CheckCannotImplicitConversion(variableExpression, expression))
            {
                throw new Exception("Cannot implicitly convert type 'long ' to int");
            }

            if (_tokenSequence.Current?.Type != TokenType.Semicolon)
            {
                throw new Exception("Statement must be ended by semicolon");
            }

            _tokenSequence.Step();

            return new AssignmentStatement(variableExpression, expression);
        }

        public IStatement Statement()
        {
            if (_tokenSequence.Get(2) == Token.Assignment || _tokenSequence.Get(1) == Token.Assignment)
            {
                return AssignmentStatement();
            }

            if (_tokenSequence.Current?.Type == TokenType.ReturnWord)
            {
                _tokenSequence.Step();
                var expression = Expression();
                if (_tokenSequence.Current?.Type != TokenType.Semicolon)
                {
                    throw new Exception("Return must be ended by semicolon");
                }

                _tokenSequence.Step();
                return new ReturnStatement(expression);
            }

            if (_tokenSequence.Current?.Type == TokenType.IfWord)
            {
                return IfElseStatement();
            }

            // todo: использовать другую проверку, что это метод
            if (_tokenSequence.Current?.Type == TokenType.Word && _tokenSequence.Next.Type == TokenType.LeftParent)
            {
                var methodCall = MethodCallExpression();
                if (_tokenSequence.Current.Type != TokenType.Semicolon)
                {
                    throw new Exception("Missing semicolon");
                }

                _tokenSequence.Step();
                return new VoidMethodCallStatement(methodCall);
            }

            throw new Exception("дописать остальные виды statements");
        }

        public IfElseStatement IfElseStatement()
        {
            _tokenSequence.Step();
            if (_tokenSequence.Current.Type != TokenType.LeftParent)
            {
                throw new Exception("Missing Left Parent");
            }

            _tokenSequence.Step();
            var expression = Expression();

            if (_tokenSequence.Current.Type != TokenType.RightParent)
            {
                throw new Exception("Missing Right Parent");
            }

            _tokenSequence.Step();
            if (_tokenSequence.Current.Type != TokenType.LeftBrace)
            {
                throw new Exception("Missing left brace");
            }

            _tokenSequence.Step();


            var ifStatements = new List<IStatement>();
            while (_tokenSequence.Current.Type != TokenType.RightBrace)
            {
                ifStatements.Add(Statement());
            }


            if (_tokenSequence.Next?.Type != TokenType.ElseWord)
            {
                _tokenSequence.Step();
                return new IfElseStatement((LogicalBinaryExpression) expression,
                    new Statement(ifStatements.ToArray()));
            }

            _tokenSequence.Step();
            _tokenSequence.Step();
            if (_tokenSequence.Current.Type != TokenType.LeftBrace)
            {
                throw new Exception("Missing left brace");
            }

            _tokenSequence.Step();
            var elseStatements = new List<IStatement>();
            while (_tokenSequence.Current.Type != TokenType.RightBrace)
            {
                elseStatements.Add(Statement());
            }

            if (_tokenSequence.Current.Type != TokenType.RightBrace)
            {
                throw new Exception("Missing right brace");
            }

            _tokenSequence.Step();

            return new IfElseStatement((LogicalBinaryExpression) expression,
                new Statement(ifStatements.ToArray()),
                new Statement(elseStatements.ToArray()));
        }

        public IExpression Expression()
        {
            var expression = OrLogical();
            return expression;
        }


        public IExpression OrLogical()
        {
            var result = AndLogical();
            while (_tokenSequence.Current?.Type == TokenType.Or)
            {
                _tokenSequence.Step();
                result = new LogicalBinaryExpression(result, AndLogical(), LogicalOperator.Or);
            }

            return result;
        }

        public IExpression AndLogical()
        {
            var result = EqualsNoEquals();
            while (_tokenSequence.Current?.Type == TokenType.And)
            {
                _tokenSequence.Step();
                result = new LogicalBinaryExpression(result, EqualsNoEquals(), LogicalOperator.And);
            }

            return result;
        }

        public IExpression EqualsNoEquals()
        {
            var result = Logical();
            while (_tokenSequence.Current?.Type == TokenType.EqualTo ||
                   _tokenSequence.Current?.Type == TokenType.NotEqualTo)
            {
                switch (_tokenSequence.Current?.Type)
                {
                    case TokenType.EqualTo:
                        _tokenSequence.Step();
                        result = new LogicalBinaryExpression(result, Logical(), LogicalOperator.Eq);
                        break;
                    case TokenType.NotEqualTo:
                        _tokenSequence.Step();
                        result = new LogicalBinaryExpression(result, Logical(), LogicalOperator.NoEq);
                        break;
                }
            }

            return result;
        }

        public IExpression Logical()
        {
            var left = Additive();
            switch (_tokenSequence.Current?.Type)
            {
                case TokenType.LessThan:
                    _tokenSequence.Step();
                    return new LogicalBinaryExpression(left, Additive(), LogicalOperator.Less);
                case TokenType.LessThanOrEquals:
                    _tokenSequence.Step();
                    return new LogicalBinaryExpression(left, Additive(), LogicalOperator.LessOrEq);
                case TokenType.GreaterThan:
                    _tokenSequence.Step();
                    return new LogicalBinaryExpression(left, Additive(), LogicalOperator.Greater);
                case TokenType.GreaterThanOrEquals:
                    _tokenSequence.Step();
                    return new LogicalBinaryExpression(left, Additive(), LogicalOperator.GreaterOrEq);
                default:
                    return left;
            }
        }


        public IExpression Additive()
        {
            var result = Multiplicative();
            // x + y + (x*y+x) = (x+y)+(x*y+x), not x+(y+(x*y+x)), it is important, because c# works this way
            while (true)
            {
                if (_tokenSequence.Current?.Type == TokenType.Plus)
                {
                    _tokenSequence.Step();
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

                if (_tokenSequence.Current?.Type == TokenType.Minus)
                {
                    _tokenSequence.Step();
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
                if (_tokenSequence.Current?.Type == TokenType.Star)
                {
                    _tokenSequence.Step();
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

                if (_tokenSequence.Current?.Type == TokenType.Slash)
                {
                    _tokenSequence.Step();
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
            if (_tokenSequence.Current.Type == TokenType.Minus)
            {
                if (_tokenSequence.Get(1).Type == TokenType.Num)
                {
                    return new UnaryExpression(ParseNumber());
                }

                _tokenSequence.Step();

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
            if (_tokenSequence.Current.Type == TokenType.Minus)
            {
                _tokenSequence.Step();
                var stringNumber = _tokenSequence.Current.Value;
                if (PrimaryExpression.GetPrimaryType('-' + _tokenSequence.Current.Value, out var compilerType))
                {
                    _tokenSequence.Step();
                    return new PrimaryExpression(stringNumber, compilerType);
                }
            }
            else
            {
                var stringNumber = _tokenSequence.Current.Value;
                if (PrimaryExpression.GetPrimaryType(_tokenSequence.Current.Value, out var compilerType))
                {
                    _tokenSequence.Step();
                    return new PrimaryExpression(stringNumber, compilerType);
                }
            }

            throw new Exception("Integral constant is too large");
        }

        public IExpression Primary()
        {
            if (_tokenSequence.Current.Type == TokenType.Num)
            {
                return ParseNumber();
            }

            if (_tokenSequence.Current.Type == TokenType.Num)
            {
                var stringNumber = _tokenSequence.Current.Value;
                if (PrimaryExpression.GetPrimaryType(_tokenSequence.Current.Value, out var compilerType))
                {
                    _tokenSequence.Step();
                    return new PrimaryExpression(stringNumber, compilerType);
                }

                throw new Exception("Integral constant is too large");
            }

            if (_tokenSequence.Current?.Type == TokenType.Variable)
            {
                var varToken = _tokenSequence.Current;
                _tokenSequence.Step();

                var variableType = GetVariableType(varToken);
                return new VariableExpression(varToken.Value, variableType);
            }

            if (_tokenSequence.Current?.Type == TokenType.RefWord)
            {
                if (_tokenSequence.Next.Type != TokenType.Variable)
                {
                    throw new Exception("ref keyword must using only with variables or method args");
                }

                _tokenSequence.Step();
                var varToken = _tokenSequence.CurrentWithStep();
                var variableType = GetVariableType(varToken);
                return new VariableExpression(varToken.Value, variableType, true);
            }

            if (_tokenSequence.Current?.Type == TokenType.LeftParent)
            {
                _tokenSequence.Step();
                var expression = Expression();
                if (_tokenSequence.Current?.Type != TokenType.RightParent)
                {
                    throw new Exception("Count of opening brackets must be equals count of closing brackets");
                }

                _tokenSequence.Step();
                return expression;
            }

            if (_tokenSequence.Current?.Type == TokenType.RightParent)
            {
                throw new Exception("Amount of opening brackets have to equals amount of closing brackets");
            }

            if (_tokenSequence.Current?.Type == TokenType.Word)
            {
                return MethodCallExpression();
            }

            if (_tokenSequence.Current?.Type == TokenType.Constant)
            {
                if (Constants.Dictionary.TryGetValue(_tokenSequence.Current.Value, out var item))
                {
                    _tokenSequence.Step();
                    if (item.Item1[0] != '-') return new PrimaryExpression(item.Item1, item.Item2);
                    return new UnaryExpression(new PrimaryExpression(item.Item1[1..], item.Item2));
                }

                throw new Exception("Unknown constant");
            }

            throw new ArgumentException();
        }

        private CompilerType GetVariableType(Token varToken)
        {
            if (_parameters.TryGetValue(varToken.Value, out var variableType) ||
                _closedFields.TryGetValue(varToken.Value, out variableType) ||
                _localVariables.TryGetValue(varToken.Value, out variableType))
            {
                return variableType;
            }

            throw new Exception("can't find this variable");
        }

        private MethodCallExpression MethodCallExpression()
        {
            var methodName = _tokenSequence.Current.Value;
            _tokenSequence.Step();
            if (_tokenSequence.Current.Type != TokenType.LeftParent)
            {
                throw new Exception("Opening bracket must be after method name");
            }

            _tokenSequence.Step();
            var @params = new List<IExpression>();
            while (_tokenSequence.Current?.Type != TokenType.RightParent)
            {
                @params.Add(Expression());

                if (_tokenSequence.Current.Type != TokenType.Comma &&
                    _tokenSequence.Current.Type != TokenType.RightParent)
                {
                    throw new Exception("Method parameters must be separated by comma");
                }

                if (_tokenSequence.Current.Type == TokenType.Comma)
                {
                    _tokenSequence.Step();
                }
            }

            if (_tokenSequence.Current == null) throw new Exception("Method must end with the closing bracket");
            _tokenSequence.Step();
            return new MethodCallExpression(methodName, @params);
        }
    }
}