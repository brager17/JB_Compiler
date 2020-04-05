using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Parser.Parser;

namespace Parser.Lexer
{
    public class Lexer
    {
        private readonly string _program;

        Dictionary<string, Token> keyWordsDictionary = new Dictionary<string, Token>()
        {
            {"==", Token.EqualTo},
            {"!=", Token.NotEqualTo},
            {">=", Token.GreaterThanOrEquals},
            {"<=", Token.LessThanOrEquals},
            {"<", Token.LessThan},
            {">", Token.GreaterThan},
            {"+", Token.Plus},
            {"-", Token.Minus},
            {"*", Token.Star},
            {"/", Token.Slash},
            {"(", Token.LeftParent},
            {")", Token.RightParent},
            {"{", Token.LeftBrace},
            {"}", Token.RightBrace},
            {";", Token.Semicolon},
            {"=", Token.Assignment},
            {",", Token.Comma},
            {"if", Token.IfWord},
            {"else", Token.ElseWord},
            {"int", Token.IntWord},
            {"long", Token.LongWord},
            {"bool", Token.BoolWord},
            {"return", Token.Return},
            {"int.MinValue", Token.IntMinValue},
            {"int.MaxValue", Token.IntMaxValue},
            {"long.MinValue", Token.LongMinValue},
            {"long.MaxValue", Token.LongMaxValue},
            {"||", Token.Or},
            {"&&", Token.And},
            {"ref", Token.RefWord},
            {"!", Token.Not},
            {"true", Token.True},
            {"false", Token.False},
        };

        private List<Token> _tokens;

        string[] SeqKeyWords => keyWordsDictionary.Select(x => x.Key).ToArray();

        public Lexer(string program)
        {
            _program = program;
            _tokens = new List<Token>();
        }

        public IReadOnlyList<Token> Tokenize()
        {
            for (var i = 0; i < _program.Length; i++)
            {
                if (_program[i] == ' ') continue;
                if (TryGetKeyWord(ref i, out var token))
                {
                    if (CheckTheNeed(token))
                    {
                        _tokens.AddRange(ReplaceIfDefinedConstant(token));
                    }
                }
                else if (long.TryParse(_program[i].ToString(), out _))
                {
                    var sb = new StringBuilder();
                    while (i < _program.Length && char.IsDigit(_program[i]))
                    {
                        sb.Append(_program[i]);
                        i++;
                    }

                    i--;
                    _tokens.Add(new Token(sb.ToString(), TokenType.Constant));
                }
                else if (IsMethodName(ref i, out string methodName))
                {
                    _tokens.Add(new Token(methodName, TokenType.Word));
                    _tokens.Add(Token.LeftParent);
                }
                else if (IsVariable(ref i, out var variable))
                {
                    _tokens.Add(new Token(variable, TokenType.Variable));
                }
            }

            return _tokens;
        }

        private bool CheckTheNeed(Token token)
        {
            if (_tokens.LastOrDefault()?.Type == TokenType.RightBrace && token.Type == TokenType.Semicolon)
            {
                return false;
            }

            return true;
        }

        private Token[] ReplaceIfDefinedConstant(Token token)
        {
            if (token.Type == TokenType.Constant)
            {
                var constant = Constants.Dictionary[token.Value];
                var isUnary = constant.toString[0] == '-';
                if (isUnary)
                {
                    return new[] {Token.Minus, new Token(constant.toString[1..], TokenType.Constant)};
                }

                return new[] {new Token(constant.toString, TokenType.Constant)};
            }

            return new[] {token};
        }

        private bool TryGetKeyWord(ref int i, out Token token)
        {
            token = default;
            var j = i;
            var sb = new StringBuilder();
            var matchedTokens = new List<Token>();

            while (true)
            {
                if (j == _program.Length)
                {
                    if (!matchedTokens.Any()) return false;
                    token = matchedTokens.Last();
                    i = j - 1;
                    return true;
                }

                sb.Append(_program[j++]);

                if (!SeqKeyWords.Any(x => x.StartsWith(sb.ToString())))
                {
                    if (!matchedTokens.Any()) return false;
                    // bool boolVariable;
                    if (char.IsLetter(sb[^1]) && (char.IsLetter(sb[^2]))) return false;

                    token = matchedTokens.Last();
                    i = j - 2;
                    return true;
                }

                if (keyWordsDictionary.TryGetValue(sb.ToString(), out var item))
                {
                    matchedTokens.Add(item);
                }
            }
        }


        private bool IsVariable(ref int i, out string name)
        {
            name = default;
            if (char.IsDigit(_program[i])) return false;
            int j = i;
            var sb = new StringBuilder();
            while (j < _program.Length && (char.IsLetter(_program[j]) || char.IsDigit(_program[j])))
            {
                sb.Append(_program[j++]);
            }

            if (sb.Length == 0) return false;
            name = sb.ToString();
            i = --j;
            return true;
        }

        private bool IsMethodName(ref int i, out string methodName)
        {
            methodName = default;
            var j = i;
            if (!char.IsLetter(_program[j]))
            {
                return false;
            }

            var methodNameSb = new StringBuilder(10);
            for (; j < _program.Length && _program[j] != '('; j++)
            {
                if (!(char.IsLetter(_program[j]) || char.IsDigit(_program[j])))
                    return false;
                methodNameSb.Append(_program[j]);
            }

            if (j == _program.Length)
                return false;

            int k = j;
            k++;
            var differenceOpeningClosing = 1;
            var parametersStringBuilder = new StringBuilder(10);
            while (differenceOpeningClosing != 0)
            {
                if (_program[k] == '(')
                    differenceOpeningClosing++;
                else if (_program[k] == ')')
                    differenceOpeningClosing--;
                parametersStringBuilder.Append(_program[k]);
                k++;
            }

            var methodNameToString = methodNameSb.ToString();
            var parametersToString = parametersStringBuilder.ToString();
            var regexMethod = new Regex(@"(?'methodName'[\S]+)\([\s*\S,]*\)");
            var match = regexMethod.Match($"{methodNameToString}({parametersToString})");
            if (match.Success)
            {
                methodName = methodNameToString;
                i = j;
                return true;
            }

            return false;
        }
    }
}