using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        Constant
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
        private string SupportedChars = "+-*/(),";

        public Lexer(string program)
        {
            _program = program;
        }

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
                        ',' => Token.Comma
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
                else if (IsMethodName(i))
                {
                    var sb = new StringBuilder();
                    for (; _program[i] != '('; i++)
                    {
                        sb.Append(_program[i]);
                    }

                    var methodName = sb.ToString();
                    tokens.Add(new Token(methodName, TokenType.Word));
                    tokens.Add(Token.OpeningBracket);
                }
                else if (TryGetConstant(ref i, out var constant))
                {
                    tokens.Add(new Token(constant, TokenType.Constant));
                    i--;
                }
                else if (IsChar(_program[i]))
                {
                    tokens.Add(new Token(_program[i].ToString(), TokenType.Variable));
                }


                // will be supported in the future
            }

            return tokens;
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