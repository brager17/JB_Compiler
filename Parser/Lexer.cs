using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Parser
{
    public enum TokenType
    {
        Plus,
        Minus,
        Slash,
        Star,
        Num,
        LeftParent,
        RightParent,
        LeftBrace,
        RightBrace,
        Variable,
        Word,
        Comma,
        Constant,

        IntWord,
        LongWord,


        Semicolon,
        Assignment,
        ReturnWord,

        IfWord,
        ElseWord,
        LessThan,
        LessThanOrEquals,
        GreaterThan,
        GreaterThanOrEquals,
        EqualTo,
        NotEqualTo,
    }


    public class Token
    {
        public static Token Plus = new Token(TokenType.Plus);
        public static Token Minus = new Token(TokenType.Minus);
        public static Token Slash = new Token(TokenType.Slash);
        public static Token Star = new Token(TokenType.Star);
        public static Token LeftParent = new Token(TokenType.LeftParent);
        public static Token RightParent = new Token(TokenType.RightParent);
        public static Token LeftBrace = new Token(TokenType.LeftBrace);
        public static Token RightBrace = new Token(TokenType.RightBrace);
        public static Token Comma = new Token(TokenType.Comma);
        public static Token Semicolon = new Token(TokenType.Semicolon);
        public static Token Assignment = new Token(TokenType.Assignment);
        public static Token Return = new Token(TokenType.ReturnWord);
        public static Token IfWord = new Token(TokenType.IfWord);
        public static Token ElseWord = new Token(TokenType.ElseWord);
        public static Token EqualTo = new Token(TokenType.EqualTo);
        public static Token NotEqualTo = new Token(TokenType.NotEqualTo);
        public static Token LessThan = new Token(TokenType.LessThan);
        public static Token LessThanOrEquals = new Token(TokenType.LessThanOrEquals);
        public static Token GreaterThan = new Token(TokenType.GreaterThan);
        public static Token GreaterThanOrEquals = new Token(TokenType.GreaterThanOrEquals);
        public static Token IntWord = new Token("int", TokenType.IntWord);

        public static Token LongWord = new Token("long", TokenType.LongWord);

        // todo:: лучше сделать Word, а не Constant
        public static Token IntMaxValue = new Token("int.MaxValue", TokenType.Constant);
        public static Token IntMinValue = new Token("int.MinValue", TokenType.Constant);
        public static Token LongMaxValue = new Token("long.MaxValue", TokenType.Constant);
        public static Token LongMinValue = new Token("long.MinValue", TokenType.Constant);

        public Token(string value)
        {
            Type = TokenType.Num;
            Value = value;
        }

        public Token(string value, TokenType type)
        {
            Type = type;
            Value = value;
        }

        private Token(TokenType type)
        {
            Type = type;
        }

        public TokenType Type { get; }
        public string? Value;
    }

    public class Lexer
    {
        private readonly string _program;

        Dictionary<string, Token> keyWordsDictionary = new Dictionary<string, Token>()
        {
            {"==", Token.EqualTo},
            {"!=", Token.NotEqualTo},
            {">=", Token.GreaterThanOrEquals},
            {"<=", Token.LessThanOrEquals},
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
            {"return", Token.Return},
            {"int.MinValue", Token.IntMinValue},
            {"int.MaxValue", Token.IntMaxValue},
            {"long.MinValue", Token.LongMinValue},
            {"long.MaxValue", Token.LongMaxValue},
        };

        string[] SeqKeyWords => keyWordsDictionary.Select(x => x.Key).ToArray();

        public Lexer(string program)
        {
            _program = program;
        }

//todo поддержать переменные состоящие из нескольких букв
        public IReadOnlyList<Token> ReadAll()
        {
            var tokens = new List<Token>();

            // todo pattern mathicg must be better
            for (var i = 0; i < _program.Length; i++)
            {
                if (TryGetKeyWord(ref i, out var token))
                {
                    tokens.Add(token);
                }
                else if (long.TryParse(_program[i].ToString(), out _))
                {
                    var sb = new StringBuilder();
                    while (i < _program.Length && IsDigit(_program[i]))
                    {
                        sb.Append(_program[i]);
                        i++;
                    }

                    i--;
                    tokens.Add(new Token(sb.ToString()));
                }
                else if (IsMethodName(ref i, out string methodName))
                {
                    tokens.Add(new Token(methodName, TokenType.Word));
                    tokens.Add(Token.LeftParent);
                }
                else if (IsChar(_program[i]))
                {
                    tokens.Add(new Token(_program[i].ToString(), TokenType.Variable));
                }


                // will be supported in the future
            }

            return tokens;
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

        private bool IsDigit(char c) => c >= '0' && c <= '9';
        private bool IsChar(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';

        private bool IsMethodName(ref int i, out string methodName)
        {
            methodName = default;
            var j = i;
            if (!IsChar(_program[j]))
            {
                return false;
            }

            var methodNameSb = new StringBuilder(10);
            for (; j < _program.Length && _program[j] != '('; j++)
            {
                if (!(IsChar(_program[j]) || IsDigit(_program[j])))
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
            // todo should improve regex
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