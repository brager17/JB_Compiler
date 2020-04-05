using System;

namespace Parser.Parser.Exceptions
{
    public class CompileException : Exception
    {
        public CompileException(string message) : base(message)
        {
        }

        public CompileException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}