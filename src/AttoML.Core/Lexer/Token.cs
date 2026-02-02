using System;

namespace AttoML.Core.Lexer
{
    public enum TokenKind
    {
        EOF,
        Identifier,
        IntLiteral,
        FloatLiteral,
        StringLiteral,
        BoolLiteral,
        Unit,
        // Keywords
        Match,
        With,
        Case,
        Type,
        Of,
        Let,
        Rec,
        In,
        Fun,
        Fn,
        If,
        Then,
        Else,
        Open,
        Structure,
        Signature,
        Val,
        Exception,
        Raise,
        Handle,
        ModuleQualifiedSep, // '.'
        // Punctuation
        Plus,
        Minus,
        Star,
        Slash,
        LParen,
        RParen,
        LBrace,
        RBrace,
        LBracket,
        RBracket,
        Comma,
        Arrow, // '->'
        FatArrow, // '=>'
        Equals, // '='
        EqEq,   // '=='
        BangEq, // '!='
        NotEqual, // '<>'
        Colon,
        ColonColon, // '::'
        At,    // '@'
        Bar,   // '|'
        LessThan, // '<'
        GreaterThan, // '>'
        LessEqual,   // '<='
        GreaterEqual, // '>='
        AndThen, // keyword 'andthen'
        OrElse,  // keyword 'orelse'
        Caret,   // '^'
    }

    public readonly struct Token
    {
        public TokenKind Kind { get; }
        public string Text { get; }
        public int Position { get; }

        public Token(TokenKind kind, string text, int position)
        {
            Kind = kind;
            Text = text;
            Position = position;
        }

        public override string ToString() => $"{Kind}('{Text}')@{Position}";
    }
}
