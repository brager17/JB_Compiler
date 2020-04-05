using System;
using System.Linq;
using System.Reflection.Emit;
using Parser;

namespace Compiler
{
    public class ExpressionVisitor
    {
        public virtual IStatement VisitStatement(IStatement statement)
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

        public virtual VoidMethodCallStatement VisitVoidMethod(VoidMethodCallStatement statement)
        {
            var method = VisitMethod(statement.Method);
            return new VoidMethodCallStatement(method);
        }

        public virtual IExpression VisitExpression(IExpression expression)
        {
            return expression.ExpressionType switch
            {
                ExpressionType.LocalVariable => (IExpression) VisitLocalVariable((LocalVariableExpression) expression),
                ExpressionType.FieldVariable => (IExpression) VisitField((FieldVariableExpression) expression),
                ExpressionType.MethodArgVariable => (IExpression) VisitMethodArgument((MethodArgumentVariableExpression) expression),
                ExpressionType.Primary => VisitPrimary((PrimaryExpression) expression),
                ExpressionType.Binary => VisitBinary((BinaryExpression) expression),
                ExpressionType.Unary => VisitUnary((UnaryExpression) expression),
                ExpressionType.MethodCallExpression => VisitMethod((MethodCallExpression) expression),
                ExpressionType.Logical => VisitLogical((LogicalBinaryExpression) expression),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public virtual MethodArgumentVariableExpression VisitMethodArgument(MethodArgumentVariableExpression expression)
        {
            return expression;
        }

        public virtual FieldVariableExpression VisitField(FieldVariableExpression expression)
        {
            return expression;
        }

        public virtual LogicalBinaryExpression VisitLogical(LogicalBinaryExpression logical)
        {
            return logical;
        }


        public virtual Statement VisitStatement(Statement statement)
        {
            return new Statement(statement.Statements.Select(VisitStatement).ToArray());
        }

        public virtual IfElseStatement VisitIfElse(IfElseStatement statement) => statement;


        public virtual BinaryExpression VisitBinary(BinaryExpression binaryExpression)
        {
            var left = VisitExpression(binaryExpression.Left);
            var right = VisitExpression(binaryExpression.Right);
            return new BinaryExpression(left, right, binaryExpression.TokenType);
        }

        public virtual ReturnStatement VisitReturn(ReturnStatement returnStatement)
        {
            VisitExpression(returnStatement.Returned);
            return returnStatement;
        }

        public virtual AssignmentStatement VisitAssignment(AssignmentStatement assignmentStatement)
        {
            VisitExpression(assignmentStatement.Right);
            return assignmentStatement;
        }

        public virtual IExpression VisitUnary(UnaryExpression unaryExpression)
        {
            var expression = VisitExpression(unaryExpression.Expression);
            return new UnaryExpression(expression, UnaryType.Negative);
        }

        public virtual PrimaryExpression VisitPrimary(PrimaryExpression primaryExpression)
        {
            return primaryExpression;
        }

        public virtual MethodCallExpression VisitMethod(MethodCallExpression methodCallExpression)
        {
            var expressions = methodCallExpression.Parameters.Select(VisitExpression).ToList();
            return new MethodCallExpression(methodCallExpression.Name, expressions);
        }

        public virtual VariableExpression VisitLocalVariable(LocalVariableExpression variable)
        {
            return variable;
        }
    }
}