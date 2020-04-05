using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Parser.Lexer;
using Parser.Parser;
using Parser.Parser.Expressions;
using Parser.Parser.Statements;
using Parser.Utils;

namespace Parser.ILCompiler
{
    public class CompileExpressionVisitor : ExpressionVisitor
    {
        public Dictionary<string, CompilerType> Variables { get; }

        // todo: собрать логику логгирования в логгере)))
        // todo вынести все switch cas'ы в отдельный файл 

        public class Logger
        {
            private List<string> _logger = new List<string>();

            public void Log(string log)
            {
                _logger.Add(log);
            }    

            public string[] GetLogs => _logger.ToArray();
        }

        // todo: убрать введя MethodArgumentExpression
        private readonly ILGenerator _ilGenerator;
        // for tests
        public Logger logger = new Logger();
        private const string TestedTypeFullName = "RunnerNamespace.Runner";

        public CompileExpressionVisitor(ILGenerator ilGenerator)
        {
            _ilGenerator = ilGenerator;
        }

        public string[] Start(Statement statements)
        {
            var localVariables = statements
                .Statements
                .OfType<AssignmentStatement>()
                .Where(x => x.Left.ExpressionType == ExpressionType.LocalVariable)
                .GroupBy(x => x.Left.Name, x => x.Left.ReturnType)
                .ToDictionary(x => x.Key, x => x.First());

            foreach (var localVariable in localVariables)
            {
                _ilGenerator.DeclareLocal(localVariable.Value.GetCSharpType());
            }

            foreach (var statement in statements.Statements)
            {
                VisitStatement(statement);
            }

            return logger.GetLogs;
        }

        public string[] Start(IExpression expression)
        {
            VisitExpression(expression);
            logger.Log("ret");
            _ilGenerator.Emit(OpCodes.Ret);

            return logger.GetLogs;
        }

        public LogicalBinaryExpression PrepareForConditionalIfNeeded(IExpression expression)
        {
            LogicalBinaryExpression result = null;
            if (expression.TryCast<UnaryExpression>(out var unary))
            {
                if (unary.Expression.TryCast<LogicalBinaryExpression>(out var logicalBinaryExpression))
                    result = UnWrapUnaryExpression(logicalBinaryExpression);

                else if (ExpressionType.Variables.HasFlag(unary.Expression.ExpressionType))
                {
                    result = new LogicalBinaryExpression(unary.Expression, new PrimaryExpression("false"), Operator.Eq);
                }
            }
            else if (ExpressionType.Variables.HasFlag(expression.ExpressionType))
            {
                result = new LogicalBinaryExpression(expression, new PrimaryExpression("true"), Operator.Eq);
            }
            else if (expression.TryCast<PrimaryExpression>(out var primary) && primary.ReturnType == CompilerType.Bool)
            {
                result = new LogicalBinaryExpression(primary, new PrimaryExpression("true"), Operator.Eq);
            }
            else if (!expression.TryCast<LogicalBinaryExpression>(out result))
            {
                throw new Exception("Не разобран случай, дописать");
            }


            return result;
        }

        public LogicalBinaryExpression UnWrapUnaryExpression(LogicalBinaryExpression logical)
        {
            var left = logical.Left;
            if (logical.Left.TryCast<UnaryExpression>(out var leftUnary))
            {
                left = leftUnary.Expression;
            }
            else if (logical.Left.TryCast<LogicalBinaryExpression>(out var leftLogical))
            {
                left = new UnaryExpression(leftLogical, UnaryType.Not);
            }

            var right = logical.Right;
            if (logical.Right.TryCast<UnaryExpression>(out var rightUnary))
            {
                right = rightUnary.Expression;
            }
            else if (logical.Right.TryCast<LogicalBinaryExpression>(out var rightLogical))
            {
                right = new UnaryExpression(rightLogical, UnaryType.Not);
            }

            var @operator = RevertOperator(logical.Operator);

            var result = new LogicalBinaryExpression(left, right, @operator);

            return result;

            Operator RevertOperator(Operator @operator) => @operator
                switch
                {
                    Operator.Less => Operator.GreaterOrEq,
                    Operator.Eq => Operator.NoEq,
                    Operator.And => Operator.Or,
                    Operator.LessOrEq => Operator.Greater,
                    Operator.Greater => Operator.LessOrEq,
                    Operator.GreaterOrEq => Operator.Less,
                    Operator.NoEq => Operator.Eq,
                    Operator.Or => Operator.And,
                    _ => throw new ArgumentOutOfRangeException()
                }
            ;
        }

