using Xunit;
using AttoML.Core.Lexer;
using AttoML.Core.Parsing;
using AttoML.Core.Types;
using System.Linq;

namespace AttoML.Tests.Types
{
    public class TypeInferenceTests
    {
        private (TypeEnv, Expr) ParseExpr(string src)
        {
            var lx = new Lexer(src);
            var toks = lx.Lex().ToList();
            var p = new Parser(toks);
            var (mods, expr) = p.ParseCompilationUnit();
            return (new TypeEnv(), expr!);
        }

        [Fact]
        public void InfersIdentityFunction()
        {
            var (env, expr) = ParseExpr("fun x -> x");
            var ti = new TypeInference();
            var (subst, t) = ti.Infer(env, expr);
            var ts = subst.Apply(t).ToString();
            Assert.Contains("->", ts);
        }

        [Fact]
        public void InfersLetBinding()
        {
            var (env, expr) = ParseExpr("let x = 1 in x");
            var ti = new TypeInference();
            var (subst, t) = ti.Infer(env, expr);
            Assert.Equal("int", subst.Apply(t).ToString());
        }
    }
}
