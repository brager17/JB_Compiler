using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Principal;
using Parser;

namespace Compiler
{
    public static class ExpressionExtensions
    {
        public static bool TryCast<T>(this IExpression expression, out T value) where T : IExpression
        {
            switch (expression.ExpressionType)
            {
                case ExpressionType.Variable when typeof(T) == typeof(VariableExpression):
                case ExpressionType.Primary when typeof(T) == typeof(PrimaryExpression):
                case ExpressionType.Binary when typeof(T) == typeof(BinaryExpression):
                case ExpressionType.Unary when typeof(T) == typeof(UnaryExpression):
                case ExpressionType.MethodCall when typeof(T) == typeof(MethodCallExpression):
                    value = (T) expression;
                    return true;
                default:
                    value = default;
                    return false;
            }
        }

        public static long AsLong(this PrimaryExpression primaryExpression) => long.Parse(primaryExpression.Value);
        public static int AsInt(this PrimaryExpression primaryExpression) => int.Parse(primaryExpression.Value);
        public static uint AsUInt(this PrimaryExpression primaryExpression) => uint.Parse(primaryExpression.Value);
    }

    public class ILCompiler
    {
        private readonly bool _throwIfOverflow;
        private ILGenerator _generator;
        private ILGenerator _ilGenerator;

        public ILCompiler(bool throwIfOverflow = false)
        {
            _throwIfOverflow = throwIfOverflow;
        }


        public delegate long CompileResult(long x, long y, long z);


        public CompileResult Compile(IStatement[] statements)
        {
            var dynamicMethod = new DynamicMethod(
                "method",
                typeof(long),
                new[] {typeof(long), typeof(long), typeof(long)});

            var visitor = new CompileExpressionVisitor(
                dynamicMethod,
                new[] {"x", "y", "z"},
                null,
                null);
            visitor.Start(statements);
            return (CompileResult) dynamicMethod.CreateDelegate(typeof(CompileResult), null);
        }

        public T Compile<T>(IStatement[] statements, string[] methodParameters) where T : Delegate
        {
            var dynamicMethod = new DynamicMethod(
                "method",
                typeof(T).GenericTypeArguments[^1],
                typeof(T).GenericTypeArguments[..^1]);

            var visitor = new CompileExpressionVisitor(
                dynamicMethod,
                methodParameters,
                null,
                null);
            visitor.Start(statements);
            return (T) dynamicMethod.CreateDelegate(typeof(T), null);
        }

        public CompileResult CompileExpression(IExpression expression)
        {
            var dynamicMethod = new DynamicMethod(
                "method",
                typeof(long),
                new[] {typeof(long), typeof(long), typeof(long)});

            var visitor = new CompileExpressionVisitor(
                dynamicMethod,
                new[] {"x", "y", "z"},
                null,
                null);
            visitor.Start(expression);
            return (CompileResult) dynamicMethod.CreateDelegate(typeof(CompileResult), null);
        }
    }

    public class CompileExpressionVisitor
    {
        public Dictionary<string, CompilerType> Variables { get; }

        public class Logger
        {
            private List<string> _logger = new List<string>();

            public void Log(string log)
            {
                _logger.Add(log);
            }

            internal string[] GetLogs => _logger.ToArray();
        }

        private readonly string[] _parameters;
        private readonly Dictionary<string, FieldInfo> _closureFields;
        private readonly Dictionary<string, MethodInfo> _closedMethods;
        private Dictionary<string, CompilerType> _localVariables;
        private ILGenerator _ilGenerator;

        // for tests
        public Logger logger = new Logger();
        private const string TestedNamespace = "RunnerNamespace";
        private const string TestedTypeFullName = "RunnerNamespace.Runner";
        private const string TestedClass = "Runner";

        public CompileExpressionVisitor(
            DynamicMethod dynamicMethod,
            string[] parameters,
            Dictionary<string, FieldInfo> closureFields,
            Dictionary<string, MethodInfo> closedMethods)
        {
            _parameters = parameters;
            _localVariables = new Dictionary<string, CompilerType>();
            _closureFields = closureFields ?? new Dictionary<string, FieldInfo>();
            _closedMethods = closedMethods ?? new Dictionary<string, MethodInfo>();
            _ilGenerator = dynamicMethod.GetILGenerator();
        }

        public void Start(IStatement[] statements)
        {
            _localVariables = statements
                .OfType<AssignmentStatement>()
                // todo optimize:
                .GroupBy(x => x.Left.Name)
                .Select(x => x.First().Left)
                .ToDictionary(x => x.Name, x => x.ReturnType);

            foreach (var localVariable in _localVariables)
            {
                var type = localVariable.Value switch
                {
                    CompilerType.Long => typeof(long),
                    CompilerType.Int => typeof(int),
                    CompilerType.UInt => typeof(uint),
                    _ => throw new ArgumentOutOfRangeException()
                };

                _ilGenerator.DeclareLocal(type);
            }

            foreach (var statement in statements)
            {
                Visit((dynamic) statement);
            }
        }

        public void Start(IExpression expression)
        {
            Visit((dynamic) expression);
            logger.Log("ret");
            _ilGenerator.Emit(OpCodes.Ret);
        }

        public long Test1(long x, long y, long z)
        {
            uint t = 12;
            int i = 1;
            var t1 = i + t;
            return 1L;
        }

