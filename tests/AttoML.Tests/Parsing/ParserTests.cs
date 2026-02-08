using Xunit;
using AttoML.Core.Lexer;
using AttoML.Core.Parsing;
using System.Linq;

namespace AttoML.Tests.Parsing
{
    public class ParserTests
    {
        private Parser P(string src)
        {
            var lx = new Lexer(src);
            var toks = lx.Lex().ToList();
            return new Parser(toks);
        }

        [Fact]
        public void ParsesFunAndApp()
        {
            var p = P("fun x -> x");
            var (mods, expr) = p.ParseCompilationUnit();
            Assert.NotNull(expr);
            Assert.IsType<Fun>(expr);
        }

        [Fact]
        public void ParsesLetIn()
        {
            var p = P("let x = 1 in x");
            var (mods, expr) = p.ParseCompilationUnit();
            Assert.IsType<Let>(expr);
        }
    }
}
