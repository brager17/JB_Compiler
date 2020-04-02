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
            var expr = "if (x == 12) {int t = 33;}";
            var result = TestHelper.GetParseResultStatements(expr);

            Assert.Equal(ExpressionType.IfElse, result[0].ExpressionType);
            var ifElseStatement = (IfElseStatement) result[0];
            Assert.Equal(ExpressionType.Variable, ifElseStatement.Test.Left.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ifElseStatement.Test.Right.ExpressionType);
            Assert.Equal(ExpressionType.Assignment, ifElseStatement.IfTrue.ExpressionType);
            var assignment = (AssignmentStatement) ((IfElseStatement) result[0]).IfTrue;
            Assert.Equal("t", assignment.Left.Name);
            Assert.Equal(ExpressionType.Primary, assignment.Right.ExpressionType);
            Assert.Equal(33, ((PrimaryExpression) assignment.Right).AsInt());
        }
    }
}