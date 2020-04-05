using Parser.Parser.Expressions;
using Parser.Utils;
using Xunit;

namespace Parser.Tests.ParserTests
{
    public class ConstantFoldingTests
    {
        [Fact]
        public void Test()
        {
            var expr = "1+2+3";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(6, ((PrimaryExpression) parseResult).AsLong());
        }


        [Fact]
        public void Test3()
        {
            var expr = "1-((-(-2))+3-1)";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Unary, parseResult.ExpressionType);
            Assert.Equal(-3, -((PrimaryExpression) ((UnaryExpression) parseResult).Expression).AsLong());
        }


        [Fact]
        public void Test4()
        {
            var expr = "1-(1-(1-(1-(1-(1-0)))))";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(0, ((PrimaryExpression) parseResult).AsLong());
        }

        [Fact]
        public void Test5()
        {
            var expr = "1*1*1*1*1*1*1*1*1*1";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(1, ((PrimaryExpression) parseResult).AsLong());
        }


        [Fact]
        public void Test6()
        {
            var expr = "1/1/1/1*1*1";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(1, ((PrimaryExpression) parseResult).AsLong());
        }


        [Fact]
        public void Test7()
        {
            var expr = "1*x";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.MethodArgVariable, parseResult.ExpressionType);
            Assert.Equal("x", ((VariableExpression) parseResult).Name);
        }

        [Fact]
        public void Test8()
        {
            var expr = "x/1";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.MethodArgVariable, parseResult.ExpressionType);
            Assert.Equal("x", ((VariableExpression) parseResult).Name);
        }


        // roslyn doesn't generate exception in cases
        [InlineData("(12*13+14)/(0*(x+y+z))")]
        [InlineData("(12*13+14-1)/(0*x)")]
        [InlineData("(12-15)/(0*((y+12)))")]
        // but generates in cases: "12/0',"12/(1+2-3),"12/(1*2*3*0)"
        [Theory]
        public void Parse__ExpressionWithFoldingExprAfterMultiBy0__NotDivisionBy0CompileTimException(string expr)
        {
            TestHelper.GetParseResultExpression(expr);
        }

        [Fact]
        public void Parse__Multi0__DoesntConstantFold()
        {
            var p = TestHelper.GetParseResultExpression("0*(x-4)");

            Assert.NotEqual(p.ExpressionType, ExpressionType.Primary);
        }
    }
}