using System.ComponentModel.DataAnnotations;

namespace Parser.Lexer
{
    public enum TokenType
    {
        [Display(Name = "+")]
        Plus,
        [Display(Name = "-")]
        Minus,
        [Display(Name = "/")]
        Slash,
        [Display(Name = "*")]
        Star,
        [Display(Name = "(")]
        LeftParent,
        [Display(Name = ")")]
        RightParent,
        [Display(Name = "{")]
        LeftBrace,
        [Display(Name = "}")]
        RightBrace,
        Variable,
        Word,
        [Display(Name = ",")]
        Comma,
        Constant,
        IntWord,
        LongWord,
        BoolWord,

        RefWord,
        [Display(Name = ";")]
        Semicolon,
        Assignment,
        [Display(Name = "return")]
        ReturnWord,

        [Display(Name = "if")]
        IfWord,
        [Display(Name = "else")]
        ElseWord,
        [Display(Name = "<")]
        LessThan,
        [Display(Name = "<=")]
        LessThanOrEquals,
        [Display(Name = ">")]
        GreaterThan,
        [Display(Name = ">=")]
        GreaterThanOrEquals,
        [Display(Name = "==")]
        EqualTo,
        [Display(Name = "!=")]
        NotEqualTo,
        Or,
        And,
        Not,
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
        public static Token RefWord = new Token("ref", TokenType.RefWord);

        public static Token LongWord = new Token("long", TokenType.LongWord);
        public static Token BoolWord = new Token("bool", TokenType.BoolWord);

        // todo:: лучше сделать Word, а не Constant
        public static Token IntMaxValue = new Token("int.MaxValue", TokenType.Constant);
        public static Token IntMinValue = new Token("int.MinValue", TokenType.Constant);
        public static Token LongMaxValue = new Token("long.MaxValue", TokenType.Constant);
        public static Token LongMinValue = new Token("long.MinValue", TokenType.Constant);
        public static Token And = new Token("&&", TokenType.And);
        public static Token Or = new Token("||", TokenType.Or);
        public static Token Not = new Token("!", TokenType.Not);
        public static Token True = new Token("true", TokenType.Constant);
        public static Token False = new Token("false", TokenType.Constant);

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
}