using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Parser;

namespace Compiler
{
    public static class IExpressionExtensions
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


        public CompileResult Compile(IExpression expression)
        {
            var dynamicMethod = new DynamicMethod(
                "method",
                typeof(long),
                new[] {typeof(long), typeof(long), typeof(long)});

            var visitor = new CompileExpressionVisitor(dynamicMethod, new[] {"x", "y", "z"}, null, null);
            visitor.Start((BinaryExpression) expression);
            return (CompileResult) dynamicMethod.CreateDelegate(typeof(CompileResult), null);
        }
    }

    public class CompileExpressionVisitor
    {
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
        private readonly Dictionary<string, FieldInfo> _closedFields;
        private readonly string[] _closedMethods;
        private readonly string[] _variables;
        private ILGenerator _ilGenerator;

        public Logger logger = new Logger();
        // for tests

        public CompileExpressionVisitor(
            DynamicMethod dynamicMethod,
            string[] parameters,
            Dictionary<string, FieldInfo> closedFields,
            string[] closedMethods)
        {
            _parameters = parameters;
            _closedFields = closedFields;
            _closedMethods = closedMethods;
            _ilGenerator = dynamicMethod.GetILGenerator();
        }

        public void Start(BinaryExpression binaryExpression)
        {
            Visit(binaryExpression);
            logger.Log("ret");
            _ilGenerator.Emit(OpCodes.Ret);
        }

        public void Visit(BinaryExpression binaryExpression)
        {
            Visit((dynamic) binaryExpression.Left);
            Visit((dynamic) binaryExpression.Right);
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
        }

        public void Visit(UnaryExpression unaryExpression)
        {
            Visit((dynamic) unaryExpression.Expression);
            logger.Log("neg");
            _ilGenerator.Emit(OpCodes.Neg);
        }

        public void Visit(PrimaryExpression primaryExpression)
        {
            if (primaryExpression.Value > int.MaxValue)
            {
                logger.Log($"ldc.i8 {primaryExpression.Value}");
                _ilGenerator.Emit(OpCodes.Ldc_I8, primaryExpression.Value);
            }
            else
            {
                switch (primaryExpression.Value)
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
                        if (primaryExpression.Value < 128)
                        {
                            logger.Log($"ldc.i4.s {primaryExpression.Value}");
                            _ilGenerator.Emit(OpCodes.Ldc_I4_S, primaryExpression.Value);
                        }
                        else
                        {
                            logger.Log($"ldc.i4 {primaryExpression.Value}");
                            _ilGenerator.Emit(OpCodes.Ldc_I4, primaryExpression.Value);
                        }

                        break;
                }

                logger.Log("conv.i8");
                _ilGenerator.Emit(OpCodes.Conv_I8);
            }
        }

        public void Visit(MethodCallExpression methodCallExpression)
        {
        }

        public void Visit(VariableExpression variable)
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
                        logger.Log($"ldarg.5");
                        _ilGenerator.Emit(OpCodes.Ldarg_S, variable.Name);
                        break;
                }
            }
            else if (_closedFields.TryGetValue(variable.Name, out var value))
            {
                logger.Log($"ldsfld {value.FieldType} {value.DeclaringType}::{value.Name}");
                _ilGenerator.Emit(OpCodes.Ldsfld, value);
            }
        }
    }
}