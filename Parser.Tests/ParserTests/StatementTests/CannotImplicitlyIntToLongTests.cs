using System;
using Parser.Exceptions;
using Parser.Tests.ILGeneratorTests;
using Xunit;

namespace Parser
{
    public class CannotImplicitlyIntToLongTests
    {
        [Theory]
        [InlineData("long q=12; int w = q;")]
        [InlineData("int q = long.MaxValue;")]
        public void Parse__ImplicitIntToLong__ThrowError(string expr)
        {
            var exception = Assert.Throws<CompileException>(() => Compiler.CompileStatement(expr, out _));
            Assert.Equal("Cannot implicitly convert type 'long ' to int", exception.Message);
        }

        // 
        // todo :: нужно сделать один класс, который передать в Parse, что-то типа контекста и туда положить всю необходимую информацию
        // public static long a() => 1;
//            [MethodImpl(MethodImplOptions.NoOptimization)]
        //          public static long Run(long x, long y, long z)
        //        {
        //        int w = a(); <- expcetion
        //        return 1;
        //  }
        //
        //
        [Theory]
        [InlineData("int q = MethodWithoutParameters();return 1;")]
        public void Parse__ImplicitIntToLongAssignmentMethod__ThrowError(string expr)
        {
            var exception = Assert.Throws<CompileException>(() =>
                Compiler.CompileStatement(expr, out _, typeof(MethodsFieldsForTests)));
            Assert.Equal("Cannot implicitly convert type 'long ' to int", exception.Message);
        }
    }
}