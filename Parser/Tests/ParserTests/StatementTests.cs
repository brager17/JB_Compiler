using System.Linq;
using System.Reflection;
using Xunit;

namespace Parser
{
    public class StatementTests
    {
        [Fact]
        public void Parse__DefineVariable__Correct()
        {
            string expr = "long q = 12;long w = -14;return q+w;";
            var result = TestHelper.GetParseResultStatements(expr);

            var logs = TestHelper.GeneratedStatementsMySelf(expr, out var func, GetType());
            var roslyn = TestHelper.GeneratedRoslyn("q+w",
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Fact]
        public void Statement()
        {
            long n = 123L;
            int m = 123;
        }

        [Fact]
        public void Parse__DefineVariablesWithExpression__Correct()
        {
            string expr = @"
                    long q = 12*x;
                    long w = -14+12;";
            var result = TestHelper.GetParseResultStatements(expr);


            Assert.Equal(ExpressionType.Assignment, result[0].ExpressionType);
            var qAssignment = (AssignmentStatement) result[0];
            Assert.Equal("q", qAssignment.Left.Name);
            Assert.Equal(ExpressionType.Binary, qAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((BinaryExpression) qAssignment.Right).Left.ExpressionType);
            Assert.Equal(12, ((PrimaryExpression) ((BinaryExpression) qAssignment.Right).Left).LongValue);
            Assert.Equal(ExpressionType.Variable, ((BinaryExpression) qAssignment.Right).Right.ExpressionType);
            Assert.Equal("x", ((VariableExpression) ((BinaryExpression) qAssignment.Right).Right).Name);

            Assert.Equal(ExpressionType.Assignment, result[1].ExpressionType);
            var qqAssignment = (AssignmentStatement) result[1];
            Assert.Equal("w", qqAssignment.Left.Name);
            Assert.Equal(ExpressionType.Unary, qqAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((UnaryExpression) qqAssignment.Right).Expression.ExpressionType);
            Assert.Equal(2, ((PrimaryExpression) ((UnaryExpression) qqAssignment.Right).Expression).LongValue);
        }


        [Fact]
        public void Parse__DefineVariablesWithExpressionUsingBeforeDefinedVariable__Correct()
        {
            string expr = @"
                    long q = 12*x;
                    long w = -q;";
            var result = TestHelper.GetParseResultStatements(expr);

            Assert.Equal(ExpressionType.Assignment, result[0].ExpressionType);
            var qAssignment = (AssignmentStatement) result[0];
            Assert.Equal("q", qAssignment.Left.Name);
            Assert.Equal(ExpressionType.Binary, qAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((BinaryExpression) qAssignment.Right).Left.ExpressionType);
            Assert.Equal(12, ((PrimaryExpression) ((BinaryExpression) qAssignment.Right).Left).LongValue);
            Assert.Equal(ExpressionType.Variable, ((BinaryExpression) qAssignment.Right).Right.ExpressionType);
            Assert.Equal("x", ((VariableExpression) ((BinaryExpression) qAssignment.Right).Right).Name);

            Assert.Equal(ExpressionType.Assignment, result[1].ExpressionType);
            var qqAssignment = (AssignmentStatement) result[1];
            Assert.Equal("w", qqAssignment.Left.Name);
            Assert.Equal(ExpressionType.Unary, qqAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Variable, ((UnaryExpression) qqAssignment.Right).Expression.ExpressionType);
            Assert.Equal("q", ((VariableExpression) ((UnaryExpression) qqAssignment.Right).Expression).Name);
        }
    }
}