using System;
using System.Collections.Generic;
using System.Linq;
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
                    break;
                case ExpressionType.Return:
                    return VisitReturn((ReturnStatement) statement);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public virtual IExpression VisitExpression(IExpression expression)
        {
            switch (expression.ExpressionType)
            {
                case ExpressionType.Variable:
                    return VisitVariable((VariableExpression) expression);
                case ExpressionType.Primary:
                    return VisitPrimary((PrimaryExpression) expression);
                case ExpressionType.Binary:
                    return VisitBinary((BinaryExpression) expression);
                case ExpressionType.Unary:
                    return VisitUnary((UnaryExpression) expression);
                case ExpressionType.MethodCall:
                    return VisitMethod((MethodCallExpression) expression);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


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

        public virtual UnaryExpression VisitUnary(UnaryExpression unaryExpression)
        {
            var expression = VisitExpression(unaryExpression.Expression);
            return new UnaryExpression(expression);
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

        public virtual VariableExpression VisitVariable(VariableExpression variable)
        {
            return variable;
        }
    }
}