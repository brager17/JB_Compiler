using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Parser
{
    public class LexerTests
    {
        [Fact]
        public void ReadAll__BracketNumBracket__LexerDoesntReturnMethodCallExpression()
        {
            var randomExpr = @"x *(8 * (5))";
            var lexer = new Lexer(randomExpr);
            var tokens = lexer.ReadAll();
            Assert.Equal(tokens[0].Type, TokenType.Variable);
            Assert.Equal(tokens[0].Value, "x");
            Assert.Equal(tokens[1].Type, TokenType.Star);
            Assert.Equal(tokens[2].Type, TokenType.OpeningBracket);
            Assert.Equal(tokens[3].Type, TokenType.Num);
            Assert.Equal(tokens[3].Value, "8");
            Assert.Equal(tokens[4].Type, TokenType.Star);
            Assert.Equal(tokens[5].Type, TokenType.OpeningBracket);
            Assert.Equal(tokens[6].Type, TokenType.Num);
            Assert.Equal(tokens[6].Value, "5");
            Assert.Equal(tokens[7].Type, TokenType.ClosingBracket);
            Assert.Equal(tokens[8].Type, TokenType.ClosingBracket);
        }

        [Fact]
        public void ReadAll__ConstantSum__Correct()
        {
            var expr = "int.MaxValue + long.MaxValue";
            var lexer = new Lexer(expr);
            var tokens = lexer.ReadAll();

            Assert.Equal(TokenType.Constant, tokens[0].Type);
            Assert.Equal("int.MaxValue", tokens[0].Value);
            Assert.Equal(TokenType.Plus, tokens[1].Type);
            Assert.Equal(TokenType.Constant, tokens[2].Type);
            Assert.Equal("long.MaxValue", tokens[2].Value);
        }

        [Fact]
        public void ReadAll__ConstantMul__Correct()
        {
            var expr = "(int.MaxValue + long.MaxValue)*(int.MinValue*long.MinValue)";
            var lexer = new Lexer(expr);
            var tokens = lexer.ReadAll();

            Assert.Equal(TokenType.Constant, tokens[1].Type);
            Assert.Equal(TokenType.Constant, tokens[3].Type);
            Assert.Equal(TokenType.Constant, tokens[7].Type);
            Assert.Equal(TokenType.Constant, tokens[9].Type);
        }

        [Fact]
        public void ReadAll__ConstantMulWithExcessBrackets__Correct()
        {
            var expr = "(int.MaxValue + (long.MaxValue))*(int.MinValue*long.MinValue)";
            var lexer = new Lexer(expr);
            var tokens = lexer.ReadAll();

            Assert.Equal(TokenType.Constant, tokens[1].Type);
            Assert.Equal(TokenType.Constant, tokens[4].Type);
            Assert.Equal(TokenType.Constant, tokens[9].Type);
            Assert.Equal(TokenType.Constant, tokens[11].Type);
        }

        [InlineData("Method()")]
        [Theory]
        public void ReadAll__Methods__Correct(string expr)
        {
            var result = GetLexerResult(expr);

            Assert.Equal(result[0].Type, TokenType.Word);
            Assert.Equal(result[0].Value, "Method");
            Assert.Equal(result[1].Type, TokenType.OpeningBracket);
            Assert.Equal(result[2].Type, TokenType.ClosingBracket);
        }

        [Fact]
        public void ReadAll__MethodWithParameters__Correct()
        {
            var result = GetLexerResult("Method(1,2,3)");

            Assert.Equal(result[0].Type, TokenType.Word);
            Assert.Equal(result[0].Value, "Method");
            Assert.Equal(result[1].Type, TokenType.OpeningBracket);
            Assert.Equal(result[2].Type, TokenType.Num);
            Assert.Equal(result[2].Value, "1");
            Assert.Equal(result[3].Type, TokenType.Comma);
            Assert.Equal(result[4].Type, TokenType.Num);
            Assert.Equal(result[4].Value, "2");
            Assert.Equal(result[5].Type, TokenType.Comma);
            Assert.Equal(result[6].Type, TokenType.Num);
            Assert.Equal(result[6].Value, "3");
            Assert.Equal(result[7].Type, TokenType.ClosingBracket);
        }

        [Fact]
        public void ReadAll__MethodWithExpressionParameter__Correct()
        {
            var result = GetLexerResult("Method(1+x)");

            Assert.Equal(TokenType.Word, result[0].Type);
            Assert.Equal("Method", result[0].Value);
            Assert.Equal(TokenType.OpeningBracket, result[1].Type);
            Assert.Equal(TokenType.Num, result[2].Type);
            Assert.Equal("1", result[2].Value);
            Assert.Equal(TokenType.Plus, result[3].Type);
            Assert.Equal(TokenType.Variable, result[4].Type);
            Assert.Equal("x", result[4].Value);
            Assert.Equal(TokenType.ClosingBracket, result[5].Type);
        }

        [Fact]
        public void Lexer__Correct()
        {
            var expr = "x/y/z/a";
            var r = GetLexerResult(expr);
            Assert.Equal(TokenType.Variable, r[0].Type);
            Assert.Equal("x", r[0].Value);
            Assert.Equal(TokenType.Slash, r[1].Type);
            Assert.Equal(TokenType.Variable, r[2].Type);
            Assert.Equal("y", r[2].Value);
            Assert.Equal(TokenType.Slash, r[3].Type);
            Assert.Equal(TokenType.Variable, r[4].Type);
            Assert.Equal("z", r[4].Value);
            Assert.Equal(TokenType.Slash, r[5].Type);
            Assert.Equal(TokenType.Variable, r[6].Type);
            Assert.Equal("a", r[6].Value);
        }

        [Fact]
        public void NestedMethods()
        {
            var expr = "Method(1+Method())";
            var r = GetLexerResult(expr);
            Assert.Equal(r[0].Type, TokenType.Word);
            Assert.Equal(r[0].Value, "Method");
            Assert.Equal(r[1].Type, TokenType.OpeningBracket);
            Assert.Equal(r[2].Type, TokenType.Num);
            Assert.Equal(r[2].Value, "1");
            Assert.Equal(r[3].Type, TokenType.Plus);
            Assert.Equal(r[4].Type, TokenType.Word);
            Assert.Equal(r[4].Value, "Method");
        }

        private IReadOnlyList<Token> GetLexerResult(string expr)
        {
            var lexer = new Lexer(expr);
            return lexer.ReadAll();
        }
    }
}