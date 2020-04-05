using System;
using System.Linq;
using Parser.Parser.Expressions;
using Parser.Parser.Statements;

namespace Parser.ILCompiler
{
    public class ExpressionVisitor
    {
        protected virtual IStatement VisitStatement(IStatement statement)
        {
            switch (statement.ExpressionType)
            {
                case ExpressionType.Assignment:
                    return VisitAssignment((AssignmentStatement) statement);
                case ExpressionType.Return:
                    return VisitReturn((ReturnStatement) statement);
                case ExpressionType.IfElse:
                    return VisitIfElse((IfElseStatement) statement);
                case ExpressionType.VoidMethodCallStatement:
                    return VisitVoidMethod((VoidMethodCallStatement) statement);
                case ExpressionType.Statement:
                    return VisitVoidMethod((VoidMethodCallStatement) statement);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual VoidMethodCallStatement VisitVoidMethod(VoidMethodCallStatement statement)
        {
            var method = VisitMethod(statement.Method);
            return new VoidMethodCallStatement(method);
        }

        protected virtual IExpression VisitExpression(IExpression expression)
        {
            return expression.ExpressionType switch
            {
                ExpressionType.LocalVariable => (IExpression) VisitLocalVariable((LocalVariableExpression) expression),
                ExpressionType.FieldVariable => (IExpression) VisitField((FieldVariableExpression) expression),
                ExpressionType.MethodArgVariable => (IExpression) VisitMethodArgument(
                    (MethodArgumentVariableExpression) expression),
                ExpressionType.Primary => VisitPrimary((PrimaryExpression) expression),
                ExpressionType.Binary => VisitBinary((BinaryExpression) expression),
                ExpressionType.Unary => VisitUnary((UnaryExpression) expression),
                ExpressionType.MethodCallParameter => VisitMethodCallParameter(
                    (MethodCallParameterExpression) expression),
                ExpressionType.MethodCallExpression => VisitMethod((MethodCallExpression) expression),
                ExpressionType.Logical => VisitLogical((LogicalBinaryExpression) expression),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        protected virtual MethodCallParameterExpression VisitMethodCallParameter(MethodCallParameterExpression exp)
        {
            var newExp = VisitExpression(exp.Expression);
            return new MethodCallParameterExpression(newExp, exp.ParameterInfo);
        }

        protected virtual MethodArgumentVariableExpression VisitMethodArgument(
            MethodArgumentVariableExpression expression)
        {
            return expression;
        }

        protected virtual FieldVariableExpression VisitField(FieldVariableExpression expression)
        {
            return expression;
        }

        protected virtual LogicalBinaryExpression VisitLogical(LogicalBinaryExpression logical)
        {
            return logical;
        }


        protected virtual Statement VisitStatement(Statement statement)
        {
            return new Statement(statement.Statements.Select(VisitStatement).ToArray());
        }

        protected virtual IfElseStatement VisitIfElse(IfElseStatement statement) => statement;


        protected virtual BinaryExpression VisitBinary(BinaryExpression binaryExpression)
        {
            var left = VisitExpression(binaryExpression.Left);
            var right = VisitExpression(binaryExpression.Right);
            return new BinaryExpression(left, right, binaryExpression.TokenType);
        }

        protected virtual ReturnStatement VisitReturn(ReturnStatement returnStatement)
        {
            VisitExpression(returnStatement.Returned);
            return returnStatement;
        }

        protected virtual AssignmentStatement VisitAssignment(AssignmentStatement assignmentStatement)
        {
            VisitExpression(assignmentStatement.Right);
            return assignmentStatement;
        }

        protected virtual IExpression VisitUnary(UnaryExpression unaryExpression)
        {
            var expression = VisitExpression(unaryExpression.Expression);
            return new UnaryExpression(expression, UnaryType.Negative);
        }

        protected virtual PrimaryExpression VisitPrimary(PrimaryExpression primaryExpression)
        {
            return primaryExpression;
        }

        protected virtual MethodCallExpression VisitMethod(MethodCallExpression methodCallExpression)
        {
            var expressions = methodCallExpression.Parameters.Select(VisitExpression)
                .Cast<MethodCallParameterExpression>()
                .ToArray();

            return new MethodCallExpression(methodCallExpression.Name, methodCallExpression.MethodInfo, expressions);
        }

        protected virtual VariableExpression VisitLocalVariable(LocalVariableExpression variable)
        {
            return variable;
        }
    }
}