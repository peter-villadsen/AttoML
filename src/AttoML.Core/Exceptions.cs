using System;

namespace AttoML.Core
{
    /// <summary>
    /// Base exception for all AttoML errors
    /// </summary>
    public class AttoMLException : Exception
    {
        public AttoMLException(string message) : base(message) { }
        public AttoMLException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Exception thrown during lexical analysis
    /// </summary>
    public class LexerException : AttoMLException
    {
        public int Position { get; }

        public LexerException(string message, int position = -1) : base(message)
        {
            Position = position;
        }
    }

    /// <summary>
    /// Exception thrown during parsing
    /// </summary>
    public class ParseException : AttoMLException
    {
        public string? Expected { get; }
        public string? Got { get; }

        public ParseException(string message) : base(message) { }

        public ParseException(string expected, string got)
            : base($"Expected {expected} but got {got}")
        {
            Expected = expected;
            Got = got;
        }
    }

    /// <summary>
    /// Exception thrown during type inference
    /// </summary>
    public class TypeException : AttoMLException
    {
        public TypeException(string message) : base(message) { }
        public TypeException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Exception thrown during module system operations
    /// </summary>
    public class ModuleException : AttoMLException
    {
        public string? ModuleName { get; }

        public ModuleException(string message) : base(message) { }

        public ModuleException(string message, string moduleName) : base(message)
        {
            ModuleName = moduleName;
        }
    }
}
