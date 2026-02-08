using System.Linq;
using Xunit;
using AttoML.Core.Lexer;

namespace AttoML.Tests.Parsing
{
    public class LexerTests
    {
        [Fact]
        public void LexesIdentifiersAndLiterals()
        {
            var lx = new Lexer("let x = 42 in x");
            var toks = lx.Lex().ToList();
            var expected = new (TokenKind kind, string text)[]
            {
                (TokenKind.Let, "let"),
                (TokenKind.Identifier, "x"),
                (TokenKind.Equals, "="),
                (TokenKind.IntLiteral, "42"),
                (TokenKind.In, "in"),
                (TokenKind.Identifier, "x"),
                (TokenKind.EOF, string.Empty)
            };
            Assert.Equal(expected.Length, toks.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].kind, toks[i].Kind);
                Assert.Equal(expected[i].text, toks[i].Text);
            }
        }

        [Fact]
        public void LexesStringLiteral()
        {
            var lx = new Lexer("\"hello\"");
            var toks = lx.Lex().ToList();
            Assert.Contains(toks, t => t.Kind == TokenKind.StringLiteral && t.Text == "hello");
        }

        [Fact]
        public void LexesFloatLiteral()
        {
            var lx = new Lexer("3.14");
            var toks = lx.Lex().ToList();
            Assert.Contains(toks, t => t.Kind == TokenKind.FloatLiteral && t.Text == "3.14");
        }

        [Fact]
        public void LexesBoolLiterals()
        {
            var lxTrue = new Lexer("true");
            var toksTrue = lxTrue.Lex().ToList();
            Assert.Contains(toksTrue, t => t.Kind == TokenKind.BoolLiteral && t.Text == "true");

            var lxFalse = new Lexer("false");
            var toksFalse = lxFalse.Lex().ToList();
            Assert.Contains(toksFalse, t => t.Kind == TokenKind.BoolLiteral && t.Text == "false");
        }
    }
}
