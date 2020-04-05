using System;
using System.IO;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Parser;
using Parser.Tests;

namespace Bumper
{
    class Program
    {
        static void BumpTest()
        {
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

        [UsedImplicitly]
        static void PrintErrorMessage()
        {
            Console.WriteLine(
                "Пожалуйста, передайте в качестве параметра z  число от 0 до 3, или заполните поле \"operation\"");
        }

        [UsedImplicitly]
        private static int operation = -1;

        [UsedImplicitly]
        private static bool useSecretOperation;

        [UsedImplicitly]
        private static long SecretOperation(long a, long b) => (a + b) << 1;

        static void Main(string[] args)
        {
            BumpTest();
            var calculator = @"
        int op = -1;
        if ((z < 0 || z > 3) && (operation < 0 || operation > 3) && !useSecretOperation)
        {
            PrintErrorMessage();
            return -1;
        }
        if(!(z < 0 || z > 3))
        { 
            op = z;
        }
        else
        {
            if(!(operation < 0 || operation > 3))
            {
               op = operation;
            }
        }
        
        if(op == 0) 
        {
            return x + y;
        }
        if(op == 1)
        {
            return x - y;
        }
        if(op == 2) 
        {
            return x * y;
        }
        if(op == 3)
        {
            return x / y;
        }
        return SecretOperation(x,y);
";


            var func = Compile(calculator);

            Console.WriteLine($"10 + 5 = {func(10, 5, 0)}");
            Console.WriteLine($"10 - 5 = {func(10, 5, 1)}");
            Console.WriteLine($"10 * 5 = {func(10, 5, 2)}");
            Console.WriteLine($"10 / 5 = {func(10, 5, 3)}");
            operation = 0;
            Console.WriteLine($"10 + 5 = {func(10, 5, 4)}");
            operation = -1;
            useSecretOperation = true;
            Console.WriteLine($"Secret Operation = {func(10, 5, 5)}");
        }

        public static CompileResult Compile(string str)
        {
            return str.Contains(";") || str.Contains("{")
                ? Compiler.CompileStatement(str, typeof(Program))
                : Compiler.CompileExpression(str, typeof(Program));
        }
    }
}