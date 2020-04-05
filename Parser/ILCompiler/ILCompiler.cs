using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Parser;

namespace Compiler
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
        private readonly Dictionary<string, MethodInfo> _closedMethods;
        private readonly ILGenerator _ilGenerator;

        // for tests
        public Logger logger = new Logger();
        private const string TestedTypeFullName = "RunnerNamespace.Runner";

        public CompileExpressionVisitor(ILGenerator ilGenerator, Dictionary<string, MethodInfo> closedMethods)
        {
            _closedMethods = closedMethods ?? new Dictionary<string, MethodInfo>();
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

        public void VisitLogical(LogicalBinaryExpression expession, bool successLabel, Label label)
        {
            LogicalBinaryExpression exp;
            if (expession.TryCast<LogicalBinaryExpression>(out var logical))
            {
                exp = logical;
            }
            else if (expession.TryCast<UnaryExpression>(out var unary))
            {
                exp = (LogicalBinaryExpression) VisitUnary(unary);
            }
            else throw new NotImplementedException();

            if (exp.Left.ExpressionType != ExpressionType.Logical && exp.Right.ExpressionType != ExpressionType.Logical)
            {
                VisitExpression(exp.Left);
                VisitExpression(exp.Right);
                switch (exp.Operator)
                {
                    case LogicalOperator.Less:
                        logger.Log("clt");
                        _ilGenerator.Emit(OpCodes.Clt);
                        break;
                    case LogicalOperator.LessOrEq:
                        logger.Log("cgt");
                        _ilGenerator.Emit(OpCodes.Cgt);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case LogicalOperator.Greater:
                        logger.Log("cgt");
                        _ilGenerator.Emit(OpCodes.Cgt);
                        break;
                    case LogicalOperator.GreaterOrEq:
                        logger.Log("clt");
                        _ilGenerator.Emit(OpCodes.Clt);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case LogicalOperator.Eq:
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case LogicalOperator.NoEq:
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ceq);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case LogicalOperator.And:
                        logger.Log("and");
                        _ilGenerator.Emit(OpCodes.And);
                        break;
                    case LogicalOperator.Or:
                        logger.Log("or");
                        _ilGenerator.Emit(OpCodes.Or);
                        break;
                }

                _ilGenerator.Emit(successLabel ? OpCodes.Brtrue : OpCodes.Brfalse, label);
            }
            else
            {
                switch (exp.Operator, branch: successLabel)
                {
                    case (LogicalOperator.And, true):
                        VisitAsAnd(exp, successLabel, label);
                        break;
                    case (LogicalOperator.Or, true):
                        VisitAsOr(exp, successLabel, label);
                        break;
                    case (LogicalOperator.And, false):
                        VisitAsOr(exp, successLabel, label);
                        break;
                    case (LogicalOperator.Or, false):
                        VisitAsAnd(exp, successLabel, label);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void VisitAsOr(LogicalBinaryExpression exp, bool successLabel, Label label)
        {
            VisitLogical((LogicalBinaryExpression) exp.Left, successLabel, label);
            VisitLogical((LogicalBinaryExpression) exp.Right, successLabel, label);
        }

        public void VisitAsAnd(LogicalBinaryExpression exp, bool successLabel, Label label)
        {
            var endIf = _ilGenerator.DefineLabel();
            VisitLogical((LogicalBinaryExpression) exp.Left, !successLabel, endIf);
            VisitLogical((LogicalBinaryExpression) exp.Right, successLabel, label);
            _ilGenerator.MarkLabel(endIf);
        }


        public override LogicalBinaryExpression VisitLogical(LogicalBinaryExpression logical)
        {
            if (logical.Left.ExpressionType != ExpressionType.Logical &&
                logical.Right.ExpressionType != ExpressionType.Logical)
            {
                VisitExpression(logical.Left);
                VisitExpression(logical.Right);
                switch (logical.Operator)
                {
                    case LogicalOperator.Less:
                        logger.Log("clt");
                        _ilGenerator.Emit(OpCodes.Clt);
                        break;
                    case LogicalOperator.LessOrEq:
                        logger.Log("cgt");
                        _ilGenerator.Emit(OpCodes.Cgt);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case LogicalOperator.Greater:
                        logger.Log("cgt");
                        _ilGenerator.Emit(OpCodes.Cgt);
                        break;
                    case LogicalOperator.GreaterOrEq:
                        logger.Log("clt");
                        _ilGenerator.Emit(OpCodes.Clt);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case LogicalOperator.Eq:
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case LogicalOperator.NoEq:
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ceq);
                        logger.Log("ldc.i4.0");
                        logger.Log("ceq");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.Emit(OpCodes.Ceq);
                        break;
                    case LogicalOperator.And:
                        logger.Log("and");
                        _ilGenerator.Emit(OpCodes.And);
                        break;
                    case LogicalOperator.Or:
                        logger.Log("or");
                        _ilGenerator.Emit(OpCodes.Or);
                        break;
                }
            }
            else if (logical.Left.ExpressionType == ExpressionType.Logical &&
                     logical.Right.ExpressionType == ExpressionType.Primary)
            {
                logical.Right.TryCast<PrimaryExpression>(out var primaryExpression);
                if (primaryExpression.Value == "true")
                {
                    VisitLogical((LogicalBinaryExpression) logical.Left);
                }
                else if (primaryExpression.Value == "false")
                {
                    VisitUnary(new UnaryExpression(logical.Left, UnaryType.Not));
                }
            }
            else if (logical.Left.ExpressionType == ExpressionType.Primary &&
                     logical.Right.ExpressionType == ExpressionType.Logical)
            {
                logical.Left.TryCast<PrimaryExpression>(out var primaryExpression);
                if (primaryExpression.Value == "true")
                {
                    VisitLogical((LogicalBinaryExpression) logical.Right);
                }
                else if (primaryExpression.Value == "false")
                {
                    VisitUnary(new UnaryExpression(logical.Right, UnaryType.Not));
                }
            }
            else
            {
                LogicalBinaryExpression left;
                LogicalBinaryExpression right;

                if (logical.TryCast<UnaryExpression>(out var unaryExpression))
                    left = (LogicalBinaryExpression) VisitUnary(unaryExpression);
                else logical.Left.TryCast(out left);

                if (logical.TryCast(out unaryExpression))
                    right = (LogicalBinaryExpression) VisitUnary(unaryExpression);
                else logical.Right.TryCast(out right);
                switch (logical.Operator)
                {
                    case LogicalOperator.And:
                        Label @else = _ilGenerator.DefineLabel();
                        Label @end = _ilGenerator.DefineLabel();
                        VisitLogical(left, false, @else);
                        VisitLogical(right);
                        _ilGenerator.Emit(OpCodes.Br, end);
                        _ilGenerator.MarkLabel(@else);
                        _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        _ilGenerator.MarkLabel(end);
                        break;
                    case LogicalOperator.Or:
                        @else = _ilGenerator.DefineLabel();
                        @end = _ilGenerator.DefineLabel();
                        VisitLogical(left, false, @else);
                        _ilGenerator.Emit(OpCodes.Ldc_I4_1);
                        _ilGenerator.Emit(OpCodes.Br, end);
                        _ilGenerator.MarkLabel(@else);
                        VisitLogical(right);
                        _ilGenerator.MarkLabel(end);
                        break;
                }
            }


            return logical;
        }

        public override IfElseStatement VisitIfElse(IfElseStatement statement)
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


        public override BinaryExpression VisitBinary(BinaryExpression binaryExpression)
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

        public override ReturnStatement VisitReturn(ReturnStatement returnStatement)
        {
            VisitExpression(returnStatement.Returned);
            logger.Log("ret");
            _ilGenerator.Emit(OpCodes.Ret);
            return returnStatement;
        }

        public override AssignmentStatement VisitAssignment(AssignmentStatement assignmentStatement)
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

        public override IExpression VisitUnary(UnaryExpression unaryExpression)
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
                    if (unaryExpression.Expression.TryCast<LogicalBinaryExpression>(
                        out var logExp))
                    {
                        var left = logExp.Left;
                        if (logExp.Left.TryCast<UnaryExpression>(out var leftUnary))
                        {
                            left = leftUnary.Expression;
                        }
                        else if (logExp.Left.TryCast<LogicalBinaryExpression>(out var leftLogical))
                        {
                            left = new UnaryExpression(leftLogical, UnaryType.Not);
                        }

                        var right = logExp.Right;
                        if (logExp.Right.TryCast<UnaryExpression>(out var rightUnary))
                        {
                            right = rightUnary.Expression;
                        }
                        else if (logExp.Right.TryCast<LogicalBinaryExpression>(out var rightLogical))
                        {
                            right = new UnaryExpression(rightLogical, UnaryType.Not);
                        }

                        var @operator = RevertOperator(logExp.Operator);

                        var result = new LogicalBinaryExpression(left, right, @operator);

                        VisitExpression(result);
                        return result;

                        LogicalOperator RevertOperator(LogicalOperator @operator) => @operator
                            switch
                            {
                                LogicalOperator.Less => LogicalOperator.GreaterOrEq,
                                LogicalOperator.Eq => LogicalOperator.NoEq,
                                LogicalOperator.And => LogicalOperator.Or,
                                LogicalOperator.LessOrEq => LogicalOperator.Greater,
                                LogicalOperator.Greater => LogicalOperator.LessOrEq,
                                LogicalOperator.GreaterOrEq => LogicalOperator.Less,
                                LogicalOperator.NoEq => LogicalOperator.Eq,
                                LogicalOperator.Or => LogicalOperator.And,
                                _ => throw new ArgumentOutOfRangeException()
                            }
                        ;
                    }

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

        public override PrimaryExpression VisitPrimary(PrimaryExpression primaryExpression)
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

        public override MethodCallExpression VisitMethod(MethodCallExpression methodCallExpression)
        {
            var method = _closedMethods[methodCallExpression.Name];

            var methodParams = method.GetParameters();
            for (var i = 0;
                i < methodCallExpression.Parameters.Count;
                i++)
            {
                var expression = methodCallExpression.Parameters[i];
                VisitExpression(expression);
                if (expression.ReturnType == CompilerType.Int && methodParams[i].ParameterType == typeof(long))
                {
                    logger.Log("conv.i8");
                    _ilGenerator.Emit(OpCodes.Conv_I8);
                }
            }

            var logParams = string.Join(",", method.GetParameters().Select(x => x.ParameterType.ToString()));
            logger.Log($"call {method.ReturnType} {TestedTypeFullName}::{method.Name}({logParams})");
            _ilGenerator.Emit(OpCodes.Call, _closedMethods[methodCallExpression.Name]);
            return methodCallExpression;
        }

        public override MethodArgumentVariableExpression VisitMethodArgument(
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

        public override FieldVariableExpression VisitField(FieldVariableExpression expression)
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

        public override VariableExpression VisitLocalVariable(LocalVariableExpression variable)
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