using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Principal;
using Parser;

namespace Compiler
{
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


        public CompileResult Compile(Statement statements)
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

        public T Compile<T>(Statement statements, string[] methodParameters) where T : Delegate
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


    public partial class CompileExpressionVisitor : ExpressionVisitor
    {
        public Dictionary<string, CompilerType> Variables { get; }

        public class Logger
        {
            private List<string> _logger = new List<string>();

            public void Log(string log)
            {
                _logger.Add(log);
            }

            public string[] GetLogs => _logger.ToArray();
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


        public void Start(Statement statements)
        {
            _localVariables = statements
                .Statements
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
                    CompilerType.Bool => typeof(bool),
                    // CompilerType.UInt => typeof(uint),
                    _ => throw new ArgumentOutOfRangeException()
                };

                _ilGenerator.DeclareLocal(type);
            }

            foreach (var statement in statements.Statements)
            {
                VisitStatement(statement);
            }
        }

        public override LogicalBinaryExpression VisitLogical(LogicalBinaryExpression logical)
        {
            VisitExpression(logical.Left);
            VisitExpression(logical.Right);
            switch (logical.Operator)
            {
                case LogicalOperator.Less:
                    logger.Log("clt");
                    _ilGenerator.Emit(OpCodes.Clt);
                    break;
                case LogicalOperator.LessOrEq:
                    logger.Log("cgt");
                    _ilGenerator.Emit(OpCodes.Cgt);
                    logger.Log("ldc.i4.0");
                    logger.Log("ceq");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    _ilGenerator.Emit(OpCodes.Ceq);
                    break;
                case LogicalOperator.Greater:
                    logger.Log("cgt");
                    _ilGenerator.Emit(OpCodes.Cgt);
                    break;
                case LogicalOperator.GreaterOrEq:
                    logger.Log("clt");
                    _ilGenerator.Emit(OpCodes.Clt);
                    logger.Log("ldc.i4.0");
                    logger.Log("ceq");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    _ilGenerator.Emit(OpCodes.Ceq);
                    break;
                case LogicalOperator.Eq:
                    logger.Log("ceq");
                    _ilGenerator.Emit(OpCodes.Ceq);
                    break;
                case LogicalOperator.NoEq:
                    logger.Log("ceq");
                    _ilGenerator.Emit(OpCodes.Ceq);
                    logger.Log("ldc.i4.0");
                    logger.Log("ceq");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    _ilGenerator.Emit(OpCodes.Ceq);
                    break;
                case LogicalOperator.And:
                    logger.Log("and");
                    _ilGenerator.Emit(OpCodes.And);
                    break;
                case LogicalOperator.Or:
                    logger.Log("or");
                    _ilGenerator.Emit(OpCodes.Or);
                    break;
            }

            return logical;
        }

        public override IfElseStatement VisitIfElse(IfElseStatement statement)
        {
            VisitExpression(statement.Test);
            var @startEnd = _ilGenerator.DefineLabel();
            var @elseStart = statement.Else == null ? startEnd : _ilGenerator.DefineLabel();
            logger.Log($"brfalse {nameof(elseStart)}");
            _ilGenerator.Emit(OpCodes.Brfalse_S, elseStart);
            VisitStatement(statement.IfTrue);
            if (!statement.IfTrue.IsReturnStatement)
            {
                logger.Log($"br {nameof(startEnd)}");
                _ilGenerator.Emit(OpCodes.Br, startEnd);
            }

            if (statement.Else != null)
            {
                logger.Log($"mark {nameof(elseStart)}");
                _ilGenerator.MarkLabel(elseStart);
                VisitStatement(statement.Else);
                logger.Log($"mark {nameof(startEnd)}");
                _ilGenerator.MarkLabel(startEnd);
            }
            else
            {
                logger.Log($"mark {nameof(startEnd)}");
                _ilGenerator.MarkLabel(startEnd);
            }


            return statement;
        }

