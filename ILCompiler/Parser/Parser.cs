using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parser.Lexer;
using Parser.Parser.Exceptions;
using Parser.Parser.Expressions;
using Parser.Parser.Statements;
using Parser.Utils;

namespace Parser.Parser
{
    public static class ParserExtensions
    {
        public static bool CheckCannotImplicitConversion(this VariableExpression left, IExpression right)
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
    }

    public class Parser
    {
        private readonly Dictionary<string, FieldInfo> _closedFields;
        private readonly bool _constantFolding;
        private readonly Dictionary<string, CompilerType> _parameters;
        private readonly Dictionary<string, CompilerType> _localVariables = new Dictionary<string, CompilerType>();
        private readonly TokenSequence _tokenSequence;
        private readonly Dictionary<string, MethodInfo> _closureMethods;

        public Parser(ParserContext context)
        {
            _tokenSequence = new TokenSequence(context.Tokens);
            _parameters = context.MethodParameters;
            _closedFields = context.ClosureFields;
            _constantFolding = context.ConstantFolding;
            _closureMethods = context.ClosureMethods;
        }

        public Statement Parse()
        {
            var list = new List<IStatement>();
            while (!_tokenSequence.IsEmpty)
            {
                list.Add(Statement());
            }

            var statement = new Statement(list.ToArray());

            if (!statement.IsReturnStatement)
            {
                throw new CompileException("End of function is reachable without any return statement");
            }

            return statement;
        }

        public IExpression ParseExpression()
        {
            var expression = Expression();
            if (!_tokenSequence.IsEmpty)
            {
                throw new CompileException("Expression is incorrect");
            }

            return expression;
        }

        private IStatement AssignmentStatement()
        {
            VariableExpression varExpression = null;
            if (_tokenSequence.Current?.Type == TokenType.BoolWord ||
                _tokenSequence.Current?.Type == TokenType.IntWord ||
                _tokenSequence.Current?.Type == TokenType.LongWord)
            {
                var keywordType = _tokenSequence.Current.Type;
                _tokenSequence.Step();

                var type = keywordType.TokenToCompilerType();

                if (_localVariables.TryGetValue(_tokenSequence.Current.Value, out _))
                {
                    throw new CompileException(
                        $"Variable with name '{_tokenSequence.Current.Value}' is already declared. Use ");
                }

                if (_parameters.ContainsKey(_tokenSequence.Current.Value))
                {
                    throw new CompileException(
                        $"A local or parameter named '{_tokenSequence.Current.Value}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter");
                }

                varExpression = new LocalVariableExpression(_tokenSequence.Current.Value, type, _localVariables.Count);
                _localVariables.Add(varExpression.Name, varExpression.ReturnType);
                _tokenSequence.Step(); // variable name
            }
            else if (_tokenSequence.Current?.Type == TokenType.Variable)
            {
                varExpression = GetVariable(_tokenSequence.CurrentWithStep());
            }

            _tokenSequence.Step(); // assignment sign
            var expression = Expression();

            if (varExpression.CheckCannotImplicitConversion(expression))
            {
                throw new CompileException("Cannot implicitly convert type 'long ' to int");
            }

            _tokenSequence.ThrowIfNotMatched(TokenType.Semicolon, "Assignment statement must be ended by semicolon");

            return new AssignmentStatement(varExpression, expression);
        }

        public IStatement Statement()
        {
            if (_tokenSequence.Get(2) == Token.Assignment || _tokenSequence.Get(1) == Token.Assignment)
            {
                return AssignmentStatement();
            }

            if (_tokenSequence.IsTypeKeyWord())
            {
                _tokenSequence.Throw("You cannot leave a variable uninitialized");
            }

            if (_tokenSequence.Current?.Type == TokenType.ReturnWord)
            {
                _tokenSequence.Step();
                var expression = Expression();
                _tokenSequence.ThrowIfNotMatched(TokenType.Semicolon, "Return must be ended by semicolon");
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
                _tokenSequence.ThrowIfNotMatched(TokenType.Semicolon, "Missing semicolon");
                return new VoidMethodCallStatement(methodCall);
            }

            throw new NotSupportedException("Not supported statement");
        }

        public IfElseStatement IfElseStatement()
        {
            _tokenSequence.Step();
            _tokenSequence.ThrowIfNotMatched(TokenType.LeftParent, "Missing Left Parent");
            var test = Expression();

            _tokenSequence.ThrowIfNotMatched(TokenType.RightParent, "Missing Right Parent");
            _tokenSequence.ThrowIfNotMatched(TokenType.LeftBrace, "Missing left brace");

            var ifStatements = new List<IStatement>();
            while (_tokenSequence.Current.Type != TokenType.RightBrace)
            {
                ifStatements.Add(Statement());
            }


            if (_tokenSequence.Next?.Type != TokenType.ElseWord)
            {
                _tokenSequence.Step();
                return new IfElseStatement(test, new Statement(ifStatements.ToArray()));
            }

            _tokenSequence.Step();
            _tokenSequence.Step();

            _tokenSequence.ThrowIfNotMatched(TokenType.LeftBrace, "Missing left brace");

            var elseStatements = new List<IStatement>();
            while (_tokenSequence.Current.Type != TokenType.RightBrace)
            {
                elseStatements.Add(Statement());
            }

            _tokenSequence.ThrowIfNotMatched(TokenType.RightBrace, "Missing right brace");

            return new IfElseStatement(test,
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
                result = new LogicalBinaryExpression(result, AndLogical(), Operator.Or);
            }

            return result;
        }

