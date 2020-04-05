using Parser.Parser.Expressions;
using Parser.Utils;
using Xunit;

namespace Parser.Tests.ParserTests
{
    public class ConstantFoldingTests
    {
        [Fact]
        public void Parse__SumThreeNumbers__Folded()
        {
            var expr = "1+2+3";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(6, ((PrimaryExpression) parseResult).AsLong());
        }


        [Fact]
        public void Parse__UsingUnaryOps__Folded()
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
        public void Parse__1MulWithVariable__FoldedToVariable()
        {
            var expr = "1*x";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.MethodArgVariable, parseResult.ExpressionType);
            Assert.Equal("x", ((VariableExpression) parseResult).Name);
        }

        [Fact]
        public void Parse__VariableDiv1__FoldedToVariable()
        {
            var expr = "x/1";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.MethodArgVariable, parseResult.ExpressionType);
            Assert.Equal("x", ((VariableExpression) parseResult).Name);
        }

        

        [Fact]
        // roslyn will count x-4 
        public void Parse__Multi0__DoesntConstantFold()
        {
            var p = TestHelper.GetParseResultExpression("0*(x-4)");

            Assert.NotEqual(p.ExpressionType, ExpressionType.Primary);
        }
    }
}