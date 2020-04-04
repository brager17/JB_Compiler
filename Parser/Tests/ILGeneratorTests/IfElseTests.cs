using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Xunit;
using Xunit.Abstractions;

namespace Parser.Tests.ILGeneratorTests
{
    public class IfElseTests
    {
        private static ITestOutputHelper _testOutputHelper;

        public IfElseTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }


        public static void AddByRef(ref int x)
        {
            x++;
        }

        public static void AddBy3Ref(ref int x)
        {
            x += 3;
        }

        public static int s = 1;

        [Fact]
        public void IfElseTest()
        {
            var exprWithArgX =
                $@"
            if (x == 1)
            {{
                AddByRef(ref x);
            }}
            else 
            {{
                AddBy3Ref(ref x);
            }}
            return x;
            ";

            TestHelper.GeneratedStatementsMySelf(exprWithArgX, out var func, @this: GetType());
            var r = func(1, 1, 1);
            Assert.Equal(2, r);
            r = func(2, 1, 1);
            Assert.Equal(5, r);
        }


        [Fact]
        public void RefTests()
        {
            var exprWithArgX =
                $@"
            if (x == 1)
            {{
                AddByRef(ref x);
            }}
            else 
            {{
                AddBy3Ref(ref x);
            }}
            return x;
            ";

            TestHelper.GeneratedStatementsMySelf(exprWithArgX, out var func, @this: GetType());
            var r = func(1, 1, 1);
            Assert.Equal(2, r);


            var exprWithField =
                $@"
            if(s == 1) 
            {{
                AddByRef(ref s);
            }}
            else 
            {{
                AddBy3Ref(ref s);
            }}
            return s;
            ";
            TestHelper.GeneratedStatementsMySelf(exprWithField, out func, @this: GetType());
            r = func(1, 1, 1);
            Assert.Equal(2, r);


            var exprWithLocal =
                $@"
            int l = 1;
            if(l == 1) 
            {{
                AddByRef(ref l);
            }}
            else 
            {{
                AddBy3Ref(ref l);
            }}
            return l;
            ";

            TestHelper.GeneratedStatementsMySelf(exprWithLocal, out func, @this: GetType());
            r = func(1, 1, 1);
            Assert.Equal(2, r);
        }

        [Theory]
        [InlineData("==", 1, 1)]
        [InlineData("==", 10, 2)]
        [InlineData("!=", 1, 2)]
        [InlineData("!=", 10, 1)]
        [InlineData(">", 0, 2)]
        [InlineData(">", 1, 2)]
        [InlineData(">", 10, 1)]
        [InlineData(">=", 1, 1)]
        [InlineData(">=", 10, 1)]
        [InlineData(">=", 0, 2)]
        [InlineData("<", 0, 1)]
        [InlineData("<", 1, 2)]
        [InlineData("<=", 10, 2)]
        [InlineData("<=", 0, 1)]
        [InlineData("<=", 1, 1)]
        [InlineData("<=", 10, 2)]
        public void SimpleIfElse(string @operator, long x, long expected)
        {
            var expr =
                $@"
            if ({x} {@operator} 1)
            {{
                return 1;
            }}
            else
            {{
                return 2;
            }}";

            _testOutputHelper.WriteLine(expr);

            var result = TestHelper.GeneratedStatementsMySelf(expr, out var func);

            Assert.Equal(expected, func(x, 1, 1));
        }

