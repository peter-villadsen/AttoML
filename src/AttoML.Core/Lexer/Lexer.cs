using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AttoML.Core.Lexer
{
    public sealed class Lexer
    {
        private readonly string _text;
        private int _pos;

        public Lexer(string text)
        {
            _text = text ?? string.Empty;
            _pos = 0;
        }

        public IEnumerable<Token> Lex()
        {
            Token t;
            do
            {
                t = NextToken();
                {
                    yield return t;
                }
            } while (t.Kind != TokenKind.EOF);
        }

        private Token NextToken()
        {
            SkipWhitespaceAndComments();
            if (IsEOF)
            {
                return new Token(TokenKind.EOF, string.Empty, _pos);
            }

            char c = Peek();
            int start = _pos;

            // Identifiers and keywords
            if (char.IsLetter(c) || c == '_')
            {
                var id = ReadWhile(ch => char.IsLetterOrDigit(ch) || ch == '_' );
                return new Token(KeywordKind(id), id, start);
            }

            // Numbers: int or float
            if (char.IsDigit(c))
            {
                var num = ReadWhile(ch => char.IsDigit(ch));
                if (!IsEOF && Peek() == '.')
                {
                    Advance();
                    var frac = ReadWhile(ch => char.IsDigit(ch));
                    var text = num + "." + frac;
                    return new Token(TokenKind.FloatLiteral, text, start);
                }
                return new Token(TokenKind.IntLiteral, num, start);
            }

            // String
            if (c == '"')
            {
                Advance();
                var sb = new StringBuilder();
                while (!IsEOF && Peek() != '"')
                {
                    if (Peek() == '\\')
                    {
                        Advance();
                        if (IsEOF)
                        {
                            break;
                        }
                        char esc = Peek();
                        Advance();
                        sb.Append(esc switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '"' => '"',
                            '\\' => '\\',
                            _ => esc
                        });
                    }
                    else
                    {
                        sb.Append(Peek());
                        Advance();
                    }
                }
                if (!IsEOF && Peek() == '"')
                {
                    Advance();
                }
                return new Token(TokenKind.StringLiteral, sb.ToString(), start);
            }

            // Operators and punctuation
            switch (c)
            {
                case '+':
                    Advance();
                    return new Token(TokenKind.Plus, "+", start);
                case '*':
                    Advance();
                    return new Token(TokenKind.Star, "*", start);
                case '/':
                    Advance();
                    return new Token(TokenKind.Slash, "/", start);
                case '^':
                    Advance();
                    return new Token(TokenKind.Caret, "^", start);
                case '(':
                    Advance();
                    return new Token(TokenKind.LParen, "(", start);
                case ')':
                    Advance();
                    return new Token(TokenKind.RParen, ")", start);
                case '{':
                    Advance();
                    return new Token(TokenKind.LBrace, "{", start);
                case '}':
                    Advance();
                    return new Token(TokenKind.RBrace, "}", start);
                case '[':
                    Advance();
                    return new Token(TokenKind.LBracket, "[", start);
                case ']':
                    Advance();
                    return new Token(TokenKind.RBracket, "]", start);
                case ',':
                    Advance();
                    return new Token(TokenKind.Comma, ",", start);
                case '.':
                    Advance();
                    return new Token(TokenKind.ModuleQualifiedSep, ".", start);
                case ':':
                    Advance();
                    return new Token(TokenKind.Colon, ":", start);
                case '=':
                    Advance();
                    if (!IsEOF && Peek() == '=')
                    {
                        Advance();
                        return new Token(TokenKind.EqEq, "==", start);
                    }
                    return new Token(TokenKind.Equals, "=", start);
                case '!':
                    Advance();
                    if (!IsEOF && Peek() == '=')
                    {
                        Advance();
                        return new Token(TokenKind.BangEq, "!=", start);
                    }
                    break;
                case '@':
                    Advance();
                    return new Token(TokenKind.At, "@", start);
                case '<':
                    Advance();
                    if (!IsEOF && Peek() == '=')
                    {
                        Advance();
                        return new Token(TokenKind.LessEqual, "<=", start);
                    }
                    if (!IsEOF && Peek() == '>')
                    {
                        Advance();
                        return new Token(TokenKind.NotEqual, "<>", start);
                    }
                    return new Token(TokenKind.LessThan, "<", start);
                case '>':
                    Advance();
                    if (!IsEOF && Peek() == '=')
                    {
                        Advance();
                        return new Token(TokenKind.GreaterEqual, ">=", start);
                    }
                    return new Token(TokenKind.GreaterThan, ">", start);
                case '|':
                    Advance();
                    return new Token(TokenKind.Bar, "|", start);
                case '-':
                    if (Peek2() == '>')
                    {
                        Advance();
                        Advance();
                        return new Token(TokenKind.Arrow, "->", start);
                    }
                    Advance();
                    return new Token(TokenKind.Minus, "-", start);
            }

            throw new Exception($"Unexpected character '{c}' at position {start}");
        }

        private void SkipWhitespaceAndComments()
        {
            while (!IsEOF)
            {
                if (char.IsWhiteSpace(Peek()))
                {
                    Advance();
                    continue;
                }
                if (Peek() == '/' && Peek2() == '/')
                {
                    while (!IsEOF && Peek() != '\n')
                    {
                        Advance();
                    }
                    continue;
                }
                // Nested block comments: (* ... *) can be nested to any depth
                if (Peek() == '(' && Peek2() == '*')
                {
                    SkipNestedComment();
                    continue;
                }
                break;
            }
        }

        private void SkipNestedComment()
        {
            // Assume current position is at '(' and next is '*'
            // Consume the initial '(*'
            Advance(); // '('
            if (IsEOF) throw new Exception("Unterminated comment");
            Advance(); // '*'
            int depth = 1;
            while (!IsEOF && depth > 0)
            {
                // Lookahead for nested open '(*'
                if (Peek() == '(' && Peek2() == '*')
                {
                    Advance(); // '('
                    if (IsEOF) throw new Exception("Unterminated comment");
                    Advance(); // '*'
                    depth++;
                    continue;
                }
                // Lookahead for close '*)'
                if (Peek() == '*' && Peek2() == ')')
                {
                    Advance(); // '*'
                    if (IsEOF) throw new Exception("Unterminated comment");
                    Advance(); // ')'
                    depth--;
                    continue;
                }
                // Otherwise, skip current char
                Advance();
            }
            if (depth > 0)
            {
                throw new Exception("Unterminated comment");
            }
        }

        private TokenKind KeywordKind(string id)
        {
            return id switch
            {
                "true" => TokenKind.BoolLiteral,
                "false" => TokenKind.BoolLiteral,
                "()" => TokenKind.Unit,
                "type" => TokenKind.Type,
                "datatype" => TokenKind.Type,
                "exception" => TokenKind.Exception,
                "raise" => TokenKind.Raise,
                "handle" => TokenKind.Handle,
                "of" => TokenKind.Of,
                "let" => TokenKind.Let,
                "rec" => TokenKind.Rec,
                "in" => TokenKind.In,
                "fun" => TokenKind.Fun,
                "if" => TokenKind.If,
                "then" => TokenKind.Then,
                "else" => TokenKind.Else,
                "open" => TokenKind.Open,
                "structure" => TokenKind.Structure,
                "signature" => TokenKind.Signature,
                "val" => TokenKind.Val,
                "match" => TokenKind.Match,
                "with" => TokenKind.With,
                "andthen" => TokenKind.AndThen,
                "orelse" => TokenKind.OrElse,
                _ => TokenKind.Identifier,
            };
        }

        private string ReadWhile(Func<char, bool> p)
        {
            int start = _pos;
            while (!IsEOF && p(Peek())) Advance();
            return _text.Substring(start, _pos - start);
        }

        private char Peek() => _text[_pos];
        private char Peek2() => _pos + 1 < _text.Length ? _text[_pos + 1] : '\0';
        private void Advance() => _pos++;
        private bool IsEOF => _pos >= _text.Length;
    }
}
