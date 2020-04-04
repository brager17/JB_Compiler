using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Parser.Bumping;

namespace Bumper
{
    public class Object
    {
        public string Name;
        public long Age;
    }

    class Program
    {
        static void Main(string[] args)
        {
            V();
        }


        private static long t;
        private static long t1;
        private static long t2;

        public static int V()
        {
            if (t == 1 || t > 1 || t1 < 14 || t1 <= 14)
                return 1;
            
            if (t == 1 && t > 1 && t1 < 14 && t1 <= 14)
                return 1;
            
            if (t == 1 && t > 1 && t1 < 14 || t1 <= 14)
                return 1;
            return 3;
        }

        public static void Test()
        {
            var expr =
                @"
            if (t == 1)
            {
                Console.WriteLine(""true"")};
            }
            Console.WriteLine(""result"");";

            var dynamicMethod = new DynamicMethod(
                "method", typeof(void), new Type[] {typeof(int)}
            );

            var generator = dynamicMethod.GetILGenerator();
            var consoleWriteLineMethod = typeof(Console).GetMethods().First(x => x.Name == "WriteLine");
            var result = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Bne_Un_S, result);
            generator.Emit(OpCodes.Ldstr, "true");
            generator.Emit(OpCodes.Call, consoleWriteLineMethod);
            generator.MarkLabel(result);
            generator.Emit(OpCodes.Ldstr, "result");
            generator.Emit(OpCodes.Call, consoleWriteLineMethod);
            generator.Emit(OpCodes.Ret);
            var action = (Action<int>) dynamicMethod.CreateDelegate(typeof(Action<int>));
            action(-1);

            // var result = TestHelper.GeneratedStatementsMySelf(expr, out var func);
        }

        public static Action<T> BuildFieldsPrinter<T>() where T : class
        {
            var type = typeof(T);
            var method = new DynamicMethod(Guid.NewGuid().ToString(), // имя метода
                typeof(void), // возвращаемый тип
                new[] {type}, // принимаемые параметры
                typeof(string), // к какому типу привязать метод, можно указывать, например, string
                true); // просим доступ к приватным полям
            var il = method.GetILGenerator();
            var fieldValue = il.DeclareLocal(typeof(object));
            var toStringMethod = typeof(object).GetMethod("ToString");
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                il.Emit(OpCodes.Ldstr, field.Name + ": {0}"); // stack: [format]
                il.Emit(OpCodes.Ldarg_0); // stack: [format, obj]
                il.Emit(OpCodes.Ldfld, field); // stack: [format, obj.field]
                if (field.FieldType.IsValueType)
                    il.Emit(OpCodes.Box, field.FieldType); // stack: [format, (object)obj.field]
                il.Emit(OpCodes.Dup); // stack: [format, obj.field, obj.field]
                il.Emit(OpCodes.Stloc, fieldValue); // fieldValue = obj.field; stack: [format, obj.field]
                var notNullLabel = il.DefineLabel();
                il.Emit(OpCodes.Brtrue, notNullLabel); // if(obj.field != null) goto notNull; stack: [format]
                il.Emit(OpCodes.Ldstr, "null"); // stack: [format, "null"]
                var printedLabel = il.DefineLabel();
                il.Emit(OpCodes.Br, printedLabel); // goto printed
                il.MarkLabel(notNullLabel);
                il.Emit(OpCodes.Ldloc, fieldValue); // stack: [format, obj.field]
                il.EmitCall(OpCodes.Callvirt, toStringMethod, null); // stack: [format, obj.field.ToString()]
                il.MarkLabel(printedLabel);
                var writeLineMethod =
                    typeof(Console).GetMethod("WriteLine", new[] {typeof(string), typeof(object)});
                il.EmitCall(OpCodes.Call, writeLineMethod,
                    null); // Console.WriteLine(format, obj.field.ToString()); stack: []
            }

            il.Emit(OpCodes.Ret);
            return (Action<T>) method.CreateDelegate(typeof(Action<T>));
        }
    }
}