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

        private static CompileResult Compile(string str)
        {
            // for ease of testing, it is better to use methods CompileStatement and CompileExpression separately
            return str.Contains(";") || str.Contains("{")
                ? Compiler.CompileStatement(str, typeof(Program))
                : Compiler.CompileExpression(str, typeof(Program));
        }
    }
}