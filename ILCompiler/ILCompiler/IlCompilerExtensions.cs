using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Microsoft.VisualBasic.FileIO;
using Parser.Lexer;
using Parser.Parser.Expressions;
using Parser.Utils;

namespace Parser.ILCompiler
{
    public class Logger
    {
        private List<string> _logger = new List<string>();

        public void Log(string log)
        {
            _logger.Add(log);
        }

        public string[] GetLogs => _logger.ToArray();
    }

    public class IlEmitter
    {
        internal readonly ILGenerator _ilGenerator;
        // for tests
        private const string TestedTypeFullName = "RunnerNamespace.Runner";

        public IlEmitter(ILGenerator generator)
        {
            _ilGenerator = generator;
        }


        public Logger _logger = new Logger();

        public void Emit(OpCode opCodes)
        {
            _logger.Log(opCodes.Name);
            _ilGenerator.Emit(opCodes);
        }

        public void EmitLogicalOperator(LogicalBinaryExpression logical)
        {
            switch (logical.Operator)
            {
                case Operator.Less:
                    Emit(OpCodes.Clt);
                    break;
                case Operator.LessOrEq:
                    Emit(OpCodes.Cgt);
                    Emit(OpCodes.Ldc_I4_0);
                    Emit(OpCodes.Ceq);
                    break;
                case Operator.Greater:
                    Emit(OpCodes.Cgt);
                    break;
                case Operator.GreaterOrEq:
                    Emit(OpCodes.Clt);
                    Emit(OpCodes.Ldc_I4_0);
                    Emit(OpCodes.Ceq);
                    break;
                case Operator.Eq:
                    Emit(OpCodes.Ceq);
                    break;
                case Operator.NoEq:
                    Emit(OpCodes.Ceq);
                    Emit(OpCodes.Ldc_I4_0);
                    Emit(OpCodes.Ceq);
                    break;
            }
        }

        public Label DefineLabel() => _ilGenerator.DefineLabel();

        public void MarkLabel(Label label, string labelString)
        {
            _logger.Log("mark " + labelString);
            _ilGenerator.MarkLabel(label);
        }

        public void BrFalseS(Label label, string labelString)
        {
            _logger.Log($"brfalse {labelString}");
            _ilGenerator.Emit(OpCodes.Brfalse_S, label);
        }

        public void Br(Label label, string labelString)
        {
            _logger.Log($"br {nameof(labelString)}");
            _ilGenerator.Emit(OpCodes.Br, label);
        }

        public void LdLoc(int index)
        {
            switch (index)
            {
                case 0:
                    Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    _logger.Log($"ldloc.s {index}");
                    _ilGenerator.Emit(OpCodes.Ldloc_S, index);
                    break;
            }
        }

        public void LdLocByReference(int index)
        {
            _logger.Log($"ldloca.s {index}");
            _ilGenerator.Emit(OpCodes.Ldloca_S, index);
        }

        public void ldsfld(FieldVariableExpression exp)
        {
            _logger.Log($"ldsfld {exp.FieldInfo.FieldType} {TestedTypeFullName}::{exp.Name}");
            _ilGenerator.Emit(OpCodes.Ldsfld, exp.FieldInfo);
        }

        public void ldsfldByReference(FieldVariableExpression exp)
        {
            _logger.Log($"ldsflda {exp.Name}");
            _ilGenerator.Emit(OpCodes.Ldsflda, exp.FieldInfo);
        }

        public void LoadArg(MethodArgumentVariableExpression expression)
        {
            switch (expression.Index)
            {
                case 0:
                    Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    _logger.Log($"ldarg.s {expression.Name}");
                    _ilGenerator.Emit(OpCodes.Ldarg_S, expression.Name);
                    break;
            }
        }

        public void LoadArgByReference(MethodArgumentVariableExpression expression)
        {
            _logger.Log($"ldarga.s {expression.Name}");
            _ilGenerator.Emit(OpCodes.Ldarga_S, (byte) expression.Index);
        }

        public void LoadOperation(TokenType type)
        {
            switch (type)
            {
                case TokenType.Plus:
                    Emit(OpCodes.Add);
                    break;
                case TokenType.Minus:
                    Emit(OpCodes.Sub);
                    break;
                case TokenType.Star:
                    Emit(OpCodes.Mul);
                    break;
                case TokenType.Slash:
                    Emit(OpCodes.Div);
                    break;
            }
        }

        public void SetLocalVariable(LocalVariableExpression localVariableExpression)
        {
            switch (localVariableExpression.Index)
            {
                case 0:
                    _ilGenerator.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    _ilGenerator.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    _ilGenerator.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    _ilGenerator.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    _ilGenerator.Emit(OpCodes.Stloc, localVariableExpression.Name);
                    break;
            }
        }

        public void SetField(FieldVariableExpression fieldVariableExpression)
        {
            _ilGenerator.Emit(OpCodes.Stsfld, fieldVariableExpression.FieldInfo);
        }

        public void SetArg(MethodArgumentVariableExpression argumentVariableExpression)
        {
            _ilGenerator.Emit(OpCodes.Starg_S, (byte) argumentVariableExpression.Index);
        }

        public void BrTrue(Label label)
        {
            _ilGenerator.Emit(OpCodes.Brtrue, label);
        }

        public void BrFalse(Label label)
        {
            _ilGenerator.Emit(OpCodes.Brfalse, label);
        }

        public void LdcI8(long l)
        {
            _logger.Log($"ldc.i8 {l}");
            _ilGenerator.Emit(OpCodes.Ldc_I8, l);
        }

        public void LdcI4(int value)
        {
            _logger.Log($"ldc.i4 {value}");
            _ilGenerator.Emit(OpCodes.Ldc_I4, value);
        }

        public void LoadInt(int value)
        {
            switch (value)
            {
                case 0:
                    Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value > -128 && value < 128)
                    {
                        _logger.Log($"ldc.i4.s {value}");
                        _ilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte) value);
                    }
                    else
                    {
                        _logger.Log($"ldc.i4 {value}");
                        _ilGenerator.Emit(OpCodes.Ldc_I4, value);
                    }

                    break;
            }
        }

        public void MethodCall(MethodCallExpression methodCallExpression)
        {
            var logParams = string.Join(",",
                methodCallExpression.Parameters.ToArray().Select(x => x.ParameterInfo.ParameterType.ToString()));
            _logger.Log(
                $"call {methodCallExpression.MethodInfo.ReturnType} {TestedTypeFullName}::{methodCallExpression.Name}({logParams})");
            _ilGenerator.Emit(OpCodes.Call, methodCallExpression.MethodInfo);
        }

        public void DeclareLocal(CompilerType compilerType)
        {
            _ilGenerator.DeclareLocal(compilerType.GetCSharpType());
        }
    }
}