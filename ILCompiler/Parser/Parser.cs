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
    public class Parser
    {
        private readonly Dictionary<string, FieldInfo> _closedFields;
        private readonly bool _constantFolding;
        private readonly Dictionary<string, CompilerType> _parameters;
        private readonly Dictionary<string, CompilerType> _localVariables = new Dictionary<string, CompilerType>();
        private readonly SemanticTokenSequence _semTokens;
        private readonly Dictionary<string, MethodInfo> _closureMethods;

        public Parser(ParserContext context)
        {
            _semTokens = new SemanticTokenSequence(context.Tokens);
            _parameters = context.MethodParameters;
            _closedFields = context.ClosureFields;
            _constantFolding = context.ConstantFolding;
            _closureMethods = context.ClosureMethods;
        }

        public Statement Parse()
        {
            var list = new List<IStatement>();
            while (!_semTokens.IsEmpty)
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
            if (!_semTokens.IsEmpty)
            {
                throw new CompileException("Expression is incorrect");
            }

            return expression;
        }

        private IStatement AssignmentStatement()
        {
            VariableExpression varExpression = null;
            if (_semTokens.IsTypeKeyWord())
            {
                var keywordType = _semTokens.Current.Type;
                _semTokens.Step();

                var type = keywordType.TokenToCompilerType();

                if (_localVariables.TryGetValue(_semTokens.Current.Value, out _))
                {
                    Throw(
                        $"Variable with name '{_semTokens.Current.Value}' is already declared. Use ");
                }

                if (_parameters.ContainsKey(_semTokens.Current.Value))
                {
                    Throw(
                        $"A local or parameter named '{_semTokens.Current.Value}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter");
                }

                varExpression = new LocalVariableExpression(_semTokens.Current.Value, type, _localVariables.Count);
                _localVariables.Add(varExpression.Name, varExpression.ReturnType);
                _semTokens.Step(); // variable name
            }
            else if (_semTokens.Current?.Type == TokenType.Variable)
            {
                varExpression = GetVariable(_semTokens.CurrentWithStep());
            }

            _semTokens.Step(); // assignment sign
            var expression = Expression();

            if (varExpression.CheckCannotImplicitConversion(expression))
            {
                throw new CompileException("Cannot implicitly convert type 'long ' to int");
            }

            ThrowIfNotMatch(TokenType.Semicolon, "Assignment statement must be ended by semicolon");

            return new AssignmentStatement(varExpression, expression);
        }

        public IStatement Statement()
        {
            if (_semTokens.Get(2) == Token.Assignment || _semTokens.Get(1) == Token.Assignment)
            {
                return AssignmentStatement();
            }

            if (_semTokens.IsTypeKeyWord())
            {
                Throw("You cannot leave a variable uninitialized");
            }

            if (_semTokens.Current?.Type == TokenType.ReturnWord)
            {
                _semTokens.Step();
                var expression = Expression();
                ThrowIfNotMatch(TokenType.Semicolon, "Return must be ended by semicolon");
                return new ReturnStatement(expression);
            }

            if (_semTokens.Current?.Type == TokenType.IfWord)
            {
                return IfElseStatement();
            }

            if (_semTokens.IsMethod())
            {
                var methodCall = MethodCallExpression();
                ThrowIfNotMatch(TokenType.Semicolon, "Missing semicolon");
                return new VoidMethodCallStatement(methodCall);
            }

            throw new NotSupportedException("Not supported statement");
        }

        public IfElseStatement IfElseStatement()
        {
            _semTokens.Step();
            ThrowIfNotMatch(TokenType.LeftParent, "Missing Left Parent");
            var test = Expression();

            ThrowIfNotMatch(TokenType.RightParent, "Missing Right Parent");
            ThrowIfNotMatch(TokenType.LeftBrace, "Missing left brace");

            var ifStatements = new List<IStatement>();
            while (_semTokens.Current.Type != TokenType.RightBrace)
            {
                ifStatements.Add(Statement());
            }


            if (_semTokens.Next?.Type != TokenType.ElseWord)
            {
                _semTokens.Step();
                return new IfElseStatement(test, new Statement(ifStatements.ToArray()));
            }

            _semTokens.Step(2);

            ThrowIfNotMatch(TokenType.LeftBrace, "Missing left brace");

            var elseStatements = new List<IStatement>();
            while (_semTokens.Current.Type != TokenType.RightBrace)
            {
                elseStatements.Add(Statement());
            }

            ThrowIfNotMatch(TokenType.RightBrace, "Missing right brace");

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
            while (_semTokens.Current?.Type == TokenType.Or)
            {
                _semTokens.Step();
                result = new LogicalBinaryExpression(result, AndLogical(), Operator.Or);
            }

            return result;
        }

        public IExpression AndLogical()
        {
            var result = EqualsNoEquals();
            while (_semTokens.Current?.Type == TokenType.And)
            {
                _semTokens.Step();
                result = new LogicalBinaryExpression(result, EqualsNoEquals(), Operator.And);
            }

            return result;
        }

        public IExpression EqualsNoEquals()
        {
            var result = Logical();
            while (_semTokens.Current?.Type == TokenType.EqualTo ||
                   _semTokens.Current?.Type == TokenType.NotEqualTo)
            {
                switch (_semTokens.Current?.Type)
                {
                    case TokenType.EqualTo:
                        _semTokens.Step();
                        result = new LogicalBinaryExpression(result, Logical(), Operator.Eq);
                        break;
                    case TokenType.NotEqualTo:
                        _semTokens.Step();
                        result = new LogicalBinaryExpression(result, Logical(), Operator.NoEq);
                        break;
                }
            }

            return result;
        }

        public IExpression Logical()
        {
            var left = Additive();
            switch (_semTokens.Current?.Type)
            {
                case TokenType.LessThan:
                    _semTokens.Step();
                    return new LogicalBinaryExpression(left, Additive(), Operator.Less);
                case TokenType.LessThanOrEquals:
                    _semTokens.Step();
                    return new LogicalBinaryExpression(left, Additive(), Operator.LessOrEq);
                case TokenType.GreaterThan:
                    _semTokens.Step();
                    return new LogicalBinaryExpression(left, Additive(), Operator.Greater);
                case TokenType.GreaterThanOrEquals:
                    _semTokens.Step();
                    return new LogicalBinaryExpression(left, Additive(), Operator.GreaterOrEq);
                default:
                    return left;
            }
        }


        public IExpression Additive()
        {
            var result = Multiplicative();
            CheckValidArithmeticOperation(_semTokens.Current?.Type, result);
            // x + y + (x*y+x) = (x+y)+(x*y+x), not x+(y+(x*y+x)), it is important, because c# works this way
            while (true)
            {
                if (_semTokens.Current?.Type == TokenType.Plus)
                {
                    _semTokens.Step();
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

                if (_semTokens.Current?.Type == TokenType.Minus)
                {
                    _semTokens.Step();
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

        private IExpression ConstantFold(string leftValue,
            CompilerType leftType,
            string rightValue,
            CompilerType rightType,
            TokenType operationType)
        {
            return ParserExtensions.TryOperationWithCheckOverflow(leftValue, leftType, rightValue, rightType,
                operationType);
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
                Throw("Invalid arithmetic operation");
            }
        }

        public IExpression Multiplicative()
        {
            var result = Unary();
            CheckValidArithmeticOperation(_semTokens.Current?.Type, result);
            // x*y*z = ((x*y)*z) not (x*(y*z)) , it is important, because c# works this way
            while (true)
            {
                if (_semTokens.Current?.Type == TokenType.Star)
                {
                    _semTokens.Step();
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

                if (_semTokens.Current?.Type == TokenType.Slash)
                {
                    _semTokens.Step();
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
            if (_semTokens.Current.Type == TokenType.Minus)
            {
                if (_semTokens.Get(1).Type == TokenType.Constant)
                {
                    return new UnaryExpression(ParseConstant(), UnaryType.Negative);
                }

                _semTokens.Step();

                var expression = Primary();
                if (_constantFolding && expression.TryCast<UnaryExpression>(out var unary) &&
                    unary.UnaryType == UnaryType.Negative &&
                    unary.Expression.TryCast<PrimaryExpression>(out var primary))
                {
                    return primary;
                }

                return new UnaryExpression(expression, UnaryType.Negative);
            }

            if (_semTokens.Current.Type == TokenType.Not)
            {
                _semTokens.Step();
                var afterToken = _semTokens.CurrentWithStep();
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
                    throw new CompileException("Must have an opening bracket");
                }

                var expression = Expression();

                var closeParen = _semTokens.CurrentWithStep();
                if (closeParen.Type != TokenType.RightParent)
                {
                    throw new CompileException("Must have a closing bracket");
                }

                return new UnaryExpression(expression, UnaryType.Not);
            }

            return Primary();
        }

        private IExpression ParseConstant()
        {
            if (_semTokens.Current.Type == TokenType.Minus)
            {
                _semTokens.Step();
                var stringNumber = _semTokens.Current.Value;
                if (PrimaryExpression.GetPrimaryType('-' + _semTokens.Current.Value, out var compilerType))
                {
                    _semTokens.Step();
                    return new PrimaryExpression(stringNumber, compilerType);
                }
            }
            else
            {
                var stringNumber = _semTokens.Current.Value;
                if (PrimaryExpression.GetPrimaryType(_semTokens.Current.Value, out var compilerType))
                {
                    _semTokens.Step();
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
                $"Variable with name '{_semTokens.Current.Value}' is not declared");
        }

        private IExpression Primary()
        {
            if (_semTokens.Current.Type == TokenType.Constant)
            {
                return ParseConstant();
            }

            if (_semTokens.Current?.Type == TokenType.Variable)
            {
                var varToken = _semTokens.CurrentWithStep();

                return GetVariable(varToken);
            }

            if (_semTokens.Current?.Type == TokenType.RefWord)
            {
                if (_semTokens.Next.Type != TokenType.Variable)
                {
                    Throw("ref keyword must using only with variables or method args");
                }

                _semTokens.Step();
                var varToken = _semTokens.CurrentWithStep();
                var variable = GetVariable(varToken, true);
                return variable;
            }

            if (_semTokens.Current?.Type == TokenType.LeftParent)
            {
                _semTokens.Step();
                var expression = Expression();
                if (_semTokens.Current?.Type != TokenType.RightParent)
                {
                    Throw("Count of opening brackets must be equals count of closing brackets");
                }

                _semTokens.Step();
                return expression;
            }

            if (_semTokens.Current?.Type == TokenType.RightParent)
            {
                Throw("Amount of opening brackets have to equals amount of closing brackets");
            }

            if (_semTokens.Current?.Type == TokenType.Word)
            {
                return MethodCallExpression();
            }

            throw new ArgumentException();
        }

        private MethodCallExpression MethodCallExpression()
        {
            var methodName = _semTokens.Current.Value;
            if (!_closureMethods.TryGetValue(methodName, out var methodInfo))
            {
                Throw($"Could not find method \"{methodName}\", use static methods of the class, please ");
            }

            _semTokens.Step();
            if (_semTokens.Current.Type != TokenType.LeftParent)
            {
                Throw("Opening bracket must be after method name");
            }

            _semTokens.Step();
            var @params = new List<MethodCallParameterExpression>();
            int i = 0;
            var methodParameters = methodInfo.GetParameters();
            for (i = 0; _semTokens.Current?.Type != TokenType.RightParent && i < methodParameters.Length; i++)
            {
                var methodParameterType = methodParameters[i];
                var parameter = Expression();
                var expectedMethodParameter = methodParameters[i].ParameterType.GetRoslynType();

                if (expectedMethodParameter != parameter.ReturnType)
                {
                    if (!(expectedMethodParameter == CompilerType.Long && parameter.ReturnType == CompilerType.Int))
                    {
                        Throw(
                            $"Incorrect parameters passed for the {methodName} method. Expected :{methodParameterType}, actual {parameter.ReturnType}");
                    }
                }

                @params.Add(new MethodCallParameterExpression(parameter, methodParameters[i]));

                if (_semTokens.Current.Type != TokenType.Comma &&
                    _semTokens.Current.Type != TokenType.RightParent)
                {
                    Throw("Method parameters must be separated by comma");
                }

                if (_semTokens.Current.Type == TokenType.Comma)
                {
                    _semTokens.Step();
                }
            }

            if (i != methodParameters.Length || _semTokens.Current?.Type != TokenType.RightParent)
            {
                Throw($"{methodName} method passed an incorrect number of parameters");
            }

            if (_semTokens.Current == null) throw new CompileException("Method must end with the closing bracket");
            _semTokens.Step();
            return new MethodCallExpression(methodName, methodInfo, @params);
        }

        private void ThrowIfNotMatch(TokenType type, string message, bool needStep = true)
        {
            if (_semTokens.IsEmpty || _semTokens.Current.Type != type)
            {
                throw new CompileException($"{message}\nSubstring:{_semTokens.GetCurrentSubstring()}");
            }

            if (needStep)
            {
                _semTokens.Step();
            }
        }

        private void Throw(string message)
        {
            throw new CompileException($"{message}\nSubstring:{_semTokens.GetCurrentSubstring()}");
        }
    }
}