        public void Visit1(IExpression expression, bool successLabel, Label label)
        {
            var prepared = PrepareForConditionalIfNeeded(expression);
            if (Operator.Arithmetic.HasFlag(prepared.Operator))
            {
                VisitExpression(prepared.Left);
                VisitExpression(prepared.Right);
                switch (prepared.Operator)
                {
                    case Operator.Less:
                        logger.Log("clt");
                        _ilGenerator.Emit(OpCodes.Clt);
                        break;
                    case Operator.LessOrEq:
                        logger.Log("cgt");
                        _ilGenerator.Emit(OpCodes.Cgt);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case Operator.Greater:
                        logger.Log("cgt");
                        _ilGenerator.Emit(OpCodes.Cgt);
                        break;
                    case Operator.GreaterOrEq:
                        logger.Log("clt");
                        _ilGenerator.Emit(OpCodes.Clt);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case Operator.Eq:
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case Operator.NoEq:
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ceq);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);

                        break;
                }

                _ilGenerator.Emit(successLabel ? OpCodes.Brtrue : OpCodes.Brfalse, label);
            }
            else
            {
                switch (prepared.Operator, branch: successLabel)
                {
                    case (Operator.And, true):
                        VisitAsAnd(prepared, successLabel, label);
                        break;
                    case (Operator.Or, true):
                        VisitAsOr(prepared, successLabel, label);
                        break;
                    case (Operator.And, false):
                        VisitAsOr(prepared, successLabel, label);
                        break;
                    case (Operator.Or, false):
                        VisitAsAnd(prepared, successLabel, label);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void VisitAsOr(LogicalBinaryExpression exp, bool successLabel, Label label)
        {
            Visit1(exp.Left, successLabel, label);
            Visit1(exp.Right, successLabel, label);
        }

        public void VisitAsAnd(LogicalBinaryExpression exp, bool successLabel, Label label)
        {
            var afterIf = _ilGenerator.DefineLabel();
            Visit1(exp.Left, !successLabel, afterIf);
            Visit1(exp.Right, successLabel, label);
            _ilGenerator.MarkLabel(afterIf);
        }


        protected override LogicalBinaryExpression VisitLogical(LogicalBinaryExpression logical)
        {
            if (Operator.Logical.HasFlag(logical.Operator))
            {
                LogicalBinaryExpression left;
                LogicalBinaryExpression right;

                left = PrepareForConditionalIfNeeded(logical.Left);
                right = PrepareForConditionalIfNeeded(logical.Right);

                switch (logical.Operator)
                {
                    case Operator.And:
                        Label @else = _ilGenerator.DefineLabel();
                        Label @end = _ilGenerator.DefineLabel();
                        Visit1(left, false, @else);
                        VisitExpression(right);
                        _ilGenerator.Emit(OpCodes.Br, end);
                        _ilGenerator.MarkLabel(@else);
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.MarkLabel(end);
                        break;
                    case Operator.Or:
                        @else = _ilGenerator.DefineLabel();
                        @end = _ilGenerator.DefineLabel();
                        Visit1(left, false, @else);
                        _ilGenerator.Emit(OpCodes.Ldc_I4_1);
                        _ilGenerator.Emit(OpCodes.Br, end);
                        _ilGenerator.MarkLabel(@else);
                        VisitExpression(right);
                        _ilGenerator.MarkLabel(end);
                        break;
                }
            }
            else
            {
                VisitExpression(logical.Left);
                VisitExpression(logical.Right);
                switch (logical.Operator)
                {
                    case Operator.Less:
                        logger.Log("clt");
                        _ilGenerator.Emit(OpCodes.Clt);
                        break;
                    case Operator.LessOrEq:
                        logger.Log("cgt");
                        _ilGenerator.Emit(OpCodes.Cgt);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case Operator.Greater:
                        logger.Log("cgt");
                        _ilGenerator.Emit(OpCodes.Cgt);
                        break;
                    case Operator.GreaterOrEq:
                        logger.Log("clt");
                        _ilGenerator.Emit(OpCodes.Clt);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case Operator.Eq:
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case Operator.NoEq:
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ceq);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);

                        break;
                }
            }

            return logical;
        }

        protected override IfElseStatement VisitIfElse(IfElseStatement statement)
        {
            VisitExpression(statement.Test);
            var @startEnd = _ilGenerator.DefineLabel();
            var @elseStart = statement.Else == null ? startEnd : _ilGenerator.DefineLabel();
            logger.Log($"brfalse {nameof(elseStart)}");
            _ilGenerator.Emit(OpCodes.Brfalse_S, elseStart);
            VisitStatement(statement.IfTrue);
            if (!statement.IfTrue.IsReturnStatement)
            {
                logger.Log($"br {nameof(startEnd)}");
                _ilGenerator.Emit(OpCodes.Br, startEnd);
            }

            if (statement.Else != null)
            {
                logger.Log($"mark {nameof(elseStart)}");
                _ilGenerator.MarkLabel(elseStart);
                VisitStatement(statement.Else);
                logger.Log($"mark {nameof(startEnd)}");
                _ilGenerator.MarkLabel(startEnd);
            }
            else
            {
                logger.Log($"mark {nameof(startEnd)}");
                _ilGenerator.MarkLabel(startEnd);
            }


            return statement;
        }


