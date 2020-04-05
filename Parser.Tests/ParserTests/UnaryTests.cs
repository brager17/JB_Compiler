using Xunit;

namespace Parser
{
    public class UnaryTests
    {
        [Fact]
        public void Parse__NotTypeTests__CorrectAst()
        {
            var expr = "!(x!=1)";
            var result = TestHelper.GetParseResultExpression(expr);
            Assert.Equal(ExpressionType.Unary, result.ExpressionType);
            Assert.Equal(UnaryType.Not, ((UnaryExpression)result).UnaryType);
        }
    }
}