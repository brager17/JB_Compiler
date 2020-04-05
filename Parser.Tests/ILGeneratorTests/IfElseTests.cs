using System.Collections;
using System.Collections.Generic;
using Parser.Tests.ILGeneratorTests.MethodTests;
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

        [Fact]
        public void Compile__IfReturnTest__AsRoslynResult()
        {
            var expr =
                @"
            if ( x!= 1)
            {
                return 1;
            }
            return 3;";

            _testOutputHelper.WriteLine(expr);

            Compiler.CompileStatement(expr, out var func);

            Assert.Equal(3, func(1, 0, 0));
            Assert.Equal(1, func(0, 0, 0));
        }

        [Fact]
        public void Compile__IfTest__AsRoslynResult()
        {
            var expr =
                @"
            if ( x!= 1)
            {
                 x = x+1;
            }
            return x;";

            _testOutputHelper.WriteLine(expr);

            Compiler.CompileStatement(expr, out var func, GetType());

            Assert.Equal(11, func(10, 0, 0));
            Assert.Equal(1, func(1, 0, 0));
        }

        [Fact]
        public void Compile__IfElseTest__AsRoslynResult()
        {
            var expr =
                @"
            if ( x!= 1)
            {
                 x = x+1;
            }
            else { x = x+2;}
            return x;";

            _testOutputHelper.WriteLine(expr);

            Compiler.CompileStatement(expr, out var func, GetType());

            Assert.Equal(3, func(1, 0, 0));
            Assert.Equal(1, func(0, 0, 0));
        }

        [Fact]
        public void Compile__IfReturnElseTest__AsRoslynResult()
        {
            var expr =
                @"
            if ( x!= 1)
            {
                 return x;
            }
            else{
                x = x+1;
            }
            return x;";

            _testOutputHelper.WriteLine(expr);

            Compiler.CompileStatement(expr, out var func, GetType());

            Assert.Equal(0, func(0, 0, 0));
            Assert.Equal(2, func(1, 0, 0));
        }

        [Fact]
        public void Compile__IfElseReturnTest__AsRoslynResult()
        {
            var expr =
                @"
            if ( x!= 1)
            {
                 x=x+1;
            }
            else{
                return x;
            }
            return x;";

            _testOutputHelper.WriteLine(expr);

            Compiler.CompileStatement(expr, out var func, GetType());

            Assert.Equal(11, func(10, 0, 0));
            Assert.Equal(1, func(1, 0, 0));
        }

        [Fact]
        public void Compile__IfReturnElseReturnTest__AsRoslynResult()
        {
            var expr =
                @"
            if ( x!= 1)
            {
                 return x;
            }
            else{
                return x;
            }
            return x;";

            _testOutputHelper.WriteLine(expr);

            Compiler.CompileStatement(expr, out var func, GetType());

            Assert.Equal(1, func(1, 0, 0));
            Assert.Equal(0, func(0, 0, 0));
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
        public void Compile__SimpleIfElse__AsRoslynResult(string @operator, long x, long expected)
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
            }}
            return 3;
