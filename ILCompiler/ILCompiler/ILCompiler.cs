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
        private IlEmitter _IlEmitter;

        public CompileExpressionVisitor(ILGenerator ilGenerator)
        {
            _IlEmitter = new IlEmitter(ilGenerator);
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
                _IlEmitter.DeclareLocal(localVariable.Value);
            }

            foreach (var statement in statements.Statements)
            {
                VisitStatement(statement);
            }

            return _IlEmitter._logger.GetLogs;
        }

        public string[] Start(IExpression expression)
        {
            VisitExpression(expression);
            _IlEmitter.Emit(OpCodes.Ret);
            return _IlEmitter._logger.GetLogs;
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
            else if (expression.TryCast<MethodCallExpression>(out var methodCallExpression))
            {
                result = new LogicalBinaryExpression(methodCallExpression, new PrimaryExpression("true"), Operator.Eq);
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

        public void VisitLogicalExpression(IExpression expression, bool successLabel, Label label)
        {
            var prepared = PrepareForConditionalIfNeeded(expression);
            if (Operator.Arithmetic.HasFlag(prepared.Operator))
            {
                VisitExpression(prepared.Left);
                VisitExpression(prepared.Right);
                _IlEmitter.EmitLogicalOperator(prepared);
                if (successLabel) _IlEmitter.BrTrue(label);
                else _IlEmitter.BrFalse(label);
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
            VisitLogicalExpression(exp.Left, successLabel, label);
            VisitLogicalExpression(exp.Right, successLabel, label);
        }

        public void VisitAsAnd(LogicalBinaryExpression exp, bool successLabel, Label label)
        {
            var afterIf = _IlEmitter.DefineLabel();
            VisitLogicalExpression(exp.Left, !successLabel, afterIf);
            VisitLogicalExpression(exp.Right, successLabel, label);
            _IlEmitter.MarkLabel(afterIf, nameof(afterIf));
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
                        Label @else = _IlEmitter.DefineLabel();
                        Label @end = _IlEmitter.DefineLabel();
                        VisitLogicalExpression(left, false, @else);
                        VisitExpression(right);
                        _IlEmitter.Br(end, nameof(end));
                        _IlEmitter.MarkLabel(@else, nameof(@else));
                        _IlEmitter.Emit(OpCodes.Ldc_I4_0);
                        _IlEmitter.MarkLabel(end, nameof(end));
                        break;
                    case Operator.Or:
                        @else = _IlEmitter.DefineLabel();
                        @end = _IlEmitter.DefineLabel();
                        VisitLogicalExpression(left, false, @else);
                        _IlEmitter.Emit(OpCodes.Ldc_I4_1);
                        _IlEmitter.Br(end, nameof(end));
                        _IlEmitter.MarkLabel(@else, nameof(@else));
                        VisitExpression(right);
                        _IlEmitter.MarkLabel(end, nameof(end));
                        break;
                }
            }
            else
            {
                VisitExpression(logical.Left);
                VisitExpression(logical.Right);
                _IlEmitter.EmitLogicalOperator(logical);
            }

            return logical;
        }

        protected override IfElseStatement VisitIfElse(IfElseStatement statement)
        {
            VisitExpression(statement.Test);
            var @startEnd = _IlEmitter.DefineLabel();
            var @elseStart = statement.Else == null ? startEnd : _IlEmitter.DefineLabel();
            _IlEmitter.BrFalseS(elseStart, nameof(elseStart));
            VisitStatement(statement.IfTrue);
            if (!statement.IfTrue.IsReturnStatement)
            {
                _IlEmitter.Br(startEnd, nameof(startEnd));
            }

            if (statement.Else != null)
            {
                _IlEmitter.MarkLabel(elseStart, nameof(elseStart));
                VisitStatement(statement.Else);
                _IlEmitter.MarkLabel(startEnd, nameof(startEnd));
            }
            else
            {
                _IlEmitter.MarkLabel(startEnd, nameof(startEnd));
            }


            return statement;
        }


        protected override BinaryExpression VisitBinary(BinaryExpression binaryExpression)
        {
            VisitExpression(binaryExpression.Left);
            if (binaryExpression.Left.ReturnType == CompilerType.Int &&
                binaryExpression.Right.ReturnType == CompilerType.Long)
            {
                _IlEmitter.Emit(OpCodes.Conv_I8);
            }

            VisitExpression(binaryExpression.Right);
            if (binaryExpression.Left.ReturnType == CompilerType.Long &&
                binaryExpression.Right.ReturnType == CompilerType.Int)
            {
                _IlEmitter.Emit(OpCodes.Conv_I8);
            }

            _IlEmitter.LoadOperation(binaryExpression.TokenType);
            return binaryExpression;
        }

        protected override ReturnStatement VisitReturn(ReturnStatement returnStatement)
        {
            VisitExpression(returnStatement.Returned);
            _IlEmitter.Emit(OpCodes.Ret);
            return returnStatement;
        }

        protected override AssignmentStatement VisitAssignment(AssignmentStatement assignmentStatement)
        {
            VisitExpression(assignmentStatement.Right);
            if (assignmentStatement.Left.TryCast<FieldVariableExpression>(out var field))
            {
                _IlEmitter.SetField(field);
            }
            else if (assignmentStatement.Left.TryCast<LocalVariableExpression>(out var localVariableExpression))
            {
                _IlEmitter.SetLocalVariable(localVariableExpression);
            }
            else if (assignmentStatement.Left.TryCast<MethodArgumentVariableExpression>(out var methodArg))
            {
                _IlEmitter.SetArg(methodArg);
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
                            _IlEmitter.LdcI8(long.Parse(longMinValue));
                        }
                        else if (pr.Value == intMinValue[1..])
                        {
                            expression = pr;
                            _IlEmitter.LdcI4(int.Parse(intMinValue));
                        }
                        else
                        {
                            expression = VisitExpression(unaryExpression.Expression);
                            _IlEmitter.Emit(OpCodes.Neg);
                        }
                    }
                    else
                    {
                        expression = VisitExpression(unaryExpression.Expression);
                        _IlEmitter.Emit(OpCodes.Neg);
                    }

                    return new UnaryExpression(expression, UnaryType.Negative);
                }

                case UnaryType.Not:
                {
                    var expression = VisitExpression(unaryExpression.Expression);

                    _IlEmitter.Emit(OpCodes.Ldc_I4_0);
                    _IlEmitter.Emit(OpCodes.Ceq);
                    return new UnaryExpression(expression, UnaryType.Not);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void IntEmit(int value)
        {
            _IlEmitter.LoadInt(value);
        }

        protected override PrimaryExpression VisitPrimary(PrimaryExpression primaryExpression)
        {
            switch (primaryExpression.ReturnType)
            {
                case CompilerType.Long:
                    _IlEmitter.LdcI8(primaryExpression.AsLong());
                    break;
                case CompilerType.Int:
                    _IlEmitter.LoadInt(primaryExpression.AsInt());
                    break;
                case CompilerType.Bool:
                    var value = primaryExpression.AsBool();
                    _IlEmitter.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
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
                    _IlEmitter.Emit(OpCodes.Conv_I8);
                }
            }

            _IlEmitter.MethodCall(methodCallExpression);
            return methodCallExpression;
        }

        protected override MethodArgumentVariableExpression VisitMethodArgument(
            MethodArgumentVariableExpression expression)
        {
            if (expression.ByReference)
            {
                _IlEmitter.LoadArgByReference(expression);
                return expression;
            }

            _IlEmitter.LoadArg(expression);

            return expression;
        }

        protected override FieldVariableExpression VisitField(FieldVariableExpression expression)
        {
            if (expression.ByReference)
            {
                _IlEmitter.ldsfldByReference(expression);
                return expression;
            }

            _IlEmitter.ldsfld(expression);
            return expression;
        }

        protected override VariableExpression VisitLocalVariable(LocalVariableExpression variable)
        {
            if (variable.ByReference)
            {
                _IlEmitter.LdLocByReference(variable.Index);
                return variable;
            }

            _IlEmitter.LdLoc(variable.Index);
            return variable;
        }
    }
}