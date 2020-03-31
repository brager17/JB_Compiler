using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
            var lexer = new Lexer("1+1/13");
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList, false);
            var result = parser.Parse().Single();

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
            var lexer = new Lexer("1-13");
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList, false);
            var result = parser.Parse().Single();

            JsonAssert(
                new BinaryExpression(new PrimaryExpression("1"), new PrimaryExpression("13"), TokenType.Minus),
                result);
        }

        [Fact]
        public void Test3()
        {
            var lexer = new Lexer("(2+2)*2");
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList, false);
            var result = parser.Parse().Single();

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(new PrimaryExpression("2"), new PrimaryExpression("2"), TokenType.Plus),
                    new PrimaryExpression("2"), TokenType.Star)
                , result);
        }

        [Fact]
        // todo не разобран случай когда ((2+2)*2, тогда тоже нужно вернуть нормальную ошибку
        public void Test4()
        {
            var lexer = new Lexer("(2+2))*2");
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList);
            var @exception = Assert.Throws<Exception>(() => parser.Parse());
            Assert.Equal(exception.Message, "Amount of opening brackets have to equals amount of closing brackets");
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
            var lexer = new Lexer("(x+1)*y+z");
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList);
            var result = parser.Parse().Single();

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(
                        new BinaryExpression(new VariableExpression("x"), new PrimaryExpression("1"), TokenType.Plus),
                        new VariableExpression("y"), TokenType.Star),
                    new VariableExpression("z"), TokenType.Plus)
                , result);
        }

        [Fact]
        public void Test6()
        {
            var lexer = new Lexer("(x+1)*y+z+Method(1,x,14,-1)"); // (x+1)*y+(z+Method());
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList);
            var result = parser.Parse().Single();

            JsonAssert(
                new BinaryExpression(
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(new VariableExpression("x"), new PrimaryExpression("1"), TokenType.Plus),
                            new VariableExpression("y"), TokenType.Star),
                        new VariableExpression("z"), TokenType.Plus),
                    new MethodCallExpression("Method",
                        new List<IExpression>()
                        {
                            new PrimaryExpression("1"), new VariableExpression("x"), new PrimaryExpression("14"),
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
            var lexer = new Lexer("M(1) + M1(x)"); // (x+1)*y+(z+Method());
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList);
            var result = parser.Parse().Single();

            JsonAssert(
                new BinaryExpression(
                    new MethodCallExpression("M", new[] {new PrimaryExpression("1"),}),
                    new MethodCallExpression("M1", new[] {new VariableExpression("x")}),
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
            Expression<Func<long, long, long, long>> expression = (x, y, z) => x + y + (x + y) * z;
            var lexer = new Lexer("x + y + (x + y) * z");
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList);
            var result = parser.Parse().Single();
        }

        [Fact]
        // public void Test(long x, long y, long z)
        public void Test8()
        {
            var lexer = new Lexer("x * y * z * x");
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList);
            var result = parser.Parse().Single();
        }

        public long Fact(long x, long y, long z)
        {
            return x * y * z * x;
        }

        [Fact]
        public void Test9()
        {
            var lexer = new Lexer("x * y * z * x");
            var readOnlyList = lexer.ReadAll();
            var parser = new Parser(readOnlyList);
            var result = parser.Parse().Single();
            var compiler = new ILCompiler();
            var method = compiler.Compile(result);
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
    }
}