        public IExpression AndLogical()
        {
            var result = EqualsNoEquals();
            while (_tokenSequence.Current?.Type == TokenType.And)
            {
                _tokenSequence.Step();
                result = new LogicalBinaryExpression(result, EqualsNoEquals(), Operator.And);
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
                        result = new LogicalBinaryExpression(result, Logical(), Operator.Eq);
                        break;
                    case TokenType.NotEqualTo:
                        _tokenSequence.Step();
                        result = new LogicalBinaryExpression(result, Logical(), Operator.NoEq);
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
                    return new LogicalBinaryExpression(left, Additive(), Operator.Less);
                case TokenType.LessThanOrEquals:
                    _tokenSequence.Step();
                    return new LogicalBinaryExpression(left, Additive(), Operator.LessOrEq);
                case TokenType.GreaterThan:
                    _tokenSequence.Step();
                    return new LogicalBinaryExpression(left, Additive(), Operator.Greater);
                case TokenType.GreaterThanOrEquals:
                    _tokenSequence.Step();
                    return new LogicalBinaryExpression(left, Additive(), Operator.GreaterOrEq);
                default:
                    return left;
            }
        }


        public IExpression Additive()
        {
            var result = Multiplicative();
            CheckValidArithmeticOperation(_tokenSequence.Current?.Type, result);
            // x + y + (x*y+x) = (x+y)+(x*y+x), not x+(y+(x*y+x)), it is important, because c# works this way
            while (true)
            {
                if (_tokenSequence.Current?.Type == TokenType.Plus)
                {
                    _tokenSequence.Step();
                    var right = Multiplicative();
                    CheckValidArithmeticOperation(TokenType.Plus, right);
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
                    CheckValidArithmeticOperation(TokenType.Minus, right);
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
                        return new UnaryExpression(new PrimaryExpression(num[1..], type), UnaryType.Negative);
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

        private void CheckValidArithmeticOperation(TokenType? type, IExpression expression)
        {
            if (type != null && expression.ReturnType == CompilerType.Bool &&
                (type == TokenType.Star || type == TokenType.Slash || type == TokenType.Minus ||
                 type == TokenType.Plus))
            {
                _tokenSequence.Throw("Invalid arithmetic operation");
            }
        }

        public IExpression Multiplicative()
        {
            var result = Unary();
            CheckValidArithmeticOperation(_tokenSequence.Current?.Type, result);
            // x*y*z = ((x*y)*z) not (x*(y*z)) , it is important, because c# works this way
            while (true)
            {
                if (_tokenSequence.Current?.Type == TokenType.Star)
                {
                    _tokenSequence.Step();
                    var right = Unary();
                    CheckValidArithmeticOperation(TokenType.Star, right);
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
                    CheckValidArithmeticOperation(TokenType.Slash, right);
                    if (TryFold(result, out var leftValue, out var leftType) &&
                        TryFold(right, out var rightValue, out var rightType))
                    {
                        //12/0 
                        if (rightValue == "0")
                            throw new CompileException("Divide by zero");

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
                if (_tokenSequence.Get(1).Type == TokenType.Constant)
                {
                    return new UnaryExpression(ParseConstant(), UnaryType.Negative);
                }

                _tokenSequence.Step();

                var expression = Primary();
                if (_constantFolding && expression.TryCast<UnaryExpression>(out var unary) &&
                    unary.UnaryType == UnaryType.Negative &&
                    unary.Expression.TryCast<PrimaryExpression>(out var primary))
                {
                    return primary;
                }

                return new UnaryExpression(expression, UnaryType.Negative);
            }

            if (_tokenSequence.Current.Type == TokenType.Not)
            {
                _tokenSequence.Step();
                var afterToken = _tokenSequence.CurrentWithStep();
                if (afterToken.Type == TokenType.Variable)
                {
                    var variable = GetVariable(afterToken);
                    if (variable.ReturnType != CompilerType.Bool)
                    {
                        throw new CompileException($"Sign! cannot be used with a {variable.ReturnType} variable");
                    }

                    // _tokenSequence.Step();
                    return new UnaryExpression(variable, UnaryType.Not);
                }

                if (afterToken.Type != TokenType.LeftParent)
                {
                    throw new CompileException("Должна стоять открывающая скобка");
                }

                var expression = Expression();

                var closeParen = _tokenSequence.CurrentWithStep();
                if (closeParen.Type != TokenType.RightParent)
                {
                    throw new CompileException("Должна стоять закрывающая скобка");
                }

                return new UnaryExpression(expression, UnaryType.Not);
            }

            return Primary();
        }

        private IExpression ParseConstant()
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

            throw new CompileException("Integral constant is too large");
        }

        private VariableExpression GetVariable(Token token, bool byReference = false)
        {
            if (_parameters.TryGetValue(token.Value, out var compilerType))
            {
                var index = Array.IndexOf(_parameters.Keys.ToArray(), token.Value);
                return new MethodArgumentVariableExpression(token.Value, compilerType, index, byReference);
            }

            if (_localVariables.TryGetValue(token.Value, out compilerType))
            {
                var index = Array.IndexOf(_localVariables.Keys.ToArray(), token.Value);
                return new LocalVariableExpression(token.Value, compilerType, index, byReference);
            }

            if (_closedFields.TryGetValue(token.Value, out var fieldInfo))
            {
                return new FieldVariableExpression(token.Value, fieldInfo, byReference);
            }


            throw new ArgumentOutOfRangeException(
                $"Variable with name '{_tokenSequence.Current.Value}' is not declared");
            // var variableType = GetVariableType(token);
            // return new LocalVariableExpression(token.Value, variableType, byReference);
        }

        private IExpression Primary()
        {
            if (_tokenSequence.Current.Type == TokenType.Constant)
            {
                return ParseConstant();
            }

            if (_tokenSequence.Current?.Type == TokenType.Variable)
            {
                var varToken = _tokenSequence.Current;
                _tokenSequence.Step();

                return GetVariable(varToken);
            }

            if (_tokenSequence.Current?.Type == TokenType.RefWord)
            {
                if (_tokenSequence.Next.Type != TokenType.Variable)
                {
                    throw new CompileException("ref keyword must using only with variables or method args");
                }

                _tokenSequence.Step();
                var varToken = _tokenSequence.CurrentWithStep();
                var variable = GetVariable(varToken, true);
                return variable;
            }

            if (_tokenSequence.Current?.Type == TokenType.LeftParent)
            {
                _tokenSequence.Step();
                var expression = Expression();
                if (_tokenSequence.Current?.Type != TokenType.RightParent)
                {
                    throw new CompileException("Count of opening brackets must be equals count of closing brackets");
                }

                _tokenSequence.Step();
                return expression;
            }

            if (_tokenSequence.Current?.Type == TokenType.RightParent)
            {
                throw new CompileException("Amount of opening brackets have to equals amount of closing brackets");
            }

            if (_tokenSequence.Current?.Type == TokenType.Word)
            {
                return MethodCallExpression();
            }

            throw new ArgumentException();
        }

        private MethodCallExpression MethodCallExpression()
        {
            var methodName = _tokenSequence.Current.Value;
            if (!_closureMethods.TryGetValue(methodName, out var methodInfo))
            {
                throw new CompileException(
                    $"Could not find method \"{methodName}\", use static methods of the class, please ");
            }

            _tokenSequence.Step();
            if (_tokenSequence.Current.Type != TokenType.LeftParent)
            {
                throw new CompileException("Opening bracket must be after method name");
            }

            _tokenSequence.Step();
            var @params = new List<MethodCallParameterExpression>();
            int i = 0;
            var methodParameters = methodInfo.GetParameters();
            for (i = 0; _tokenSequence.Current?.Type != TokenType.RightParent && i < methodParameters.Length; i++)
            {
                var methodParameterType = methodParameters[i];
                var parameter = Expression();
                var expectedMethodParameter = methodParameters[i].ParameterType.GetRoslynType();

                if (expectedMethodParameter != parameter.ReturnType)
                {
                    if (!(expectedMethodParameter == CompilerType.Long && parameter.ReturnType == CompilerType.Int))
                    {
                        _tokenSequence.Throw(
                            $"Incorrect parameters passed for the {methodName} method. Expected :{methodParameterType}, actual {parameter.ReturnType}");
                    }
                }

                @params.Add(new MethodCallParameterExpression(parameter, methodParameters[i]));

                if (_tokenSequence.Current.Type != TokenType.Comma &&
                    _tokenSequence.Current.Type != TokenType.RightParent)
                {
                    _tokenSequence.Throw("Method parameters must be separated by comma");
                }

                if (_tokenSequence.Current.Type == TokenType.Comma)
                {
                    _tokenSequence.Step();
                }
            }

            if (i != methodParameters.Length || _tokenSequence.Current?.Type != TokenType.RightParent)
            {
                _tokenSequence.Throw($"{methodName} method passed an incorrect number of parameters");
            }

            if (_tokenSequence.Current == null) throw new CompileException("Method must end with the closing bracket");
            _tokenSequence.Step();
            return new MethodCallExpression(methodName, methodInfo, @params);
        }
    }
}