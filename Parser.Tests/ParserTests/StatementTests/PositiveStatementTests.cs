using System;
using System.Collections.Generic;
using System.Linq;
using Parser.Parser.Exceptions;
using Parser.Parser.Expressions;
using Parser.Parser.Statements;
using Parser.Tests.ILGeneratorTests.MethodTests;
using Parser.Utils;
using Xunit;

namespace Parser.Tests.ParserTests.StatementTests
{
    public class NegativeStatementTests
    {
        [Fact]
        public void Parse__LocalVariableCalledAsParameter__ThrowException()
        {
            var expr = "int a = 12;return a;";
            var ex = Assert.Throws<CompileException>(() => Compiler.CompileStatement(expr, out var func,
                methodParameters: new Dictionary<string, CompilerType> {{"a", CompilerType.Int}}));
            Assert.Contains(
                "A local or parameter named 'a' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter",
                ex.Message);
        }
    }

    public class PositiveStatementTests
    {
        [Fact]
        public void Parse__DefineVariable__Correct()
        {
            string expr = "long q = 12;long w = -14;return q+w;";
            var result = TestHelper.GetParseResultStatements(expr);

            var logs = Compiler.CompileStatement(expr, out var func);
            var roslyn = TestHelper.GeneratedRoslynExpression("q+w",
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Fact]
        public void Parse__DefineVariablesWithExpression__Correct()
        {
            string expr = @"
                    long q = 12*x;
                    long w = -14+12;
                    return 2;
                    ";
            var result = TestHelper.GetParseResultStatements(expr);


            Assert.Equal(ExpressionType.Assignment, result[0].ExpressionType);
            var qAssignment = (AssignmentStatement) result[0];
            Assert.Equal("q", qAssignment.Left.Name);
            Assert.Equal(ExpressionType.Binary, qAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((BinaryExpression) qAssignment.Right).Left.ExpressionType);
            Assert.Equal(12, ((PrimaryExpression) ((BinaryExpression) qAssignment.Right).Left).AsLong());
            Assert.Equal(ExpressionType.MethodArgVariable, ((BinaryExpression) qAssignment.Right).Right.ExpressionType);
            Assert.Equal("x", ((VariableExpression) ((BinaryExpression) qAssignment.Right).Right).Name);

            Assert.Equal(ExpressionType.Assignment, result[1].ExpressionType);
            var qqAssignment = (AssignmentStatement) result[1];
            Assert.Equal("w", qqAssignment.Left.Name);
            Assert.Equal(ExpressionType.Unary, qqAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((UnaryExpression) qqAssignment.Right).Expression.ExpressionType);
            Assert.Equal(2, ((PrimaryExpression) ((UnaryExpression) qqAssignment.Right).Expression).AsLong());
        }


        [Fact]
        public void Parse__DefineVariablesWithExpressionUsingBeforeDefinedVariable__Correct()
        {
            string expr = @"
                    long q = 12*x;
                    long w = -q;
                    return 1;
                    ";
            var result = TestHelper.GetParseResultStatements(expr);

            Assert.Equal(ExpressionType.Assignment, result[0].ExpressionType);
            var qAssignment = (AssignmentStatement) result[0];
            Assert.Equal("q", qAssignment.Left.Name);
            Assert.Equal(ExpressionType.Binary, qAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((BinaryExpression) qAssignment.Right).Left.ExpressionType);
            Assert.Equal(12, ((PrimaryExpression) ((BinaryExpression) qAssignment.Right).Left).AsLong());
            Assert.Equal(ExpressionType.MethodArgVariable, ((BinaryExpression) qAssignment.Right).Right.ExpressionType);
            Assert.Equal("x", ((VariableExpression) ((BinaryExpression) qAssignment.Right).Right).Name);

            Assert.Equal(ExpressionType.Assignment, result[1].ExpressionType);
            var qqAssignment = (AssignmentStatement) result[1];
            Assert.Equal("w", qqAssignment.Left.Name);
            Assert.Equal(ExpressionType.Unary, qqAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.LocalVariable,
                ((UnaryExpression) qqAssignment.Right).Expression.ExpressionType);
            Assert.Equal("q", ((VariableExpression) ((UnaryExpression) qqAssignment.Right).Expression).Name);
        }


        [Fact]
        public void Parse__LocalVariableCalledAsStaticField__PreferLocalVariable()
        {
            var expr = "int a = 12;return a;";
            // MethodsFieldsForTests has static field "a"
            Compiler.CompileStatement(expr, out var func, typeof(MethodsFieldsForTests));
            Assert.Equal(12, func(1, 2, 3));
        }

        [Fact]
        public void Parse__ParameterCalledAsStaticField__PreferLocalParameter()
        {
            var func = Compiler.CompileStatement(
                "return a;",
                typeof(MethodsFieldsForTests),
                new Dictionary<string, CompilerType>() {{"a", CompilerType.Int}});

            Assert.Equal(12, func(12, 1, 1));
        }

        [Fact]
        // todo: сделать ```int q = 12``` , где int q - VariableDeclaration, а в ```q=12``` ` q - Variable 
        public void AssignmentTest()
        {
            var expr = "int q=12;q=11;return q;";
            var result = TestHelper.GetParseResultStatements(expr);

            Assert.Equal(ExpressionType.Assignment, result[0].ExpressionType);
            Assert.Equal(ExpressionType.Assignment, result[1].ExpressionType);
            Assert.Equal(ExpressionType.Return, result[2].ExpressionType);
        }

        private static void Print()
        {
        }

        [Fact]
        public void IfStatementTests()
        {
            var expr =
                $@"
            if (x == 1)
            {{
                Print();
                Print();
                int q = 21;
            }}
            else
            {{
                Print();
                Print();
                int w = 21;
                return 12;
            }}
            return 1;
            ";

            var r = TestHelper.GetParseResultStatements(expr, (new[] {((Action) Print).Method}));
            Assert.Equal(ExpressionType.IfElse, r[0].ExpressionType);
            Assert.Equal(3, ((IfElseStatement) r[0]).IfTrue.Statements.Length);
            Assert.Equal(4, ((IfElseStatement) r[0]).Else.Statements.Length);
            Assert.True(((IfElseStatement) r[0]).Else.IsReturnStatement);
        }


        [Fact]
        public void DefineBooleanVariableTest()
        {
            var expr =
                $@"
            bool boolTrue = true;
            bool boolFalse = false;
            return 1;
            ";

            var r = TestHelper.GetParseResultStatements(expr);
            Assert.Equal(ExpressionType.Assignment, r[0].ExpressionType);
            Assert.Equal("boolTrue", ((AssignmentStatement) r[0]).Left.Name);
            Assert.Equal(ExpressionType.Assignment, r[1].ExpressionType);
            Assert.Equal("boolFalse", ((AssignmentStatement) r[1]).Left.Name);
        }
    }
}