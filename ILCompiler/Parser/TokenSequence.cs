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
    public class TokenSequence
    {
        private readonly Token[] _tokens;

        public TokenSequence(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens.ToArray();
        }

        private int _currentIndex;
        public Token? Current => Get(0);
        public Token? Next => Get(1);

        public void Step()
        {
            if (IsEmpty) throw new Exception("sequence is empty");
            _currentIndex++;
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

        public void ThrowIfNotMatched(TokenType type, string message)
        {
            if (IsEmpty || Current.Type != type)
            {
                throw new CompileException($"{message}{GetPlace()}");
            }

            Step();
        }

        public void Throw(string message)
        {
            throw new CompileException($"{message}\n Position: {GetPlace()}");
        }

        private string GetPlace()
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
    }
}