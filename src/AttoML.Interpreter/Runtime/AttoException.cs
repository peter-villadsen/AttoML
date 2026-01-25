using System;

namespace AttoML.Interpreter.Runtime
{
    // Public exception type to carry AttoML exceptions as runtime values
    public sealed class AttoException : Exception
    {
        public Value Exn { get; }
        public AttoException(Value exn) : base($"exception {exn}")
        {
            Exn = exn;
        }
    }
}
