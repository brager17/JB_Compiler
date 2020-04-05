#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using Parser.Lexer;
using Parser.Parser.Exceptions;

namespace Parser.Parser
{
    public class SemanticTokenSequence
    {
        private readonly Token[] _tokens;

        public SemanticTokenSequence(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens.ToArray();
        }

        private int _currentIndex;
        public Token? Current => Get(0);
        public Token? Next => Get(1);

        public void Step(int i = 1)
        {
            if (IsEmpty) throw new Exception("sequence is empty");
            for (; i > 0; i--) _currentIndex++;
        }

        public Token? Get(int i)
        {
            if (_currentIndex + i >= _tokens.Length)
                return null;
            return _tokens[_currentIndex + i];
        }

        public bool StepIfMatch(TokenType type)
        {
            if (Current == null) return false;
            if (Current.Type != type) return false;
            _currentIndex++;
            return true;
        }

        public Token? CurrentWithStep()
        {
            if (Current == null)
            {
                throw new Exception("Sequence ends");
            }

            var result = Current;
            _currentIndex++;
            return result;
        }

        internal string GetCurrentSubstring()
        {
            var start = _currentIndex > 5 ? _currentIndex - 5 : 0;

            var end = _tokens.Length - _currentIndex > 5 ? _currentIndex + 5 : _tokens.Length;

            return string.Join(" ", _tokens[start .. end].Select(x => x.Value ?? GetDisplayName(x.Type)));
        }

        private string GetDisplayName(TokenType type)
        {
            return typeof(TokenType).GetMember(type.ToString()).Single()
                .GetCustomAttribute<DisplayAttribute>()
                ?.Name;
        }

        public bool IsEmpty => _currentIndex == _tokens.Length;

        public bool IsTypeKeyWord() =>
            Current.Type == TokenType.IntWord || Current.Type == TokenType.LongWord ||
            Current.Type == TokenType.BoolWord;

        public bool IsMethod() => Current?.Type == TokenType.Word && Next.Type == TokenType.LeftParent;
    }
}