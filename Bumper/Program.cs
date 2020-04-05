using System;
using System.IO;
using System.Linq.Expressions;
using Parser;

namespace Bumper
{
    class Program
    {
        static void Main(string[] args)
        {
            Expression<Func<int, int>> e = x => x;
            e.Compile();
            var generator = new TestCasesGenerator();
            var expressions = generator.GenerateRandomExpression(1000);
            using var @out = new StreamWriter("output.txt");
            using var exceptions = new StreamWriter("exceptions.txt");
            foreach (var expression in expressions)
            {
                long x = 0, y = 0, z = 0;
                try
                {
                    var myFunc = Parser.Compiler.CompileExpression(expression);
                    TestHelper.GeneratedRoslynExpression(expression, out var func);
                    for (int i = 0; i < 1000; i++)
                    {
                        (x, y, z) = generator.GenerateRandomParameters();

                        long actual = 0;
                        long expected = 0;
                        try
                        {
                            actual = myFunc(x, y, z);
                        }
                        catch (DivideByZeroException)
                        {
                            try
                            {
                                func(x, y, z);
                            }
                            catch (DivideByZeroException)
                            {
                                continue;
                            }

                            exceptions.WriteLine($"actual thrown divide by zero");
                            exceptions.WriteLine($"expected didn't throw");
                        }

                        try
                        {
                            expected = func(x, y, z);
                        }
                        catch (DivideByZeroException)
                        {
                            exceptions.WriteLine($"actual didn't throw divide by zero");
                            exceptions.WriteLine($"expected thrown");
                            continue;
                        }

                        @out.WriteLine($"Expression {expression}");
                        @out.WriteLine($"x {x} y {y} z {z}");
                        if (actual != expected)
                        {
                            exceptions.WriteLine($"actual {actual}");
                            exceptions.WriteLine($"expected {expected}");
                            @out.WriteLine($"wrong");
                        }

                        @out.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.WriteLine($"Expression {expression}");
                    exceptions.WriteLine($"x {x} y {y} z {z}");
                    exceptions.WriteLine($"Exception {ex}");
                    exceptions.WriteLine();
                }
            }
        }
    }
}