        public void Start(IExpression expression)
        {
            VisitExpression(expression);
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

        public override BinaryExpression VisitBinary(BinaryExpression binaryExpression)
        {
            var left = VisitExpression(binaryExpression.Left);
            if (binaryExpression.Left.ReturnType == CompilerType.Int &&
                binaryExpression.Right.ReturnType == CompilerType.Long)
            {
                logger.Log("conv.i8");
                _ilGenerator.Emit(OpCodes.Conv_I8);
            }

            var right = VisitExpression(binaryExpression.Right);
            if (binaryExpression.Left.ReturnType == CompilerType.Long &&
                binaryExpression.Right.ReturnType == CompilerType.Int)
            {
                logger.Log("conv.i8");
                _ilGenerator.Emit(OpCodes.Conv_I8);
            }

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

        public override ReturnStatement VisitReturn(ReturnStatement returnStatement)
        {
            VisitExpression(returnStatement.Returned);
            logger.Log("ret");
            _ilGenerator.Emit(OpCodes.Ret);
            return returnStatement;
        }

        public override AssignmentStatement VisitAssignment(AssignmentStatement assignmentStatement)
        {
            VisitExpression(assignmentStatement.Right);
            var count = Array.IndexOf(_localVariables.Select(x => x.Key).ToArray(), assignmentStatement.Left.Name);
            logger.Log($"stloc {count}");
            _ilGenerator.Emit(OpCodes.Stloc, count);
            return assignmentStatement;
        }

        public override UnaryExpression VisitUnary(UnaryExpression unaryExpression)
        {
            var expression = VisitExpression(unaryExpression.Expression);
            switch (unaryExpression.UnaryType)
            {
                case UnaryType.Negative:
                {
                    logger.Log("neg");
                    _ilGenerator.Emit(OpCodes.Neg);
                    return new UnaryExpression(expression, UnaryType.Negative);
                }
                case UnaryType.Not:
                {
                    logger.Log("Ldc.I4.0");
                    _ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    logger.Log("ceq");
                    _ilGenerator.Emit(OpCodes.Ceq);
                    return new UnaryExpression(expression, UnaryType.Not);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                    if (value > -128 && value < 128)
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

        public override PrimaryExpression VisitPrimary(PrimaryExpression primaryExpression)
        {
            switch (primaryExpression.ReturnType)
            {
                case CompilerType.Long:
                    logger.Log($"ldc.i8 {primaryExpression.AsLong()}");
                    _ilGenerator.Emit(OpCodes.Ldc_I8, primaryExpression.AsLong());
                    break;
                case CompilerType.Int:
                    IntEmit(primaryExpression.AsInt());
                    break;
                case CompilerType.Bool:
                    var value = primaryExpression.AsBool();
                    logger.Log(value ? "ldc.i4.1" : "ldc.i4.0");
                    _ilGenerator.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return primaryExpression;
        }

        public override MethodCallExpression VisitMethod(MethodCallExpression methodCallExpression)
        {
            var method = _closedMethods[methodCallExpression.Name];
            var methodParams = method.GetParameters();

            for (var i = 0; i < methodCallExpression.Parameters.Count; i++)
            {
                var expression = methodCallExpression.Parameters[i];
                VisitExpression(expression);
                if (expression.ReturnType == CompilerType.Int && methodParams[i].ParameterType == typeof(long))
                {
                    logger.Log("conv.i8");
                    _ilGenerator.Emit(OpCodes.Conv_I8);
                }
            }

            var logParams = string.Join(",", method.GetParameters().Select(x => x.ParameterType.ToString()));
            logger.Log($"call {method.ReturnType} {TestedTypeFullName}::{method.Name}({logParams})");
            _ilGenerator.Emit(OpCodes.Call, _closedMethods[methodCallExpression.Name]);
            return methodCallExpression;
        }

        public override VariableExpression VisitVariable(VariableExpression variable)
        {
            if (variable.ByReference)
            {
                if (_parameters.Contains(variable.Name))
                {
                    var indexParameter = Array.IndexOf(_parameters, variable.Name);
                    logger.Log($"ldarga.s {variable.Name}");
                    _ilGenerator.Emit(OpCodes.Ldarga_S, (byte) indexParameter);
                    return variable;
                }

                if (_localVariables.TryGetValue(variable.Name, out _))
                {
                    var indexLocal = Array.IndexOf(_localVariables.Select(x => x.Key).ToArray(), variable.Name);
                    logger.Log($"ldloca.s {indexLocal}");
                    _ilGenerator.Emit(OpCodes.Ldloca_S, indexLocal);
                    return variable;
                }

                if (_closureFields.TryGetValue(variable.Name, out var fieldInfo))
                {
                    logger.Log($"ldsflda {variable.Name}");
                    _ilGenerator.Emit(OpCodes.Ldsflda, fieldInfo);
                    return variable;
                }
            }

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