#nullable enable
using System;
using System.Collections.Generic;
using Parser.Lexer;

namespace Parser.Parser
{
    public class TokenSequence
    {
        private readonly IReadOnlyList<Token> _tokens;

        public TokenSequence(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens;
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
            if (_currentIndex + i >= _tokens.Count)
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

        public bool IsEmpty => _currentIndex == _tokens.Count;
    }
}