        protected override BinaryExpression VisitBinary(BinaryExpression binaryExpression)
        {
            var left = VisitExpression(binaryExpression.Left);
            if (binaryExpression.Left.ReturnType == CompilerType.Int &&
                binaryExpression.Right.ReturnType == CompilerType.Long)
            {
                logger.Log("conv.i8");
                _ilGenerator.Emit(OpCodes.Conv_I8);
            }

            var right = VisitExpression(binaryExpression.Right);
            if (binaryExpression.Left.ReturnType == CompilerType.Long &&
                binaryExpression.Right.ReturnType == CompilerType.Int)
            {
                logger.Log("conv.i8");
                _ilGenerator.Emit(OpCodes.Conv_I8);
            }

            switch (binaryExpression.TokenType)
            {
                case TokenType.Plus:
                    logger.Log("add");
                    _ilGenerator.Emit(OpCodes.Add);
                    break;
                case TokenType.Minus:
                    logger.Log("sub");
                    _ilGenerator.Emit(OpCodes.Sub);
                    break;
                case TokenType.Star:
                    logger.Log("mul");
                    _ilGenerator.Emit(OpCodes.Mul);
                    break;
                case TokenType.Slash:
                    logger.Log("div");
                    _ilGenerator.Emit(OpCodes.Div);
                    break;
            }

            return binaryExpression;
        }

        protected override ReturnStatement VisitReturn(ReturnStatement returnStatement)
        {
            VisitExpression(returnStatement.Returned);
            logger.Log("ret");
            _ilGenerator.Emit(OpCodes.Ret);
            return returnStatement;
        }

        protected override AssignmentStatement VisitAssignment(AssignmentStatement assignmentStatement)
        {
            VisitExpression(assignmentStatement.Right);
            if (assignmentStatement.Left.TryCast<FieldVariableExpression>(out var field))
            {
                _ilGenerator.Emit(OpCodes.Stsfld, field.FieldInfo);
            }
            else if (assignmentStatement.Left.TryCast<LocalVariableExpression>(out var localVariableExpression))
            {
                switch (localVariableExpression.Index)
                {
                    case 0:
                        _ilGenerator.Emit(OpCodes.Stloc_0);
                        break;
                    case 1:
                        _ilGenerator.Emit(OpCodes.Stloc_1);
                        break;
                    case 2:
                        _ilGenerator.Emit(OpCodes.Stloc_2);
                        break;
                    case 3:
                        _ilGenerator.Emit(OpCodes.Stloc_3);
                        break;
                    default:
                        _ilGenerator.Emit(OpCodes.Stloc, localVariableExpression.Name);
                        break;
                }
            }
            else if (assignmentStatement.Left.TryCast<MethodArgumentVariableExpression>(out var methodArg))
            {
                _ilGenerator.Emit(OpCodes.Starg_S, (byte) methodArg.Index);
            }

            return assignmentStatement;
        }

        protected override IExpression VisitUnary(UnaryExpression unaryExpression)
        {
            switch (unaryExpression.UnaryType)
            {
                case UnaryType.Negative:
                {
                    IExpression expression = default;
                    // long.Parse("9223372036854775808") throw ex, workaroundd:
                    if (unaryExpression.Expression.TryCast<PrimaryExpression>(out var pr))
                    {
                        var longMinValue = Constants.Dictionary["long.MinValue"].toString;
                        var intMinValue = Constants.Dictionary["int.MinValue"].toString;
                        if (pr.Value == longMinValue[1..])
                        {
                            expression = pr;
                            logger.Log($"ldc.i8 {longMinValue}");
                            _ilGenerator.Emit(OpCodes.Ldc_I8, long.Parse(longMinValue));
                        }
                        else if (pr.Value == intMinValue[1..])
                        {
                            expression = pr;
                            logger.Log($"ldc.i4 {intMinValue}");
                            _ilGenerator.Emit(OpCodes.Ldc_I4, int.Parse(intMinValue));
                        }
                        else
                        {
                            expression = VisitExpression(unaryExpression.Expression);
                            logger.Log("neg");
                            _ilGenerator.Emit(OpCodes.Neg);
                        }
                    }
                    else
                    {
                        expression = VisitExpression(unaryExpression.Expression);
                        logger.Log("neg");
                        _ilGenerator.Emit(OpCodes.Neg);
                    }

                    return new UnaryExpression(expression, UnaryType.Negative);
                }

                case UnaryType.Not:
                {
                    var expression = VisitExpression(unaryExpression.Expression);

                    logger.Log("Ldc.I4.0");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    logger.Log("ceq");
                    _ilGenerator.Emit(OpCodes.Ceq);
                    return new UnaryExpression(expression, UnaryType.Not);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void IntEmit(int value)
        {
            switch (value)
            {
                case 0:
                    logger.Log("ldc.i4.0");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    logger.Log("ldc.i4.1");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    logger.Log("ldc.i4.2");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    logger.Log("ldc.i4.3");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    logger.Log("ldc.i4.4");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    logger.Log("ldc.i4.5");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    logger.Log("ldc.i4.6");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    logger.Log("ldc.i4.7");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    logger.Log("ldc.i4.8");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value > -128 && value < 128)
                    {
                        logger.Log($"ldc.i4.s {value}");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte) value);
                    }
                    else
                    {
                        logger.Log($"ldc.i4 {value}");
                        _ilGenerator.Emit(OpCodes.Ldc_I4, value);
                    }

                    break;
            }
        }