        public BinaryExpression Visit(BinaryExpression binaryExpression)
        {
            var left = Visit((dynamic) binaryExpression.Left);
            var right = Visit((dynamic) binaryExpression.Right);
            switch (binaryExpression.TokenType)
            {
                case TokenType.Plus:
                    logger.Log("add");
                    _ilGenerator.Emit(OpCodes.Add);
                    break;
                case TokenType.Minus:
                    logger.Log("sub");
                    _ilGenerator.Emit(OpCodes.Sub);
                    break;
                case TokenType.Star:
                    logger.Log("mul");
                    _ilGenerator.Emit(OpCodes.Mul);
                    break;
                case TokenType.Slash:
                    logger.Log("div");
                    _ilGenerator.Emit(OpCodes.Div);
                    break;
            }

            return binaryExpression;
        }

        public ReturnStatement Visit(ReturnStatement returnStatement)
        {
            Visit((dynamic) returnStatement.Returned);
            logger.Log("ret");
            _ilGenerator.Emit(OpCodes.Ret);
            return returnStatement;
        }

        public AssignmentStatement Visit(AssignmentStatement assignmentStatement)
        {
            Visit((dynamic) assignmentStatement.Right);
            var count = Array.IndexOf(_localVariables.Select(x => x.Key).ToArray(), assignmentStatement.Left.Name);
            logger.Log($"stloc {count}");
            _ilGenerator.Emit(OpCodes.Stloc, count);
            return assignmentStatement;
        }

        public UnaryExpression Visit(UnaryExpression unaryExpression)
        {
            var expression = Visit((dynamic) unaryExpression.Expression);
            logger.Log("neg");
            _ilGenerator.Emit(OpCodes.Neg);
            return new UnaryExpression(expression);
        }

        private void IntEmit(int value)
        {
            switch (value)
            {
                case 0:
                    logger.Log("ldc.i4.0");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    logger.Log("ldc.i4.1");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    logger.Log("ldc.i4.2");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    logger.Log("ldc.i4.3");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    logger.Log("ldc.i4.4");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    logger.Log("ldc.i4.5");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    logger.Log("ldc.i4.6");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    logger.Log("ldc.i4.7");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    logger.Log("ldc.i4.8");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value >= -128 && value <= 128)
                    {
                        logger.Log($"ldc.i4.s {value}");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte) value);
                    }
                    else
                    {
                        logger.Log($"ldc.i4 {value}");
                        _ilGenerator.Emit(OpCodes.Ldc_I4, value);
                    }

                    break;
            }
        }

        public PrimaryExpression Visit(PrimaryExpression primaryExpression)
        {
            if (primaryExpression.ReturnType == CompilerType.Long)
            {
                logger.Log($"ldc.i8 {primaryExpression.AsLong()}");
                _ilGenerator.Emit(OpCodes.Ldc_I8, primaryExpression.AsLong());
            }
            else if (primaryExpression.ReturnType == CompilerType.Int)
            {
                IntEmit(primaryExpression.AsInt());
                logger.Log("conv.i8");
                _ilGenerator.Emit(OpCodes.Conv_I8);
            }
            else if (primaryExpression.ReturnType == CompilerType.UInt)
            {
                var q = primaryExpression.AsUInt();
                if (q == uint.MaxValue)
                {
                    logger.Log("ldc.I4.m1");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_M1);
                }
                else
                {
                    IntEmit(int.MinValue + (int) (q - int.MaxValue - 1));
                }

                logger.Log("conv.u8");
                _ilGenerator.Emit(OpCodes.Conv_U8);
            }

            return primaryExpression;
        }

        public MethodCallExpression Visit(MethodCallExpression methodCallExpression)
        {
            foreach (var parameter in methodCallExpression.Parameters)
            {
                Visit((dynamic) parameter);
            }

            var method = _closedMethods[methodCallExpression.Name];
            var logParams = string.Join(",", method.GetParameters().Select(x => x.ParameterType.ToString()));
            logger.Log($"call {method.ReturnType} {TestedTypeFullName}::{method.Name}({logParams})");
            _ilGenerator.Emit(OpCodes.Call, _closedMethods[methodCallExpression.Name]);
            return methodCallExpression;
        }

        public VariableExpression Visit(VariableExpression variable)
        {
            var index = Array.IndexOf(_parameters, variable.Name);
            if (index != -1)
            {
                switch (index)
                {
                    case 0:
                        logger.Log($"ldarg.0");
                        _ilGenerator.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        logger.Log($"ldarg.1");
                        _ilGenerator.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        logger.Log($"ldarg.2");
                        _ilGenerator.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        logger.Log($"ldarg.3");
                        _ilGenerator.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        logger.Log($"ldarg.s {variable.Name}");
                        _ilGenerator.Emit(OpCodes.Ldarg_S, variable.Name);
                        break;
                }
            }

            else if (_localVariables.TryGetValue(variable.Name, out var value))
            {
                index = Array.IndexOf(_localVariables.Select(x => x.Key).ToArray(), variable.Name);
                switch (index)
                {
                    case 0:
                        logger.Log($"ldloc.0");
                        _ilGenerator.Emit(OpCodes.Ldloc_0);
                        break;
                    case 1:
                        logger.Log($"ldloc.1");
                        _ilGenerator.Emit(OpCodes.Ldloc_1);
                        break;
                    case 2:
                        logger.Log($"ldloc.2");
                        _ilGenerator.Emit(OpCodes.Ldloc_2);
                        break;
                    case 3:
                        logger.Log($"ldloc.3");
                        _ilGenerator.Emit(OpCodes.Ldloc_3);
                        break;
                    default:
                        logger.Log($"ldloc.s {index}");
                        _ilGenerator.Emit(OpCodes.Ldloc_S, index);
                        break;
                }
            }
            else if (_closureFields.TryGetValue(variable.Name, out var field))
            {
                logger.Log($"ldsfld {field.FieldType} {TestedTypeFullName}::{field.Name}");
                _ilGenerator.Emit(OpCodes.Ldsfld, field);
            }

            return variable;
        }
    }
}