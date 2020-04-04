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
            foreach (var cOp in cOps)
            {
                foreach (var op in cOps)
                {
                    yield return new object[] {cOp, op};
                }
            }
        }

        [Fact]
        public void ConditionalIfTest()
        {
            var expr =
                $@"
            if (x != 1)
            {{
                if(y != 1)
                {{
                    return 1;
                }}
                else
                {{
                    return 2;
                }}
            }}
            else
            {{
                if(z != 1)
                {{
                    return 3;
                }}
                else
                {{
                    return 4;
                }}
            }}";

            _testOutputHelper.WriteLine(expr);
            var actual = TestHelper.GeneratedStatementsMySelf(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);

            Assert.Equal(1, expectedFunc(0, 0, 0));
            Assert.Equal(3, expectedFunc(1, 0, 0));
            Assert.Equal(2, expectedFunc(0, 1, 0));
            Assert.Equal(1, expectedFunc(0, 0, 1));
            Assert.Equal(4, expectedFunc(1, 0, 1));
            Assert.Equal(3, expectedFunc(1, 1, 0));
            Assert.Equal(2, expectedFunc(0, 1, 1));
            Assert.Equal(4, expectedFunc(1, 1, 1));
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

        [Theory( /*Skip = "other time"*/)]
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


        [Fact]
        public void OrTest()
        {
            var expr =
                $@"
            if (x != 1 || y!=1)
            {{
                return 1;
            }}
            else
            {{
                return 0;
            }}";

            var actual = TestHelper.GeneratedStatementsMySelf(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);


            Assert.Equal(1, expectedFunc(0, 0, 0));
            Assert.Equal(1, expectedFunc(0, 1, 0));
            Assert.Equal(1, expectedFunc(1, 0, 0));
            Assert.Equal(0, expectedFunc(1, 1, 0));
        }

        [Fact]
        public void AndTest()
        {
            var expr =
                $@"
            if (x != 1 && y!=1)
            {{
                return 1;
            }}
            else
            {{
                return 0;
            }}";

            var actual = TestHelper.GeneratedStatementsMySelf(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);


            Assert.Equal(1, expectedFunc(0, 0, 0));
            Assert.Equal(0, expectedFunc(0, 1, 0));
            Assert.Equal(0, expectedFunc(1, 0, 0));
            Assert.Equal(0, expectedFunc(1, 1, 0));
        }

        [Fact]
        public void NotOperatorTest()
        {
            var statement = "if(!(x!=1)){return 0;}else {return 1;}";
            TestHelper.GeneratedStatementsMySelf(statement, out var func);
            TestHelper.GeneratedRoslynMethod(statement, out var roslynFunc);

            Assert.Equal(roslynFunc(0, 0, 0), func(0, 0, 0));
            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Fact]
        public void NotOperatorWithLogicalExpressionTest()
        {
            var statement = "if(!(x!=1 || y!=1)){return 0;}else {return 1;}";
            TestHelper.GeneratedStatementsMySelf(statement, out var func);
            TestHelper.GeneratedRoslynMethod(statement, out var roslynFunc);

            Assert.Equal(roslynFunc(0, 0, 0), func(0, 0, 0));
            Assert.Equal(roslynFunc(1, 0, 0), func(1, 0, 0));
            Assert.Equal(roslynFunc(0, 1, 1), func(0, 1, 1));
            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Fact]
        public void IfTrueTest()
        {
            var statement = "if(true){return 0;}else {return 1;}";
            TestHelper.GeneratedStatementsMySelf(statement, out var func);
            Assert.Equal(0, func(0, 0, 0));
        }

        [Fact]
        public void IfFalseTest()
        {
            var statement = "if(false){return 0;}else {return 1;}";
            TestHelper.GeneratedStatementsMySelf(statement, out var func);
            TestHelper.GeneratedRoslynMethod(statement, out var roslynFunc);
            Assert.Equal(1, func(0, 0, 0));
        }


        [Fact]
        public void ReductionTrueComparingTest()
        {
            var statement = "if(x == 1 == true){return 1;}else{return 0;}";
            TestHelper.GeneratedStatementsMySelf(statement, out var func);
            Assert.Equal(1, func(1, 0, 0));
            Assert.Equal(0, func(0, 0, 0));
        }

        [Fact]
        public void ReductionFalseComparingTest()
        {
            var statement = "if(x == 1 == false){return 1;}else{return 0;}";
            TestHelper.GeneratedStatementsMySelf(statement, out var func);
            Assert.Equal(0, func(1, 0, 0));
            Assert.Equal(1, func(0, 0, 0));
        }

        [Fact]
        public void ComparingLogicalConstantsTest()
        {
            var statement = "if(true && false){return 1;}else{return 0;}";
            TestHelper.GeneratedStatementsMySelf(statement, out var func);
            Assert.Equal(0, func(0, 0, 0));
            
            statement = "if(true && true){return 1;}else{return 0;}";
            TestHelper.GeneratedStatementsMySelf(statement, out func);
            Assert.Equal(1, func(0, 0, 0));
            
            statement = "if(true || false){return 1;}else{return 0;}";
            TestHelper.GeneratedStatementsMySelf(statement, out func);
            Assert.Equal(1, func(0, 0, 0));
            
            statement = "if(false || false){return 1;}else{return 0;}";
            TestHelper.GeneratedStatementsMySelf(statement, out func);
            Assert.Equal(0, func(0, 0, 0));
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