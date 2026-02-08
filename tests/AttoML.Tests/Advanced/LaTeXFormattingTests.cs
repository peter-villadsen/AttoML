using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Advanced
{
    public class LaTeXFormattingTests : AttoMLTestBase
    {
        private static string LoadPrelude()
        {
            var baseDir = System.AppContext.BaseDirectory;
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var symCalcPath = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "SymCalc.atto");
            var optionPath = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "Option.atto");
            var egraphPath = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "EGraph.atto");
            var rewritePath = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "LaTeXRewrite.atto");
            var latexPath = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "LaTeX.atto");
            return System.IO.File.ReadAllText(optionPath) + "\n" +
                   System.IO.File.ReadAllText(symCalcPath) + "\n" +
                   System.IO.File.ReadAllText(egraphPath) + "\n" +
                   System.IO.File.ReadAllText(rewritePath) + "\n" +
                   System.IO.File.ReadAllText(latexPath);
        }

        [Fact]
        public void LaTeX_Const_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.toLatex (Expr.Const 42.0)");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            Assert.Equal("42.0", strVal.Value);
        }

        [Fact]
        public void LaTeX_Var_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.toLatex (Expr.Var ""x"")");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            Assert.Equal("x", strVal.Value);
        }

        [Fact]
        public void LaTeX_Add_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.toLatex (Expr.Add (Expr.Var ""x"", Expr.Var ""y""))");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            Assert.Equal("x + y", strVal.Value);
        }

        [Fact]
        public void LaTeX_Div_UsesFrac()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.toLatex (Expr.Div (Expr.Var ""x"", Expr.Var ""y""))");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            Assert.Equal("\\frac{x}{y}", strVal.Value);
        }

        [Fact]
        public void LaTeX_Mul_SmartSeparator()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.toLatex (Expr.Mul (Expr.Const 2.0, Expr.Var ""x""))");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            // Should be "2.0x" without cdot
            Assert.Equal("2.0x", strVal.Value);
        }

        [Fact]
        public void LaTeX_Pow_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.toLatex (Expr.Pow (Expr.Var ""x"", Expr.Const 2.0))");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            Assert.Equal("x^{2.0}", strVal.Value);
        }

        [Fact]
        public void LaTeX_Sin_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.toLatex (Expr.Sin (Expr.Var ""x""))");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            Assert.Equal("\\sin(x)", strVal.Value);
        }

        [Fact]
        public void LaTeX_Precedence_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.toLatex (Expr.Mul (Expr.Add (Expr.Var ""a"", Expr.Var ""b""), Expr.Var ""c""))");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            // Should have parens around a+b
            Assert.Contains("(a + b)", strVal.Value);
        }

        [Fact]
        public void LaTeX_FormatOptimized_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.formatOptimized (""x + 0"", 5)");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            // Should optimize away the +0
            Assert.Equal("x", strVal.Value);
        }

        [Fact]
        public void LaTeX_ShowOptimization_ReturnsString()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                LaTeX.showOptimization ""1 * x""");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var strVal = Assert.IsType<StringVal>(v);
            // Should show both original and optimized
            Assert.Contains("Original:", strVal.Value);
            Assert.Contains("Optimized:", strVal.Value);
        }
    }
}
