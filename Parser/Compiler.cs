using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Compiler;

namespace Parser
{
    public delegate long CompileResult(long x, long y, long z);

    public class Compiler
    {
        public static CompileResult CompileStatement(
            string program,
            Type thisType = null,
            Dictionary<string, CompilerType> methodParameters = null)
        {
            _ = Compile(true, program, out var result, thisType, methodParameters);
            return result;
        }

        public static string[] CompileStatement(
            string program,
            out CompileResult result,
            Type thisType = null,
            Dictionary<string, CompilerType> methodParameters = null)
        {
            return Compile(true, program, out result, thisType, methodParameters);
        }

        public static CompileResult CompileExpression(
            string program,
            Type thisType = null,
            Dictionary<string, CompilerType> methodParameters = null)
        {
            _ = Compile(false, program, out var result, thisType, methodParameters);
            return result;
        }

        public static string[] CompileExpression(
            string program,
            out CompileResult result,
            Type thisType = null,
            Dictionary<string, CompilerType> methodParameters = null)
        {
            return Compile(false, program, out result, thisType, methodParameters);
        }


        private static string[] Compile(
            bool isStatement,
            string program,
            out CompileResult compileResult,
            Type thisType = null,
            Dictionary<string, CompilerType> methodParameters = null)
        {
            var lexer = new Lexer(program);

            var tokenSequence = lexer.Tokenize();

            var methods = thisType?.GetMethods(BindingFlags.Static | BindingFlags.Public)
                          ?? Array.Empty<MethodInfo>();

            var fields = thisType?.GetFields(BindingFlags.Static | BindingFlags.Public)
                         ?? Array.Empty<FieldInfo>();

            methodParameters ??= new Dictionary<string, CompilerType>()
            {
                {"x", CompilerType.Long}, {"y", CompilerType.Long}, {"z", CompilerType.Long},
            };

            var parser = new Parser(
                new ParserContext(
                    tokenSequence, methodParameters,
                    fields.ToDictionary(x => x.Name, x => x),
                    GetClosureMethods(methods),
                    true)
            );

            Statement statement = default;
            IExpression expression = default;
            if (isStatement) statement = parser.Parse();
            else expression = parser.ParseExpression();

            var dynamicMethod = new DynamicMethod(
                "JB",
                typeof(long),
                new[] {typeof(long), typeof(long), typeof(long)});

            var ilCompiler = new CompileExpressionVisitor(dynamicMethod.GetILGenerator(),
                methods.ToDictionary(x => x.Name, x => x));

            var logs = isStatement ? ilCompiler.Start(statement) : ilCompiler.Start(expression);
            compileResult = (CompileResult) dynamicMethod.CreateDelegate(typeof(CompileResult));
            return logs;
        }

        private static Dictionary<string, (CompilerType[], CompilerType)> GetClosureMethods(MethodInfo[] methods)
        {
            return methods.ToDictionary(x => x.Name,
                x => (
                    x.GetParameters()
                        .Select(x => x.ParameterType.GetRoslynType())
                        .ToArray(),
                    x.ReturnType.GetRoslynType())
            );
        }
    }
}