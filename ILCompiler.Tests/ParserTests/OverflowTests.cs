using System;
using System.Reflection;
using Parser.Parser.Exceptions;
using Parser.Parser.Expressions;
using Xunit;
using UnaryExpression = Parser.Parser.Expressions.UnaryExpression;

namespace Parser.Tests.ParserTests
{
    public class OverflowTests
    {
        [Theory]
        [InlineData("int.MaxValue+1")]
        [InlineData("(int.MaxValue/2+1)+(int.MaxValue/2+1)")]
        [InlineData("int.MaxValue-(-int.MaxValue)+1")]
        [InlineData("int.MinValue-int.MaxValue")]
        [InlineData("int.MinValue*int.MaxValue")]
        [InlineData("(long.MaxValue-int.MaxValue)*2")]
        [InlineData("long.MaxValue+1")]
        [InlineData("long.MinValue-1")]
        [InlineData("long.MinValue*long.MinValue")]
        [InlineData("long.MaxValue*(-2)")]
        public void Parser__OverflowingComping__ThrowException(string expr)
        {
            var exception = Assert.Throws<CompileException>(() => TestHelper.GetParseResultExpression(expr));
            Assert.Contains("The operation is overflow in compile mode", exception.Message);
        }


        [Theory]
        [InlineData(
            "1/(7700)*75915098131+y-z-031856*z+(99-x)-8823303206/(43)*73279+y+20121-((5-x/757))*4840920-x/5+y*41")]
        [InlineData(
            "221+y*x+742/x+375+x*15-y*304558+z-6521375758/((5))*330260-x*513/z-45170*y-y/x+z+0557603*(x-107/y-2*z)")]
        [InlineData(
            "z+y-9/y-1-z/115771+x+82877501005/(53)*5-y+8-(2302-(5-y*(3*z-3))+6)-35/(76)*7+y*100800537-y*62241060")]
        [InlineData(
            "z*(0/x*9)*89801/x+x+y+5935+y*6206999/y-7*x*0/(x*29-z*87/x-73/y+(24*(12)*5*(y+08651895648/(795)*88594)))")]
        [InlineData(
            "(87265/(089*z*2+z+6900*z/z*0-z+93-x+8248))+8682659256/(85+(2))*5586441-y/980-y*4695/x-(00/y)-y*y+3")]
        [InlineData(
            "81+y*03212-y+3738389313/(541)*(0480)/43770+x-16+z+z*7+y-770*z+32/(6*x)+09-x+03748458713227-z-0-z*474")]
        [InlineData(
            "x-1/z-z*7+x/90+x+2-((x))*5-x+59184368800/(324)*953/x+12+y-1+y+5-x/617*y+z*6123277-y/2612-y-23*x+(119)")]
        [InlineData(
            "47684053512/(66652)*3622/y*x+2113353+z*659528*(37/z-74491*z+143/x+4*x-5-z+z*y+4)/2*(9+z)*7-(761-y/28)")]
        [InlineData(
            "3234441-x*y+495-x-43/y/67-x-0*y+x/3-z+277/z+75122997709/(7733)*729-x/44-(66+z/8/z*92*z-650060)-81857")]
        [InlineData(
            "2-(3302+y-1/z)+8548*z/15-x/65/x/178*y+2074059375077+z+9970214510/(78)*24-z-8*z/40+y*099/(92/z+6/y+2)")]
        [InlineData(
            "6991905050/(84)*374-(085/(y-81+y)/212*y-1-y)/9881*z-1+x-y-(x-111*(81/z/(3*z)))*z+z*5857829*x+(z+5+z)")]
        [InlineData(
            "x+3/(4*x-(17/x-5962472544/(48)*939+y-((75)-73)+x+4*z/6+z/95485*y/3*y-8162497+(x*x*2*z+1782+y/6+(6-y))))")]
        [InlineData(
            "9593/z*884/y/5607317429-(6-(2760)/5-z)*784699-y+3+x-27926851192/(84)*46+z/8969/z-8213*(y*z-5/y/9/z+x)")]
        [InlineData(
            "985*((98)/x*y+(5626514573-(6+y)+569)+42*x/37839-(01*y)-1*(68*y)/5)*1*y/x+248+(8)+7969568136/(4)*49")]
        [InlineData(
            "z-0*y/5573*(76475939/x*y)*2*x/6/z/438833-(y*81-z)/40532+z+05*y-(7+y-y+761)*0+x+35705903+x/7647061767")]
        [InlineData(
            "4877259+y*49-(2897801*y)-25+z/413/y+7835*x/8278+x-1+(z-1492+(8161/y)-1+x+z+152234670/z*19+y)-3505820")]
        public void Parser__NotOverflowingComping__Correct(string expr)
        {
            var actual = Compiler.CompileExpression(expr);
            TestHelper.GeneratedRoslynExpression(expr, out var expected);
            Assert.Equal(expected(17,43,59),actual(17,43,59));
        }

        [Fact]
        public void ConstantIsVeryLarge()
        {
            var s = "36076070326337946946";
            var exception = Assert.Throws<CompileException>(() => TestHelper.GetParseResultExpression(s));
            Assert.Contains("Integral constant is too large", exception.Message);
        }

        [Fact]
        public void IntOverflow()
        {
            var expr = @"42209068 * (95)";
            var exception = Assert.Throws<CompileException>(() => TestHelper.GetParseResultExpression(expr));
            Assert.Contains("The operation is overflow in compile mode", exception.Message);
        }

        [Theory]
        [InlineData("int.MaxValue", CompilerType.Int)]
        [InlineData("2147483647", CompilerType.Int)]
        [InlineData("long.MaxValue", CompilerType.Long)]
        [InlineData("9223372036854775807", CompilerType.Long)]
        [InlineData("0", CompilerType.Int)]
        public void CorrectTypesDefined__PositiveOrNeutralConstants(string expression, CompilerType expected)
        {
            var result = TestHelper.GetParseResultExpression(expression);
            Assert.Equal(expected, ((PrimaryExpression) result).ReturnType);
        }


        [Theory]
        [InlineData("int.MinValue", CompilerType.Int)]
        [InlineData("-2147483648", CompilerType.Int)]
        [InlineData("long.MinValue", CompilerType.Long)]
        [InlineData("-9223372036854775808", CompilerType.Long)]
        public void CorrectTypesDefined__NegativeConstants(string expression, CompilerType expected)
        {
            var result = TestHelper.GetParseResultExpression(expression);
            Assert.Equal(expected, ((PrimaryExpression) ((UnaryExpression) result).Expression).ReturnType);
        }
    }
}