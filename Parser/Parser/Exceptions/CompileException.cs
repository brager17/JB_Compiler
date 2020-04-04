using System;

namespace Parser.Exceptions
{
    public class CompileException : Exception
    {
        public CompileException(string message) : base(message)
        {
        }
    }
}