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
        public void ReadAll__Constant__Correct()
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
    }
}