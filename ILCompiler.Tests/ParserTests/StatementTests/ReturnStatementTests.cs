using Parser.Parser.Exceptions;
using Xunit;

namespace Parser.Tests.ParserTests.StatementTests
{
    public class ReturnStatementTests
    {
        [Fact]
        public void Parse__ElseWithoutReturn__ThrowException()
        {
            var expr = @"
                if(x==1)
                {
                    return 1;
                }
                else
                {
                }
                ";

            var exception = Assert.Throws<CompileException>(() => Compiler.CompileStatement(expr, out _));
            Assert.Equal("End of function is reachable without any return statement", exception.Message);
        }

        [Fact]
        public void Parse__ElseWithoutReturnNestedIfElseWithReturn__NoThrowException()
        {
            var expr = @"
                if(x==1)
                {
                    return 1;
                }
                else
                {
                    if(x==1)
                    {
                        return 1;
                    }
                    else 
                    {
                        return 2;
                    }
                }";

            Compiler.CompileStatement(expr, out _);
        }
        
        [Fact]
        public void Parse__ElseWithoutReturnReturnAfterElse__NoThrowException()
        {
            var expr = @"
                if(x==1)
                {
                    return 1;
                }
                else
                {
                   
                }
                return 1;
            ";

            Compiler.CompileStatement(expr, out _);
        }
    }
}