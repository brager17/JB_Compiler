using System.Collections.Generic;
using Newtonsoft.Json;
using Parser.Lexer;
using Parser.Parser.Exceptions;
using Parser.Parser.Expressions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Parser.Tests.ParserTests.BinaryExpressionsParsingTests
{
    public class NegativeBinaryExpressionParsingTests
    {
        [Theory]
        [InlineData("x*true")]
        [InlineData("x/true")]
        [InlineData("x+true")]
        [InlineData("x-true")]
        [InlineData("true+true")]
        [InlineData("true*true")]
        [InlineData("true/true")]
        [InlineData("true-true")]
        public void Parse__MismatchArgumentsWithArithmeticOperation__ThrowError(string expression)
        {
            var ex = Assert.Throws<CompileException>(() => TestHelper.GetParseResultExpression(expression));
            Assert.Contains("Invalid arithmetic operation", ex.Message);
        }

        [Theory]
        [InlineData("1/0")]
        [InlineData("x*y-1/0")]
        [InlineData("0/0")]
        [InlineData("(1+0-132)/(-12+13-1)")]
        public void Parse__DivideByZero__ThrowDivideByZeroException(string expr)
        {
            var ex = Assert.Throws<CompileException>(() => TestHelper.GetParseResultExpression(expr));
            Assert.Contains("Divide by zero", ex.Message);
        }

        // there are csharp compiler rules
        [InlineData("x/0")]
        [InlineData("x/0")]
        [InlineData("(x*y-1)/0")]
        [Theory]
        public void NoDivideByZero(string expr)
        {
            TestHelper.GetParseResultExpression(expr);
        }
    }

    public class PositiveBinaryExpressionParsing
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public PositiveBinaryExpressionParsing(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private void JsonAssert(object expected, object actual)
        {
            if (JsonConvert.SerializeObject(expected) != JsonConvert.SerializeObject(actual))
                throw new EqualException(expected, actual);
        }

        [Fact]
        public void Parse__Sub__CorrectAst()
        {
            var result = TestHelper.GetParseResultExpression("1-13", false);

            JsonAssert(
                new BinaryExpression(new PrimaryExpression("1"), new PrimaryExpression("13"), TokenType.Minus),
                result);
        }

        [Fact]
        public void Parse__Plus1Plus1Div13__CorrectAst()
        {
            var result = TestHelper.GetParseResultExpression("1+1/13", false);

            JsonAssert(
                new BinaryExpression(
                    new PrimaryExpression("1"),
                    new BinaryExpression(new PrimaryExpression("1"), new PrimaryExpression("13"), TokenType.Slash),
                    TokenType.Plus),
                result
            );
        }


        [Fact]
        public void Parse__ExpressionWithParenhess__CorrectAst()
        {
            var result = TestHelper.GetParseResultExpression("(2+2)*2", false);

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(new PrimaryExpression("2"), new PrimaryExpression("2"), TokenType.Plus),
                    new PrimaryExpression("2"), TokenType.Star)
                , result);
        }

      

        [Fact]
        public void Parse__ExpressionWithVariable__CorrectAst()
        {
            var result = TestHelper.GetParseResultExpression("(x+1)*y+z");

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(
                        new BinaryExpression(new MethodArgumentVariableExpression("x", CompilerType.Long, 0),
                            new PrimaryExpression("1"),
                            TokenType.Plus),
                        new MethodArgumentVariableExpression("y", CompilerType.Long, 1), TokenType.Star),
                    new MethodArgumentVariableExpression("z", CompilerType.Long, 2), TokenType.Plus)
                , result);
        }

        [Fact]
        public void Parse__ExpressionWithMethodCall__CorrectAst()
        {
            var methods = new Dictionary<string, (CompilerType[] parameters, CompilerType @return)>()
            {
                {
                    "Method",
                    (new[] {CompilerType.Long, CompilerType.Long, CompilerType.Long, CompilerType.Long},
                        CompilerType.Long)
                }
            };

            var result = TestHelper.GetParseResultExpression("(x+1)*y+z+Method(1,x,14,-1)", methods: methods);

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(new MethodArgumentVariableExpression("x", CompilerType.Long, 0),
                                new PrimaryExpression("1"),
                                TokenType.Plus),
                            new MethodArgumentVariableExpression("y", CompilerType.Long, 1), TokenType.Star),
                        new MethodArgumentVariableExpression("z", CompilerType.Long, 2), TokenType.Plus),
                    new MethodCallExpression("Method",
                        new List<IExpression>()
                        {
                            new PrimaryExpression("1"), new MethodArgumentVariableExpression("x", CompilerType.Long, 0),
                            new PrimaryExpression("14"),
                            new UnaryExpression(new PrimaryExpression("1"), UnaryType.Negative)
                        }
                    ),
                    TokenType.Plus
                )
                , result);
        }

        [Fact]
        public void Parse__ExpressionWithCall2DifferentMethods__CorrectAst()
        {
            var methods = new Dictionary<string, (CompilerType[] parameters, CompilerType @return)>()
            {
                {"M", (new[] {CompilerType.Int}, CompilerType.Long)},
                {"M1", (new[] {CompilerType.Long}, CompilerType.Long)},
            };
            var result = TestHelper.GetParseResultExpression("M(1) + M1(x)", methods: methods);

            JsonAssert(
                new BinaryExpression(
                    new MethodCallExpression("M", new[] {new PrimaryExpression("1"),}),
                    new MethodCallExpression("M1",
                        new[] {new MethodArgumentVariableExpression("x", CompilerType.Long, 0)}),
                    TokenType.Plus)
                , result);
        }


        [Fact]
        public void TrueTest()
        {
            var expr = "if(true){} return 1;";
            var result = TestHelper.GetParseResultStatements(expr);
            Assert.Equal("true", ((PrimaryExpression) ((IfElseStatement) result[0]).Test).Value);
        }


        [Fact]
        public void FalseTest()
        {
            var expr = "if(false){} return 1;";
            var result = TestHelper.GetParseResultStatements(expr);
            Assert.Equal("false", ((PrimaryExpression) ((IfElseStatement) result[0]).Test).Value);
        }


        [Fact]
        public void Test11()
        {
            var expr =
                @"2*(9503-(128+y*z+40*y-480*x/2803+(35+y*0))*x)*529831844*z+40/(5+x)*z*9/z*9*z-37136330941/y+971/x-69";
            var x = 2040216428;
            var y = 274473045;
            var z = 25132344;

            var t = Compiler.CompileExpression(expr, out var func);
            var tt = TestHelper.GeneratedRoslynExpression(expr, out var roslynFunc);

            Assert.Equal(roslynFunc(x, y, z), func(x, y, z));
        }
        
        //negative
        [Fact]
        public void Parse__HasExtraBracket__ThrowCompileExc()
        {
            var @exception = Assert.Throws<CompileException>(() => TestHelper.GetParseResultExpression("(2+2))*2"));
            Assert.Contains(exception.Message, "Expression is incorrect");
        }
    }
}