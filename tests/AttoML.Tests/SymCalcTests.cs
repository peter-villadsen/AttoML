using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class SymCalcTests : AttoMLTestBase
    {
        private static string LoadSymCalcPrelude()
        {
            var baseDir = AppContext.BaseDirectory;
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var path = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "SymCalc.atto");
            return System.IO.File.ReadAllText(path);
        }

        private void LoadExceptionBuiltins(Evaluator ev)
        {
            ev.GlobalEnv.Set("Fail", new ClosureVal(arg => new AdtVal("Fail", arg)));
            ev.GlobalEnv.Set("Div", new AdtVal("Div", null));
            ev.GlobalEnv.Set("Domain", new AdtVal("Domain", null));
        }

        [Fact]
        public void Parse_RespectsPrecedence()
        {
            var src = LoadSymCalcPrelude() + "\nopen SymCalc\nlet e = parse \"2 + 3 * 4\" in\n toString e";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var s = Assert.IsType<StringVal>(v).Value;
            Assert.Equal("2.0 + 3.0 * 4.0", s);
        }

        [Fact]
        public void Diff_Simplify_Polynomial()
        {
            var src = LoadSymCalcPrelude() + "\nopen SymCalc\n toString (simplify (diff (\"x\", parse \"x ^ 3\")))";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var s = Assert.IsType<StringVal>(v).Value;
            Assert.Equal("3.0 * x ^ 2.0", s);
        }

        [Fact]
        public void Diff_Trigonometric()
        {
            var src = LoadSymCalcPrelude() + "\nopen SymCalc\n toString (simplify (diff (\"x\", parse \"sin(x) / x\")))";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var s = Assert.IsType<StringVal>(v).Value;
            Assert.Contains("/", s);
            Assert.Contains("sin(x)", s);
            Assert.Contains("cos(x)", s);
        }

        [Fact]
        public void Simplify_Constants_And_DomainChecks()
        {
            var src = LoadSymCalcPrelude() + "\nopen SymCalc\n toString (simplify (parse \"(x * 1) + 0\"))";
            var (fe, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var s = Assert.IsType<StringVal>(v).Value;
            Assert.Equal("x", s);

            var src2 = LoadSymCalcPrelude() + "\nopen SymCalc\n simplify (parse \"1 / 0\")";
            var (decls2, mods2, expr2, type2) = fe.Compile(src2);
            ev.LoadModules(mods2);
            ev.LoadAdts(mods2);
            ev.ApplyOpen(decls2);
            Assert.Throws<AttoException>(() => ev.Eval(expr2!, ev.GlobalEnv));
        }
    }
}
