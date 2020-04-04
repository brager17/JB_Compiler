using System.Reflection;
using Compiler;
using Xunit;

namespace Parser
{
    public class IfElseStatementTest
    {
        [Fact]
        public void SimpleIf()
        {
            var expr = "if (x == 12) {int t = 33;}return 1;";
            var result = TestHelper.GetParseResultStatements(expr);

            Assert.Equal(ExpressionType.IfElse, result[0].ExpressionType);
            var ifElseStatement = (IfElseStatement) result[0];
            Assert.Equal(ExpressionType.Variable, ((LogicalBinaryExpression)ifElseStatement.Test).Left.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((LogicalBinaryExpression)ifElseStatement.Test).Right.ExpressionType);
            Assert.Equal(ExpressionType.Assignment, ((IfElseStatement) result[0]).IfTrue.Statements[0].ExpressionType);
            var assignment = (AssignmentStatement) ((IfElseStatement) result[0]).IfTrue.Statements[0];
            Assert.Equal("t", assignment.Left.Name);
            Assert.Equal(ExpressionType.Primary, assignment.Right.ExpressionType);
            Assert.Equal(33, ((PrimaryExpression) assignment.Right).AsInt());
        }

        [Fact]
        public void SimpleIfElse()
        {
            var expr = "if (x == 12) {int t = 33;}else{int q = 12;} return 1;";
            var result = TestHelper.GetParseResultStatements(expr);

            Assert.Equal(ExpressionType.IfElse, result[0].ExpressionType);
            var ifElseStatement = (IfElseStatement) result[0];
            Assert.Equal(ExpressionType.Variable, ((LogicalBinaryExpression)ifElseStatement.Test).Left.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((LogicalBinaryExpression)ifElseStatement.Test).Right.ExpressionType);
            Assert.Equal(ExpressionType.Assignment, ((IfElseStatement) result[0]).Else.Statements[0].ExpressionType);

            var ifTrue = (AssignmentStatement) ((IfElseStatement) result[0]).IfTrue.Statements[0];
            Assert.Equal("t", ifTrue.Left.Name);
            Assert.Equal(ExpressionType.Primary, ifTrue.Right.ExpressionType);
            Assert.Equal(33, ((PrimaryExpression) ifTrue.Right).AsInt());

            var ifFalse = (AssignmentStatement) ((IfElseStatement) result[0]).Else.Statements[0];
            Assert.Equal("q", ifFalse.Left.Name);
            Assert.Equal(ExpressionType.Primary, ifFalse.Right.ExpressionType);
            Assert.Equal(12, ((PrimaryExpression) ifFalse.Right).AsInt());
        }
    }
}