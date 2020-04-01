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
        OpeningBracket,
        ClosingBracket,
        Variable,
        Word,
        Comma,
        Constant,
        IntWord,
        LongWord,
        Semicolon,
        Assignment,
        Return
    }


    public class Token
    {
        public static Token Plus = new Token(TokenType.Plus);
        public static Token Minus = new Token(TokenType.Minus);
        public static Token Slash = new Token(TokenType.Slash);
        public static Token Star = new Token(TokenType.Star);
        public static Token ClosingBracket = new Token(TokenType.ClosingBracket);
        public static Token OpeningBracket = new Token(TokenType.OpeningBracket);
        public static Token Comma = new Token(TokenType.Comma);
        public static Token Semicolon = new Token(TokenType.Semicolon);
        public static Token Assignment = new Token(TokenType.Assignment);
        public static Token Return = new Token(TokenType.Return);

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
        private string SupportedChars = "+-*/(),;=";

        private readonly Dictionary<string, TokenType> KeyWords = new Dictionary<string, TokenType>
        {
            {"int", TokenType.IntWord},
            {"long", TokenType.LongWord},
            {"return", TokenType.Return}
        };

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
                if (SupportedChars.Contains(_program[i]))
                {
                    tokens.Add(_program[i] switch
                    {
                        '+' => Token.Plus,
                        '-' => Token.Minus,
                        '*' => Token.Star,
                        '/' => Token.Slash,
                        '(' => Token.OpeningBracket,
                        ')' => Token.ClosingBracket,
                        ',' => Token.Comma,
                        ';' => Token.Semicolon,
                        '=' => Token.Assignment
                    });
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
                    tokens.Add(Token.OpeningBracket);
                }
                else if (TryGetConstant(ref i, out var constant))
                {
                    tokens.Add(new Token(constant, TokenType.Constant));
                    i--;
                }
                else if (TryGetKeyWord(ref i, out var nametype))
                {
                    tokens.Add(new Token(nametype.name, nametype.type));
                }
                else if (IsChar(_program[i]))
                {
                    tokens.Add(new Token(_program[i].ToString(), TokenType.Variable));
                }


                // will be supported in the future
            }

            return tokens;
        }

        private bool TryGetKeyWord(ref int i, out (string name, TokenType type) nameType)
        {
            nameType = default;

            var sb = new StringBuilder(4);
            var j = i;
            for (; j < _program.Length && _program[j] != ' '; j++)
                sb.Append(_program[j]);

            if (KeyWords.TryGetValue(sb.ToString(), out var tokenType))
            {
                nameType = (sb.ToString(), tokenType);
                i = j;
                return true;
            }

            return false;
        }

        private bool IsDigit(char c) => c >= '0' && c <= '9';
        private bool IsChar(char c) => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';

        // todo кейсы, когда во вложанном выражении есть expression's не рабоатет
        private bool IsMethodName(int i)
        {
            if (!IsChar(_program[i]))
            {
                return false;
            }

            i++;
            for (; i < _program.Length && _program[i] != '('; i++)
            {
                if (!(IsChar(_program[i]) || IsDigit(_program[i])))
                    return false;
            }

            if (i == _program.Length) return false;

            i++;

            for (; i < _program.Length && _program[i] != ')'; i++)
            {
                if (!(IsChar(_program[i]) || IsDigit(_program[i]) || _program[i] == ',' || _program[i] == '-'))
                    return false;
            }

            return true;
        }

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

        private bool TryGetConstant(ref int i, out string constant)
        {
            constant = null;
            var names = Constants.Dictionary.Select(x => x.Key).ToList();
            var sb = new StringBuilder();
            foreach (var name in names)
            {
                if (_program.Length - i < name.Length)
                    continue;

                for (int j = i; j < i + name.Length; j++)
                {
                    sb.Append(_program[j]);
                }

                if (sb.ToString() == name)
                {
                    constant = name;
                    i += name.Length;
                    return true;
                }

                sb.Clear();
            }

            return false;
        }
    }
}