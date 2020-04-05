using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Parser.Tests.ILGeneratorTests.MethodTests;
using Xunit;

namespace Parser.Tests
{
    public class TestCasesGenerator
    {
        private Random _random = new Random();

        long LongRandom()
        {
            byte[] buf = new byte[8];
            _random.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return longRand;
        }

        public (long x, long y, long z) GenerateRandomParameters()
        {
            return (LongRandom(), LongRandom(), LongRandom());
        }

        public void RandomExpressionToFile()
        {
            var generated = GenerateRandomExpression(100);
            File.WriteAllLines("testCases.txt", generated);
        }

        public (string[], string @return) GenerateRandomStatements(int count)
        {
            if (count > 25) count = 25;
            var result = new string[count];

            var type = new[] {"long", "int"};
            var random = new Random();
            var allowsOperators = new[] {'+', '-', '*'};

            var names = Enumerable.Range(0, 'z' - 'a').Select(x => (char) ('a' + (char) x))
                .ToArray();
            var alreadyAddedNames = new List<char>() {'x', 'y', 'z'};
            for (int i = 0; i < count; i++)
            {
                result[i] = type[random.Next(0, 1)] + " ";
                var activeNames = names.Except(alreadyAddedNames).ToArray();
                var name = activeNames[random.Next(0, activeNames.Length - 1)];
                result[i] += name + " = ";
                result[i] += GenerateRandomExpression(1, alreadyAddedNames.ToArray(), allowsOperators, false)[0];
                alreadyAddedNames.Add(name);
            }

            return (result, GenerateRandomExpression(1, alreadyAddedNames.ToArray(), allowsOperators, false)[0]);
        }

        public string[] GenerateRandomExpression(int count, char[] variables = null, char[] operators = null,
            bool check = true)
        {
            var result = new string[count];

            char[] brackets = new[] {'(', ')'};
            operators ??= new[]
            {
                '+', '-', '*',
            };
            variables ??= new[]
            {
                'x',
                'y',
                'z'
            };
            var digits = new[]
            {
                '0',
                '1',
                '2',
                '3',
                '4',
                '5',
                '6',
                '7',
                '8',
                '9',
            };

            while (count > 0)
            {
                var sb = new StringBuilder();
                var random = new Random();
                while (sb.Length < 100)
                {
                    IEnumerable<char> seq = null;
                    if (sb.Length > 0)
                    {
                        var lastSymbol = sb[^1];
                        if (digits.Contains(lastSymbol))
                        {
                            seq = operators;
                            seq = seq.Concat(digits);

                            if (DifferenceCountOpClBrackets(sb) > 0)
                                seq = seq.Concat(new[] {brackets[1]});
                        }
                        else if (variables.Contains(lastSymbol))
                        {
                            seq = operators;
                            if (DifferenceCountOpClBrackets(sb) > 0)
                                seq = seq.Concat(new[] {brackets[1]});
                        }
                        else if (operators.Contains(lastSymbol))
                        {
                            seq = variables.Concat(new[] {brackets[0]});
                            if (!digits.Contains(sb[^2]))
                                seq = seq.Concat(lastSymbol == '/' ? digits.Where(x => x != '0') : digits);
                        }
                        else if (brackets.Contains(lastSymbol))
                        {
                            if (lastSymbol == '(')
                            {
                                seq = variables.Concat(new[] {brackets[0]});
                                seq = seq.Concat(digits);
                            }
                            else
                            {
                                seq = operators;
                                if (DifferenceCountOpClBrackets(sb) > 0)
                                    seq = seq.Concat(new[] {brackets[1]});
                            }
                        }
                    }
                    else
                    {
                        seq = variables.Concat(new[] {brackets[0]});
                        seq = seq.Concat(digits);
                    }

                    var arr = seq.ToArray();
                    var randomSymbol = arr[random.Next(0, arr.Length)];
                    sb.Append(randomSymbol);
                }

                for (var i = sb.Length - 1; i > 0; i--)
                {
                    if (operators.Contains(sb[i]) || sb[i] == brackets[0])
                    {
                        sb = sb.Remove(sb.Length - 1, 1);
                    }
                    else break;
                }

                var difference = DifferenceCountOpClBrackets(sb);
                for (var i = 0; i < difference; i++)
                {
                    sb.Append(')');
                }


                var expr = sb.ToString();

                if (check)
                {
                    var syntaxTree = CSharpSyntaxTree.ParseText(Wrap(expr));
                    var compilation = CSharpCompilation.Create(
                        "assemblyName",
                        new[] {syntaxTree},
                        new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                    using var dllStream = new MemoryStream();
                    using var pdbStream = new MemoryStream();
                    var emitResult = compilation.Emit(dllStream, pdbStream);
                    if (emitResult.Success == false)
                    {
                        continue;
                    }
                }

                result[count - 1] = expr;
                count--;
            }

            return result;
        }

        private int DifferenceCountOpClBrackets(StringBuilder sb)
        {
            return CountSymbol(sb, '(') - CountSymbol(sb, ')');
        }


        [Fact]
        public void ldcSupport()
        {
            var q = Compiler.CompileExpression("22147482649 - 10000", out var f);
            f(1, 1, 1);
        }


        public MemoryStream GetAssemblyStream(string expr = null, string[] statements = null, string methodBody = null)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(Wrap(expr, statements, methodBody));
            var compilation = CSharpCompilation.Create(
                "assemblyName",
                new[] {syntaxTree},
                new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release));
            var dllStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            var result = compilation.Emit(dllStream);
            if (!result.Success)
            {
                throw new Exception("Roslyn compile exception\n" +
                                    string.Join("\n", result.Diagnostics) + "\n\n\n" +
                                    syntaxTree);
            }

            dllStream.Position = 0;
            return dllStream;
        }

        private string Wrap(string expr, string[] statements = null, string methodBody = null)
        {
            var sample = MethodsFieldsForTests.RunnerClassTemplate;
            if (methodBody != null)
            {
                var replace = sample.Replace("{statements};", "");
                var wrap = replace.Replace("return {expr};", methodBody);
                return wrap;
            }

            if (statements != null)
            {
                sample = sample.Replace("{statements}", string.Join(";\n", statements));
            }
            else
            {
                sample = sample.Replace("{statements}", "");
            }

            return sample.Replace("{expr}", expr);
        }

        private int CountSymbol(StringBuilder sb, char s)
        {
            var count = 0;
            for (var i = 0; i < sb.Length; i++)
                if (sb[i] == s)
                    count++;
            return count;
        }
    }
}