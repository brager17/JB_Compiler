using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Principal;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Parser
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private void JsonAssert(object expected, object actual)
        {
            if (JsonConvert.SerializeObject(expected) != JsonConvert.SerializeObject(actual))
                throw new EqualException(expected, actual);
        }

        [Fact]
        public void Test1()
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
        public void Test2()
        {
            var result = TestHelper.GetParseResultExpression("1-13", false);

            JsonAssert(
                new BinaryExpression(new PrimaryExpression("1"), new PrimaryExpression("13"), TokenType.Minus),
                result);
        }

        [Fact]
        public void Test3()
        {
            var result = TestHelper.GetParseResultExpression("(2+2)*2", false);

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(new PrimaryExpression("2"), new PrimaryExpression("2"), TokenType.Plus),
                    new PrimaryExpression("2"), TokenType.Star)
                , result);
        }

        [Fact]
        // todo : modify
        public void Test4()
        {
            var @exception = Assert.Throws<Exception>(() => TestHelper.GetParseResultExpression("(2+2))*2", false));
            Assert.Equal(exception.Message, "Expression is incorrect");
        }

        [Fact]
        public void Filler()
        {
            var file = new StreamWriter("input.txt");

            for (int i = 0; i < 100; i++)
            {
                file.Write($"var x{i} = {i};\n");
            }

            for (long i = int.MaxValue - 10000; i < (long) int.MaxValue + 10000; i += 100L)
            {
                file.Write($"var x{i} = {i};\n");
            }

            file.Dispose();
        }

        [Fact]
        public void Test5()
        {
            var result = TestHelper.GetParseResultExpression("(x+1)*y+z");

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(
                        new BinaryExpression(new VariableExpression("x", CompilerType.Long), new PrimaryExpression("1"),
                            TokenType.Plus),
                        new VariableExpression("y", CompilerType.Long), TokenType.Star),
                    new VariableExpression("z", CompilerType.Long), TokenType.Plus)
                , result);
        }

        [Fact]
        public void Test6()
        {
            var result = TestHelper.GetParseResultExpression("(x+1)*y+z+Method(1,x,14,-1)");

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(new VariableExpression("x", CompilerType.Long),
                                new PrimaryExpression("1"),
                                TokenType.Plus),
                            new VariableExpression("y", CompilerType.Long), TokenType.Star),
                        new VariableExpression("z", CompilerType.Long), TokenType.Plus),
                    new MethodCallExpression("Method",
                        new List<IExpression>()
                        {
                            new PrimaryExpression("1"), new VariableExpression("x", CompilerType.Long),
                            new PrimaryExpression("14"),
                            new UnaryExpression(new PrimaryExpression("1"))
                        }
                    ),
                    TokenType.Plus
                )
                , result);
        }

        [Fact]
        public void Test7()
        {
            var result = TestHelper.GetParseResultExpression("M(1) + M1(x)");

            JsonAssert(
                new BinaryExpression(
                    new MethodCallExpression("M", new[] {new PrimaryExpression("1"),}),
                    new MethodCallExpression("M1", new[] {new VariableExpression("x", CompilerType.Long)}),
                    TokenType.Plus)
                , result);
        }

        [Fact]
        public void BumpTester()
        {
            new TestCasesGenerator().RandomExpressionToFile();
        }

        [Fact]
        // public void Test(long x, long y, long z)
        public void Test()
        {
            TestHelper.GetParseResultExpression("x + y + (x + y) * z");
        }

        [Fact]
        // public void Test(long x, long y, long z)
        public void Test8()
        {
            TestHelper.GetParseResultExpression("x * y * z * x");
        }

        public long Fact(long x, long y, long z)
        {
            return x * y * z * x;
        }

        [Fact]
        public void Test9()
        {
            var result = TestHelper.GetParseResultExpression("x * y * z * x");
            var compiler = new ILCompiler();
            var method = compiler.CompileExpression(result);
            for (var x = 0; x < 10; x++)
            for (var y = 0; y < 10; y++)
            for (var z = 0; z < 10; z++)
                Assert.Equal(x * y * z * x, method.Invoke(x, y, z));
        }

        [Fact]
        public void Test123()
        {
            var m = GetType().GetMethods().First();
        }

        [Fact]
        public void Test10()
        {
            TestCasesGenerator testCasesGenerator = new TestCasesGenerator();
            var assembly = testCasesGenerator.GetAssemblyStream("x*y*x*z");

            var instructions = AssemblyDefinition.ReadAssembly(assembly).MainModule
                .GetTypes()
                .Single(x => x.Name.Contains("Runner"))
                .Methods
                .Single(x => x.Name == "Run")
                .Body.Instructions
                .ToArray();

            foreach (var instruction in instructions)
            {
                _testOutputHelper.WriteLine(instruction.ToString());
            }
        }

        [Fact]
        public void Test11()
        {
            var expr =
                @"2*(9503-(128+y*z+40*y-480*x/2803+(35+y*0))*x)*529831844*z+40/(5+x)*z*9/z*9*z-37136330941/y+971/x-69";
            var x = 2040216428;
            var y = 274473045;
            var z = 25132344;

            var t = TestHelper.GeneratedExpressionMySelf(expr, out var func);
            var tt = TestHelper.GeneratedRoslynExpression(expr, out var roslynFunc);

            Assert.Equal(roslynFunc(x, y, z), func(x, y, z));
        }

        [Fact]
        public void Tes14t()
        {
            sbyte a = 12;
            sbyte b = 12;
            var r = a + b;
        }
    }
}