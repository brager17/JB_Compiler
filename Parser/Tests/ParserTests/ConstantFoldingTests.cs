using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Parser
{
    public class ConstantFoldingTests
    {
        [Fact]
        public void Test()
        {
            var expr = "1+2+3";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(6, ((PrimaryExpression) parseResult).LongValue);
        }

        [Fact]
        public void Test1()
        {
            var expr = "1-2+3";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            // var visitor = new ConstantFoldingVisitor();
            // var result = visitor.Visit(parseResult);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(2, ((PrimaryExpression) parseResult).LongValue);
        }


        [Fact]
        public void Test3()
        {
            var expr = "1-((-(-2))+3-1)";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Unary, parseResult.ExpressionType);
            Assert.Equal(-3, -((PrimaryExpression) ((UnaryExpression) parseResult).Expression).LongValue);
        }


        [Fact]
        public void Test4()
        {
            var expr = "1-(1-(1-(1-(1-(1-0)))))";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(0, ((PrimaryExpression) parseResult).LongValue);
        }

        [Fact]
        public void Test5()
        {
            var expr = "1*1*1*1*1*1*1*1*1*1";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(1, ((PrimaryExpression) parseResult).LongValue);
        }


        [Fact]
        public void Test6()
        {
            var expr = "1/1/1/1*1*1";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(1, ((PrimaryExpression) parseResult).LongValue);
        }


        [Fact]
        public void Test7()
        {
            var expr = "1*x";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Variable, parseResult.ExpressionType);
            Assert.Equal("x", ((VariableExpression) parseResult).Name);
        }

        [Fact]
        public void Test8()
        {
            var expr = "x/1";

            var parseResult = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Variable, parseResult.ExpressionType);
            Assert.Equal("x", ((VariableExpression) parseResult).Name);
        }

        [Theory]
        [InlineData("1/0")]
        [InlineData("x*y-1/0")]
        [InlineData("0/0")]
        [InlineData("(1+0-132)/(-12+13-1)")]
        public void Parse__DivideByZero__ThrowDivideByZeroException(string expr)
        {
            Assert.Throws<DivideByZeroException>(() => TestHelper.GetParseResultExpression(expr));
        }

        [InlineData("x/0")]
        [InlineData("x/0")]
        [InlineData("(x*y-1)/0")]
        [Theory]
        public void NoDivideByZero(string expr)
        {
            var r = TestHelper.GetParseResultExpression(expr);
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

        // [InlineData(
        // "x+684451365090141806*x/y+3/y*z+z/6/(6)/4+x*6-((6/(6*(x+9/z)+y-3-z/5-z/7/x*5)-7)+0*(7/y-4/(0+(4/(3/x)))))")]
        // [Theory]
        [Fact]
        public void Parse__Multi0__DoesntConstantFold()
        {
            var p = TestHelper.GetParseResultExpression("0*(x-4)");

            Assert.NotEqual(p.ExpressionType, ExpressionType.Primary);
        }
    }
}