        protected override PrimaryExpression VisitPrimary(PrimaryExpression primaryExpression)
        {
            switch (primaryExpression.ReturnType)
            {
                case CompilerType.Long:
                    logger.Log($"ldc.i8 {primaryExpression.AsLong()}");
                    _ilGenerator.Emit(OpCodes.Ldc_I8, primaryExpression.AsLong());
                    break;
                case CompilerType.Int:
                    IntEmit(primaryExpression.AsInt());
                    break;
                case CompilerType.Bool:
                    var value = primaryExpression.AsBool();
                    logger.Log(value ? "ldc.i4.1" : "ldc.i4.0");
                    _ilGenerator.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return primaryExpression;
        }

        protected override MethodCallExpression VisitMethod(MethodCallExpression methodCallExpression)
        {
            var methodParams = methodCallExpression.Parameters.ToArray();
            for (var i = 0;
                i < methodCallExpression.Parameters.Count;
                i++)
            {
                var expression = methodCallExpression.Parameters[i];
                VisitExpression(expression);
                if (expression.ReturnType == CompilerType.Int &&
                    methodParams[i].ParameterInfo.ParameterType == typeof(long))
                {
                    logger.Log("conv.i8");
                    _ilGenerator.Emit(OpCodes.Conv_I8);
                }
            }

            var logParams = string.Join(",", methodParams.Select(x => x.ParameterInfo.ParameterType.ToString()));
            logger.Log($"call {methodCallExpression.MethodInfo.ReturnType} {TestedTypeFullName}::{methodCallExpression.Name}({logParams})");
            _ilGenerator.Emit(OpCodes.Call, methodCallExpression.MethodInfo);
            return methodCallExpression;
        }

        protected override MethodArgumentVariableExpression VisitMethodArgument(
            MethodArgumentVariableExpression expression)
        {
            if (expression.ByReference)
            {
                logger.Log($"ldarga.s {expression.Name}");
                _ilGenerator.Emit(OpCodes.Ldarga_S, (byte) expression.Index);
                return expression;
            }

            switch (expression.Index)
            {
                case 0:
                    logger.Log($"ldarg.0");
                    _ilGenerator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    logger.Log($"ldarg.1");
                    _ilGenerator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    logger.Log($"ldarg.2");
                    _ilGenerator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    logger.Log($"ldarg.3");
                    _ilGenerator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    logger.Log($"ldarg.s {expression.Name}");
                    _ilGenerator.Emit(OpCodes.Ldarg_S, expression.Name);
                    break;
            }

            return expression;
        }

        protected override FieldVariableExpression VisitField(FieldVariableExpression expression)
        {
            if (expression.ByReference)
            {
                logger.Log($"ldsflda {expression.Name}");
                _ilGenerator.Emit(OpCodes.Ldsflda, expression.FieldInfo);
                return expression;
            }


            logger.Log($"ldsfld {expression.FieldInfo.FieldType} {TestedTypeFullName}::{expression.Name}");
            _ilGenerator.Emit(OpCodes.Ldsfld, expression.FieldInfo);
            return expression;
        }

        protected override VariableExpression VisitLocalVariable(LocalVariableExpression variable)
        {
            if (variable.ByReference)
            {
                logger.Log($"ldloca.s {variable.Index}");
                _ilGenerator.Emit(OpCodes.Ldloca_S, variable.Index);
                return variable;
            }

            switch (variable.Index)
            {
                case 0:
                    logger.Log($"ldloc.0");
                    _ilGenerator.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    logger.Log($"ldloc.1");
                    _ilGenerator.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    logger.Log($"ldloc.2");
                    _ilGenerator.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    logger.Log($"ldloc.3");
                    _ilGenerator.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    logger.Log($"ldloc.s {variable.Index}");
                    _ilGenerator.Emit(OpCodes.Ldloc_S, variable.Index);
                    break;
            }

            return variable;
        }
    }
}