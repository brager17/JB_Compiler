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

            var parseResult = GetParseResult(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(6, ((PrimaryExpression) parseResult).Value);
        }

        [Fact]
        public void Test1()
        {
            var expr = "1-2+3";

            var parseResult = GetParseResult(expr);

            // var visitor = new ConstantFoldingVisitor();
            // var result = visitor.Visit(parseResult);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(2, ((PrimaryExpression) parseResult).Value);
        }


        [Fact]
        public void Test3()
        {
            var expr = "1-((-(-2))+3-1)";

            var parseResult = GetParseResult(expr);

            Assert.Equal(ExpressionType.Unary, parseResult.ExpressionType);
            Assert.Equal(-3, -((PrimaryExpression) ((UnaryExpression) parseResult).Expression).Value);
        }


        [Fact]
        public void Test4()
        {
            var expr = "1-(1-(1-(1-(1-(1-0)))))";

            var parseResult = GetParseResult(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(0, ((PrimaryExpression) parseResult).Value);
        }

        [Fact]
        public void Test5()
        {
            var expr = "1*1*1*1*1*1*1*1*1*1";

            var parseResult = GetParseResult(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(1, ((PrimaryExpression) parseResult).Value);
        }


        [Fact]
        public void Test6()
        {
            var expr = "1/1/1/1*1*1";

            var parseResult = GetParseResult(expr);

            Assert.Equal(ExpressionType.Primary, parseResult.ExpressionType);
            Assert.Equal(1, ((PrimaryExpression) parseResult).Value);
        }


        [Fact]
        public void Test7()
        {
            var expr = "1*x";

            var parseResult = GetParseResult(expr);

            Assert.Equal(ExpressionType.Variable, parseResult.ExpressionType);
            Assert.Equal("x", ((VariableExpression) parseResult).Name);
        }

        [Fact]
        public void Test8()
        {
            var expr = "x/1";

            var parseResult = GetParseResult(expr);

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
            Assert.Throws<DivideByZeroException>(() => GetParseResult(expr));
        }

        [InlineData("x/0")]
        [InlineData("x/0")]
        [InlineData("(x*y-1)/0")]
        [Theory]
        public void NoDivideByZero(string expr)
        {
            var r = GetParseResult(expr);
        }

        // roslyn doesn't generate exception in cases
        [InlineData("(12*13+14)/(0*(x+y+z))")]
        [InlineData("(12*13+14-1)/(0*x)")]
        [InlineData("(12-15)/(0*((y+12)))")]
        // but generates in cases: "12/0',"12/(1+2-3),"12/(1*2*3*0)"
        [Theory]
        public void Parse__ExpressionWithFoldingExprAfterMultiBy0__NotDivisionBy0CompileTimException(string expr)
        {
            GetParseResult(expr);
        }

        
        IExpression GetParseResult(string expression)
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.ReadAll();
            return new Parser(tokens).Parse().Single();
        }
    }
}