";

            _testOutputHelper.WriteLine(expr);

            Compiler.CompileStatement(expr, out var func);

            Assert.Equal(expected, func(x, 1, 1));
        }

        [Theory]
        [InlineData("==", 1, 2)]
        [InlineData("==", 10, 1)]
        [InlineData("!=", 1, 1)]
        [InlineData("!=", 10, 2)]
        [InlineData(">", 0, 1)]
        [InlineData(">", 1, 1)]
        [InlineData(">", 10, 2)]
        [InlineData(">=", 1, 2)]
        [InlineData(">=", 10, 2)]
        [InlineData(">=", 0, 1)]
        [InlineData("<", 0, 2)]
        [InlineData("<", 1, 1)]
        [InlineData("<=", 10, 1)]
        [InlineData("<=", 0, 2)]
        [InlineData("<=", 1, 2)]
        [InlineData("<=", 10, 1)]
        public void Compile__SimpleIfElseWithNotUnaryExpression(string @operator, long x, long expected)
        {
            var expr =
                $@"
            if (!({x} {@operator} 1))
            {{
                return 1;
            }}
            else
            {{
                return 2;
            }}
            return 3;
";

            _testOutputHelper.WriteLine(expr);

            Compiler.CompileStatement(expr, out var func);

            Assert.Equal(expected, func(x, 1, 1));
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
            var actual = Compiler.CompileStatement(expr, out var func, typeof(MethodsFieldsForTests));
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);

            Assert.Equal(1, func(0, 0, 0));
            Assert.Equal(3, func(1, 0, 0));
            Assert.Equal(2, func(0, 1, 0));
            Assert.Equal(1, func(0, 0, 1));
            Assert.Equal(4, func(1, 0, 1));
            Assert.Equal(3, func(1, 1, 0));
            Assert.Equal(2, func(0, 1, 1));
            Assert.Equal(4, func(1, 1, 1));
        }

        public class ComplexConditionalIfTestCases : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
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

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }


        // [Theory( Skip = "other time")]
        [Theory()]
        [ClassData(typeof(ComplexConditionalIfTestCases))]
        public void Compile__ComplexConditionalIfTest__AsRoslynResult(
            string oneCmp,
            string oneLog,
            string twoCmp,
            string twoLogical,
            string threeCmp)
        {
            var expr = $@"
            if(x {oneCmp} 1 {oneLog} y {twoCmp} 1 {twoLogical} z {threeCmp} 1){{return 1;}}
            else {{return 2;}}";

            _testOutputHelper.WriteLine(expr);
            var actual = Compiler.CompileStatement(expr, out var func);

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
        public void Compile__OrOperator__AsRoslynResult()
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

            var actual = Compiler.CompileStatement(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);


            Assert.Equal(func(0, 0, 0), expectedFunc(0, 0, 0));
            Assert.Equal(func(0, 1, 0), expectedFunc(0, 1, 0));
            Assert.Equal(func(1, 0, 0), expectedFunc(1, 0, 0));
            Assert.Equal(func(1, 1, 0), expectedFunc(1, 1, 0));
        }

        [Fact]
        public void Compile__NotOperator__AsRoslynResult()
        {
            var statement = "if(!(x!=1)){return 0;}else {return 1;}";
            Compiler.CompileStatement(statement, out var func);
            TestHelper.GeneratedRoslynMethod(statement, out var roslynFunc);

            Assert.Equal(roslynFunc(0, 0, 0), func(0, 0, 0));
            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Fact]
        public void Compile__NotOperatorWithLogicalExpressionTest__AsRoslynResult()
        {
            var statement = "if(!(x!=1 || y!=1)){return 0;}else {return 1;}";
            Compiler.CompileStatement(statement, out var func);
            TestHelper.GeneratedRoslynMethod(statement, out var roslynFunc);

            Assert.Equal(roslynFunc(0, 0, 0), func(0, 0, 0));
            Assert.Equal(roslynFunc(1, 0, 0), func(1, 0, 0));
            Assert.Equal(roslynFunc(0, 1, 1), func(0, 1, 1));
            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Fact]
        public void Compile__OrOperatorWithNotExpressions__AsRoslynResult()
        {
            var expr =
                $@"
            if (!(x != 1) || !(y!=1))
            {{
                return 1;
            }}
            else
            {{
                return 0;
            }}";

            var actual = Compiler.CompileStatement(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);


            Assert.Equal(expectedFunc(0, 0, 0), func(0, 0, 0));
            Assert.Equal(expectedFunc(0, 1, 0), func(0, 1, 0));
            Assert.Equal(expectedFunc(1, 0, 0), func(1, 0, 0));
            Assert.Equal(expectedFunc(1, 1, 0), func(1, 1, 0));
        }

        [Fact]
        public void Compile__OrOperatorWithConditions__AsRoslynResult()
        {
            var expr =
                $@"
            if (!(x != 1 || x < 10) || !(y!=1 || y<20))
            {{
                return 1;
            }}
            else
            {{
                return 0;
            }}";

            var actual = Compiler.CompileStatement(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);


            Assert.Equal(expectedFunc(0, 0, 0), func(0, 0, 0));
            Assert.Equal(expectedFunc(0, 1, 0), func(0, 1, 0));
            Assert.Equal(expectedFunc(1, 0, 0), func(1, 0, 0));
            Assert.Equal(expectedFunc(1, 1, 0), func(1, 1, 0));
            Assert.Equal(expectedFunc(11, 0, 0), func(11, 0, 0));
            Assert.Equal(expectedFunc(11, 1, 0), func(11, 1, 0));
            Assert.Equal(expectedFunc(11, 0, 0), func(11, 0, 0));
            Assert.Equal(expectedFunc(11, 1, 0), func(11, 1, 0));
            Assert.Equal(expectedFunc(0, 21, 0), func(0, 21, 0));
            Assert.Equal(expectedFunc(0, 21, 0), func(0, 21, 0));
            Assert.Equal(expectedFunc(1, 21, 0), func(1, 21, 0));
            Assert.Equal(expectedFunc(1, 21, 0), func(1, 21, 0));
        }


        [Fact]
        public void Compile__AndOperator__AsRoslynResult()
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

            var actual = Compiler.CompileStatement(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);

            Assert.Equal(expectedFunc(0, 0, 0), func(0, 0, 0));
            Assert.Equal(expectedFunc(0, 1, 0), func(0, 1, 0));
            Assert.Equal(expectedFunc(1, 0, 0), func(1, 0, 0));
            Assert.Equal(expectedFunc(1, 1, 0), func(1, 1, 0));
        }

        [Fact]
        public void Compile__DoubleAndOperator__AsRoslynResult()
        {
            var expr =
                $@"
            if (x != 1 && y!=1 && z!=1)
            {{
                return 1;
            }}
            else
            {{
                return 0;
            }}";

            var actual = Compiler.CompileStatement(expr, out var func);
            var expected = TestHelper.GeneratedRoslynMethod(expr, out var expectedFunc);


            Assert.Equal(1, expectedFunc(0, 0, 0));
            Assert.Equal(0, expectedFunc(0, 1, 0));
            Assert.Equal(0, expectedFunc(1, 0, 0));
            Assert.Equal(0, expectedFunc(1, 1, 1));
            Assert.Equal(0, expectedFunc(0, 1, 1));
            Assert.Equal(0, expectedFunc(1, 0, 1));
            Assert.Equal(0, expectedFunc(1, 1, 1));
            Assert.Equal(0, expectedFunc(0, 1, 1));
            Assert.Equal(0, expectedFunc(1, 0, 1));
            Assert.Equal(0, expectedFunc(1, 1, 1));
        }


        [Fact]
        public void Compile__IfTrue__UseIfStatement()
        {
            var statement = "if(true){return 0;}else {return 1;}";
            Compiler.CompileStatement(statement, out var func);
            Assert.Equal(0, func(0, 0, 0));
        }

        [Fact]
        public void Compile__IfFalse__UseElseStatement()
        {
            var statement = "if(false){return 0;}else {return 1;}";
            Compiler.CompileStatement(statement, out var func);
            TestHelper.GeneratedRoslynMethod(statement, out var roslynFunc);
            Assert.Equal(1, func(0, 0, 0));
        }


        [Fact]
        public void Compile__CompareWithTrue()
        {
            var statement = "if(x == 1 == true){return 1;}else{return 0;}";
            Compiler.CompileStatement(statement, out var func);
            Assert.Equal(1, func(1, 0, 0));
            Assert.Equal(0, func(0, 0, 0));
        }

        [Fact]
        public void Compile__CompareWithFalse()
        {
            var statement = "if(x == 1 == false){return 1;}else{return 0;}";
            Compiler.CompileStatement(statement, out var func);
            Assert.Equal(0, func(1, 0, 0));
            Assert.Equal(1, func(0, 0, 0));
        }

        [Fact]
        public void Compile__CompareLogicalConstantsTest()
        {
            var statement = "if(true && false){return 1;}else{return 0;}";
            Compiler.CompileStatement(statement, out var func);
            Assert.Equal(0, func(0, 0, 0));

            statement = "if(true && true){return 1;}else{return 0;}";
            Compiler.CompileStatement(statement, out func);
            Assert.Equal(1, func(0, 0, 0));

            statement = "if(true || false){return 1;}else{return 0;}";
            Compiler.CompileStatement(statement, out func);
            Assert.Equal(1, func(0, 0, 0));

            statement = "if(false || false){return 1;}else{return 0;}";
            Compiler.CompileStatement(statement, out func);
            Assert.Equal(0, func(0, 0, 0));
        }

        [Fact]
        public void Compile__UsingNotWithBooleanVariables()
        {
            var expr = "bool test = true; if(!test){return 0;}else {return 1;};";
            var f = Compiler.CompileStatement(expr);
            Assert.Equal(1, f(0, 0, 0));

            expr = "bool test = false; if(!test){return 0;}else {return 1;};";
            f = Compiler.CompileStatement(expr);
            Assert.Equal(0, f(0, 0, 0));
        }
    }
}