        public static IEnumerable<object[]> ConditionalIfTestTestCases()
        {
            var logOps = new[] {"||", "&&"};
            var cOps = new[] {">", ">=", "<", "<=", "==", "!="};
            foreach (var logOp in logOps)
            {
                foreach (var cOp in cOps)
                {
                    foreach (var op in cOps)
                    {
                        yield return new object[] {cOp, logOp, op};
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConditionalIfTestTestCases))]
        public void ConditionalIfTest(string leftCmp, string logOp, string rightCmp)
        {
            var expr = $@"
            if(x {leftCmp} 1 {logOp} y {rightCmp} 1){{return 1;}}
            else {{return 2;}}";

            _testOutputHelper.WriteLine(expr);
            var actual = TestHelper.GeneratedStatementsMySelf(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);

            Assert.Equal(expectedFunc(0, 0, 1), func(0, 0, 1));
            Assert.Equal(expectedFunc(0, 1, 1), func(0, 1, 1));
            Assert.Equal(expectedFunc(0, 2, 1), func(0, 2, 1));
            Assert.Equal(expectedFunc(1, 0, 1), func(1, 0, 1));
            Assert.Equal(expectedFunc(1, 1, 1), func(1, 1, 1));
            Assert.Equal(expectedFunc(1, 2, 1), func(1, 2, 1));
            Assert.Equal(expectedFunc(2, 0, 1), func(2, 0, 1));
            Assert.Equal(expectedFunc(2, 1, 1), func(2, 1, 1));
            Assert.Equal(expectedFunc(2, 2, 1), func(2, 2, 1));
        }


        public static IEnumerable<object[]> ComplexConditionalIfTestCases()
        {
            var logOps = new[] {"||", "&&"};
            var cOps = new[] {">", ">=", "<", "<=", "==", "!="};
            foreach (var log1 in logOps)
            {
                foreach (var log2 in logOps)
                {
                    foreach (var op1 in cOps)
                    {
                        foreach (var op2 in cOps)
                        {
                            foreach (var op3 in cOps)
                            {
                                yield return new object[] {op1, log1, op2, log2, op3};
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ComplexConditionalIfTestCases))]
        public void ComplexConditionalIfTest(string oneCmp, string oneLog, string twoCmp, string twoLogical,
            string threeCmp)
        {
            var expr = $@"
            if(x {oneCmp} 1 {oneLog} y {twoCmp} 1 {twoLogical} z {threeCmp} 1 ){{return 1;}}
            else {{return 2;}}";

            _testOutputHelper.WriteLine(expr);
            var actual = TestHelper.GeneratedStatementsMySelf(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);

            Assert.Equal(expectedFunc(0, 0, 0), func(0, 0, 0));
            Assert.Equal(expectedFunc(0, 1, 0), func(0, 1, 0));
            Assert.Equal(expectedFunc(0, 2, 0), func(0, 2, 0));
            Assert.Equal(expectedFunc(1, 0, 0), func(1, 0, 0));
            Assert.Equal(expectedFunc(1, 1, 0), func(1, 1, 0));
            Assert.Equal(expectedFunc(1, 2, 0), func(1, 2, 0));
            Assert.Equal(expectedFunc(2, 0, 0), func(2, 0, 0));
            Assert.Equal(expectedFunc(2, 1, 0), func(2, 1, 0));
            Assert.Equal(expectedFunc(2, 2, 0), func(2, 2, 0));

            Assert.Equal(expectedFunc(0, 0, 1), func(0, 0, 1));
            Assert.Equal(expectedFunc(0, 1, 1), func(0, 1, 1));
            Assert.Equal(expectedFunc(0, 2, 1), func(0, 2, 1));
            Assert.Equal(expectedFunc(1, 0, 1), func(1, 0, 1));
            Assert.Equal(expectedFunc(1, 1, 1), func(1, 1, 1));
            Assert.Equal(expectedFunc(1, 2, 1), func(1, 2, 1));
            Assert.Equal(expectedFunc(2, 0, 1), func(2, 0, 1));
            Assert.Equal(expectedFunc(2, 1, 1), func(2, 1, 1));
            Assert.Equal(expectedFunc(2, 2, 1), func(2, 2, 1));

            Assert.Equal(expectedFunc(0, 0, 2), func(0, 0, 2));
            Assert.Equal(expectedFunc(0, 1, 2), func(0, 1, 2));
            Assert.Equal(expectedFunc(0, 2, 2), func(0, 2, 2));
            Assert.Equal(expectedFunc(1, 0, 2), func(1, 0, 2));
            Assert.Equal(expectedFunc(1, 1, 2), func(1, 1, 2));
            Assert.Equal(expectedFunc(1, 2, 2), func(1, 2, 2));
            Assert.Equal(expectedFunc(2, 0, 2), func(2, 0, 2));
            Assert.Equal(expectedFunc(2, 1, 2), func(2, 1, 2));
            Assert.Equal(expectedFunc(2, 2, 2), func(2, 2, 2));
        }


        public void Operation1(bool left)
        {
            int x = 1;
            int y = 2;
            if (x == 1)
            {
                Console.WriteLine("x");
            }
            else
            {
                Console.WriteLine("y");
            }

            Console.WriteLine("z");
        }
    }
}