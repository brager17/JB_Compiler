using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compiler;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Parser.Exceptions;
using Xunit;

namespace Parser
{
    public class StatementTests
    {
        [Fact]
        public void Parse__DefineVariable__Correct()
        {
            string expr = "long q = 12;long w = -14;return q+w;";
            var result = TestHelper.GetParseResultStatements(expr);

            var logs = TestHelper.GeneratedStatementsMySelf(expr, out var func);
            var roslyn = TestHelper.GeneratedRoslynExpression("q+w",
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Fact]
        public void Statement()
        {
            long n = 123L;
            int m = 123;
        }

        [Fact]
        public void Parse__DefineVariablesWithExpression__Correct()
        {
            string expr = @"
                    long q = 12*x;
                    long w = -14+12;
                    return 2;
                    ";
            var result = TestHelper.GetParseResultStatements(expr);


            Assert.Equal(ExpressionType.Assignment, result[0].ExpressionType);
            var qAssignment = (AssignmentStatement) result[0];
            Assert.Equal("q", qAssignment.Left.Name);
            Assert.Equal(ExpressionType.Binary, qAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((BinaryExpression) qAssignment.Right).Left.ExpressionType);
            Assert.Equal(12, ((PrimaryExpression) ((BinaryExpression) qAssignment.Right).Left).AsLong());
            Assert.Equal(ExpressionType.Variable, ((BinaryExpression) qAssignment.Right).Right.ExpressionType);
            Assert.Equal("x", ((VariableExpression) ((BinaryExpression) qAssignment.Right).Right).Name);

            Assert.Equal(ExpressionType.Assignment, result[1].ExpressionType);
            var qqAssignment = (AssignmentStatement) result[1];
            Assert.Equal("w", qqAssignment.Left.Name);
            Assert.Equal(ExpressionType.Unary, qqAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((UnaryExpression) qqAssignment.Right).Expression.ExpressionType);
            Assert.Equal(2, ((PrimaryExpression) ((UnaryExpression) qqAssignment.Right).Expression).AsLong());
        }


        [Fact]
        public void Parse__DefineVariablesWithExpressionUsingBeforeDefinedVariable__Correct()
        {
            string expr = @"
                    long q = 12*x;
                    long w = -q;
                    return 1;
                    ";
            var result = TestHelper.GetParseResultStatements(expr);

            Assert.Equal(ExpressionType.Assignment, result[0].ExpressionType);
            var qAssignment = (AssignmentStatement) result[0];
            Assert.Equal("q", qAssignment.Left.Name);
            Assert.Equal(ExpressionType.Binary, qAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Primary, ((BinaryExpression) qAssignment.Right).Left.ExpressionType);
            Assert.Equal(12, ((PrimaryExpression) ((BinaryExpression) qAssignment.Right).Left).AsLong());
            Assert.Equal(ExpressionType.Variable, ((BinaryExpression) qAssignment.Right).Right.ExpressionType);
            Assert.Equal("x", ((VariableExpression) ((BinaryExpression) qAssignment.Right).Right).Name);

            Assert.Equal(ExpressionType.Assignment, result[1].ExpressionType);
            var qqAssignment = (AssignmentStatement) result[1];
            Assert.Equal("w", qqAssignment.Left.Name);
            Assert.Equal(ExpressionType.Unary, qqAssignment.Right.ExpressionType);
            Assert.Equal(ExpressionType.Variable, ((UnaryExpression) qqAssignment.Right).Expression.ExpressionType);
            Assert.Equal("q", ((VariableExpression) ((UnaryExpression) qqAssignment.Right).Expression).Name);
        }

        [Fact]
        public void Parse__LocalVariableCalledAsParameter__ThrowException()
        {
            var expr = "int a = 12;return a;";
            var ex = Assert.Throws<CompileException>(() => TestHelper.GeneratedStatementsMySelf(expr, out var func,
                new Dictionary<string, CompilerType> {{"a", CompilerType.Int}}));
            Assert.Equal(
                "A local or parameter named 'a' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter",
                ex.Message);
        }


        [Fact]
        public void Parse__LocalVariableCalledAsStaticField__PreferLocalVariable()
        {
            var expr = "int a = 12;return a;";
            TestHelper.GeneratedStatementsMySelf(expr, out var func,
                new Dictionary<string, CompilerType>(),
                new Dictionary<string, CompilerType>() {{"a", CompilerType.Int}});
            Assert.Equal(12, func(1, 2, 3));
        }

        [Fact]
        public void Parse__ParameterCalledAsStaticField__PreferLocalParameter()
        {
            var expr = "return a;";
            var lexer = new Lexer(expr);
            var tokens = lexer.ReadAll();
            var context = new ParserContext(tokens, new Dictionary<string, CompilerType>() {{"a", CompilerType.Int}},
                null, null);
            var parser = new Parser(context);
            var result = new ILCompiler().Compile<Func<int, int>>(parser.Parse(), new[] {"a"});
            Assert.Equal(12, result(12));
        }

        [Fact]
        // todo: сделать ```int q = 12``` , где int q - VariableDeclaration, а в ```q=12``` ` q - Variable 
        public void AssignmentTest()
        {
            var expr = "int q=12;q=11;return q;";
            var result = TestHelper.GetParseResultStatements(expr);

            Assert.Equal(ExpressionType.Assignment, result[0].ExpressionType);
            Assert.Equal(ExpressionType.Assignment, result[1].ExpressionType);
            Assert.Equal(ExpressionType.Return, result[2].ExpressionType);
        }

        [Theory]
        [InlineData(
            @"long u = 8-(936*y*y)+5714-z+90-(x*811+x-967*x+04+(006)-z)-7598756*(05180369395*y)+(z)*(y-539053+(y-91+(5+x)+y));long i = y+3353646661*(31275125821+y+u*y*(z-9948+y*496-x-678*y-731*y*z*0-u+x+3528)*x*x)*z*y-2682+y*461*(413*u);long r = i+i-z+y+7316055209*y-83169-z-9874618929-z+874477-y+958*z*96*(37954*u-739+x)-x*6-x-(z)*z*3+y*470649;long t = 101-z-813-i-0028+u-6614*r+9312742627382125261+y*i-915*u+5-z+129-z+i*842*x+r+7887-x+8238*y+y-228+i-(r);long m = u*6027457203124*r*052386670*i*i+i-r-62*(3742+(6*u-7274268-i*12-(u+i)+u-t-1+r)-48)+5-(717*i*485206408);long b = r*t-t+06751+z*y+0*m-m*z+286-i-567-t*y*13035133*x+t+u*u+t-u*(t-i+850*x+9652*m-55*t*x+t*95+z-79-y)+038;long d = 3751-y+082417188921*m-69700+m*2541-u+1245394-(16377)-(4+(3498*m))*52*r-m*5306088544905+y-z+1+r*(4-m);long e = 0*m*b+7064589+b*b+7263*x-z-(62+t)-i-y+(m)*9*m*d-7543624+x-r+32213-d+6250533+t+b*3+t+5474-y-b*i*(r*35);long o = b+063929853-e-444-m-r+93511479488701550+z*t*t*233+t-0*i+r+x+17*(y*t+b-7)*0-i*y+567-d-y+22+y-1-r-2175;long a = x-e*x*736*d*b+o+o*7*i*x*90661047372-i*x-x+93-x*u+3+z*r-i-256+y+(091472787200846)+(o*u-x+m-46)+d*9290;long g = a*b-m+(m-745)+5-y*02-o-u-1+o-d+5832+b+t-823+a-e+i+r-i-852*e*x*284662663+m+a*m-r+i*14-(115752245*e*e);long h = 56259*u-41-r-m*u+0638213814815128*z-8645331522-x+x-y*771296441*z*321-u+d+z*d+2+m+g-m*b*x-4-(9)+i+i*2;long k = h*o-24-y+77+a+d-m+z*a-i+i-d+76+m-(83*i+i)*z-z-t+45*a*6684-u+r*i-615*i*b+i*5644*a-b-m-t*h*x*x+99+d*37;long l = k*260+h+e*153665746-o+664918*h-a+82-b-9*a-r*03-x+z+a-252+x+b-x+h-t-h+k+e-h-i*9+y*b*z-6740+y-e*h*g-d;long s = o-(i+(15910+i)+5184-b+3089*e)+d-x*917889566605774182456262102-h-889386+m*k-11+l+z-x-a*51+e-e-k-l*u*o;long j = a+026798-h*l*14864+m+m+k+s-(m)+x+34410014124-l+m+d*(31*u-u+i-d*i*51)+y-g+(5)*a-a-m-7+e+94100*u+t*t+m;long c = x*z+b-u+a+g+i+d-843158-(3+b+h-t+964393-g)-6033419*z-a+9-o+d+z+j*x-y-2733372+x*g+h+u+d*095287-l-7+s+i;long n = 39-r*4+l*z*u-1+h-i*05444933946+x*2+i*7150377250386*z+u*r-k+g+y+r+21557867*d+36*t+y+44985+j*e-61*t-35;long v = u-e*u*201+k+8-k*r*u-(t)*o+u+238130854816+o*u+x+l*24+z-u*445+s*s*i-j-d+e+n-g+b-j-64-x*50-i*49*t*e*u*4;long p = x-37-t+c*97-c-a-n+l-t*a-421-n-t+s-z-i+z+t+k-j+i*y-h*33509*(1)+(z)-c-d+k+b-b+d*y-m-x-t+g+472911*u-g*9;
return k*9-p-j+x-4231309+c-p+u-c+z-t+u-h-a-208+h-37664745*p*91251472+z*s-x+x*h*u*7*r+5840949*c-y*x-(u*c-812);")]
        [InlineData(
            @"
                long k = 69 - y * y - 59 * y - (1780 - x) + 48749929 - z * 843 * (418 - x) + 5347 * (49404 * x - 7) +
                z * 8613584 + z * (7 + z) + y - z + (3526 - y + 272694 - y);
            long f = 0245691 - (4632 + x) + 4 * y + y * 497865125298 + z * 577 + y - 8 - z * 201 + z - 103969 + z -
                2 * (x - y + 10) - 2 - (k * 7 * x - 0 + y + 74634 + k) + 4729;
            long t = 91 + x + 7 - x + k + z * 1 + (x - 692117 * y + 6 + f * 0 - f - k + 4025786216312 + (8675 + y)) -
                     (x * 36 + (9882 * z - 4452957365743 + (z)) - 617 * x);
            long m = 8596095620 + (9 + x + 8 - x + 93 * x + 82 * x - x - 4) - 59931664 + t + 565880 + t -
                (1841437 + y * z + 33 - (38109 * z) + 44 * k * f) - x + z - k * t + 26;
            long v = 320 + x + (x + t + 5246242) * 45 - t * t + t + 173 * x - x - m * z * k + 7 - t + 59 -
                z * 5 * y * m - z * z + 952641 + t * k - 6932 * f + t - m * z + 59735 + y * x * f - (m);
            long r = 9 + k + v * v + 85918 * f - 367 * t * (873) + (7 + z * 0) - 1 - x - 6766487 + t - k * y +
                7042099 * t + 4672 * v * 25327 + m - 8595 - m + k - 25 - y * k - 2 * t + 6;
            long e = (288 + v * m + 353848842 * r - 67) + t * x * r * x + 7 + z - t * k - 6 + t -
                     4862 * r * 49 * m * 767644574 - ((1106) - 3701220545 + z - 32154938026);
            long j = 4139458145579 - m - 4141 - z * 7 * y - 4 + k - e - 29937136 * t - v + 897074 + m + 02563 * y - m +
                f + m + 1889 + m * 323 - z - 863 + f + (e * 6 + e - 19 * t);
            long g = 295844888 + f - y - v - 097718 + k - v + z - 1 + t - 71 * j - 20 * f * 67505289 - m * 7681100620 +
                j + f + k - y + j - 99 * m * y - 345996 + v + j + e + e * 278;
            long o = 14 * t - e + f * t + 83395574 - k + j + (f) * 98 * z * 0 - g - x - 765246 + e * 9 + (297 + v + z) +
                e + t * y * 881 + z - r - r - 1813306884004462 - z - j - 038;
            long a = 864 * v - z + 316 - y - 88 -
                m * 34222 * ((4 * v * 77425 - (k * g * 3) * y + 0418680) * t + x * 81 + z) * r - 917 * k +
                m * 137 - o * t * 37 - m - 61 + o - 1 - e - z;
            long s = k + z - x * (0 * f * g - 9 + k) - 913086 + (6411) - a + (x - 2 * m) * y + 54 * r + 0 + k + 438114 -
                     (y - 6109 - m + 5 * z * o * f * 8371360627666 - y + g) - t * a;
            long b = 5791 + (f - f * 81) - t * z * t + 263072525 + a - j + k + g + s - a -
                4 * (826537556 * y - 245066 - s - 750 - r) + 57091089 + a + v + 86 - y * 369 - o * 114;
            long n = 10732 + m - e + e + g * b * s + 6 - j - 52207098 - (89 * t) + g - 103839 * a * y + r -
                (2 - v - k + v + a - z - m * 10) - 1131 + (72 + x * 78721 * x) - 70 * j * x * k;
            long c = 6546 + m - o - f - t + 621 + a - 9 * a - g - 3 - f + k - 86 - r + t - 0 - m + s + r * 37 - k - t -
                40 - x + f + r - t + 012 - m + f - y + 0802 - o + v + x * e - 881 - x + g * 8 - f - 8;
            long p = z - 9 * e * v - m - s + k * (m - 68561) - x - g - r + g * 6224831862 + a * e + c * x - 1199762 -
                z - 0 * (96073 - m) - 8800046 * b * v * 13825721 * m + r + 11;
            long u = c - m - 1 + k * c - v - k * x + 0 * r + 7703744985 - e * o - a * c - 1946035 - p * b - t - g +
                2156 * r + z * r * 26041 - n * k + f + y + a * 82 + p + s - 6 - x * t + 03695;
            long q = 8737 * t * g * b - (4) * m * 6 - t - z - g - s + 999515768 * m + v + z - (o) - 139 + x + v -
                5 * k + (88279 * (9 * y + b * 6 + v - 8)) * c - 99412186 + x - e - 187;
            long l = v - v * 931 + e * 778 - t - 4546 * y - 18728010 * x * o - u + c * 727873 - e * k - 8 + q -
                t * b * t + b * k - 17 + b * 97 - o * 40592698 - c + t + o + n + z - 72461;
            long d = m * y - 9 - g + t - m - v + m - r + j * b -
                     4 * (00 * p * 34827333676 * o + 87 - y * 3491954 + u * r) + a - 054826 + p + p + r +
                     (r + q + (y + 8097 * e * 73928 - j));
            return 9233495 * v - 382348811852 + a * v + z * n - 861450772 + s + n + 532 - n - u - e - p - 6517507 - q +
                x * 85 + o * 4 + k - p * (0) + 88328 + t - m - q - g - a;"
        )]
        public void Test(string expr)
        {
            // TestHelper.GeneratedRoslyn(
            //     @"9233495 * v - 382348811852 + a * v + z * n - 861450772 + s + n + 532 - n - u - e - p - 6517507 - q +
            // x * 85 + o * 4 + k - p * (0) + 88328 + t - m - q - g - a;", out var func, expr.Split(';').SkipLast(1).ToArray());
            // TestHelper.GeneratedStatementsMySelf(expr, out var myFunc);
        }


        [Fact]
        // todo если в двух разных Statement'aх объявляется переменная с одним и тем же именем, то падает "Variable with name 'q' is already declared"
        // if (x == 1)
        //{{
        //   int q = 21;
        //}}
        //else
        //{{
        //    int q = 21;
        //}}
        //
        //
        public void IfStatementTests()
        {
            var methods = new Dictionary<string, (CompilerType[], CompilerType)>()
            {
                {"Print", (new CompilerType[] { }, CompilerType.Void)}
            };
            var expr =
                $@"
            if (x == 1)
            {{
                Print();
                Print();
                int q = 21;
            }}
            else
            {{
                Print();
                Print();
                int w = 21;
                return 12;
            }}
            return 1;
            ";

            var r = TestHelper.GetParseResultStatements(expr, methods: methods);
            Assert.Equal(ExpressionType.IfElse, r[0].ExpressionType);
            Assert.Equal(3, ((IfElseStatement) r[0]).IfTrue.Statements.Length);
            Assert.Equal(4, ((IfElseStatement) r[0]).Else.Statements.Length);
            Assert.True(((IfElseStatement) r[0]).Else.IsReturnStatement);
        }


        [Fact]
        public void DefineBooleanVariableTest()
        {
            var expr =
                $@"
            bool boolTrue = true;
            bool boolFalse = false;
            return 1;
            ";

            var r = TestHelper.GetParseResultStatements(expr);
            Assert.Equal(ExpressionType.Assignment, r[0].ExpressionType);
            Assert.Equal("boolTrue", ((AssignmentStatement) r[0]).Left.Name);
            Assert.Equal(ExpressionType.Assignment, r[1].ExpressionType);
            Assert.Equal("boolFalse", ((AssignmentStatement) r[1]).Left.Name);
        }
    }
}