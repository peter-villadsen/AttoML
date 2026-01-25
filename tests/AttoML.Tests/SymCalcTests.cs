using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class SymCalcTests
    {
        private static void LoadBuiltins(Evaluator ev)
        {
            var baseMod = AttoML.Interpreter.Builtins.BaseModule.Build();
            ev.Modules["Base"] = baseMod;
            foreach (var kv in baseMod.Members) ev.GlobalEnv.Set($"Base.{kv.Key}", kv.Value);
            var mathMod = AttoML.Interpreter.Builtins.MathModule.Build();
            ev.Modules["Math"] = mathMod;
            foreach (var kv in mathMod.Members) ev.GlobalEnv.Set($"Math.{kv.Key}", kv.Value);
            var listMod = AttoML.Interpreter.Builtins.ListModule.Build();
            ev.Modules["List"] = listMod;
            foreach (var kv in listMod.Members) ev.GlobalEnv.Set($"List.{kv.Key}", kv.Value);
            var stringMod = AttoML.Interpreter.Builtins.StringModule.Build();
            ev.Modules["String"] = stringMod;
            foreach (var kv in stringMod.Members) ev.GlobalEnv.Set($"String.{kv.Key}", kv.Value);
            // Open Base and List by default
            foreach (var kv in baseMod.Members) ev.GlobalEnv.Set(kv.Key, kv.Value);
            foreach (var kv in listMod.Members) ev.GlobalEnv.Set(kv.Key, kv.Value);
            // Exceptions
            ev.GlobalEnv.Set("Fail", new ClosureVal(arg => new AdtVal("Fail", arg)));
            ev.GlobalEnv.Set("Div", new AdtVal("Div", null));
            ev.GlobalEnv.Set("Domain", new AdtVal("Domain", null));
        }

        private static string LoadSymCalcPrelude()
        {
            var baseDir = AppContext.BaseDirectory;
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var path = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "SymCalc.atto");
            return System.IO.File.ReadAllText(path);
        }

        [Fact]
        public void Parse_RespectsPrecedence()
        {
            var fe = new Frontend();
            var src = LoadSymCalcPrelude() + "\nopen SymCalc\nlet e = parse \"2 + 3 * 4\" in\n toString e";
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var s = Assert.IsType<StringVal>(v).Value;
            Assert.Equal("2.0 + 3.0 * 4.0", s);
        }

        [Fact]
        public void Diff_Simplify_Polynomial()
        {
            var fe = new Frontend();
            var src = LoadSymCalcPrelude() + "\nopen SymCalc\n toString (simplify (diff (\"x\", parse \"x ^ 3\")))";
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var s = Assert.IsType<StringVal>(v).Value;
            Assert.Equal("3.0 * x ^ 2.0", s);
        }

        [Fact]
        public void Diff_Trigonometric()
        {
            var fe = new Frontend();
            var src = LoadSymCalcPrelude() + "\nopen SymCalc\n toString (simplify (diff (\"x\", parse \"sin(x) / x\")))";
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var s = Assert.IsType<StringVal>(v).Value;
            // Quotient rule and chain rule; formatting check on structure
            Assert.Contains("/", s);
            Assert.Contains("sin(x)", s);
            Assert.Contains("cos(x)", s);
        }

        [Fact]
        public void Simplify_Constants_And_DomainChecks()
        {
            var fe = new Frontend();
            var src = LoadSymCalcPrelude() + "\nopen SymCalc\n toString (simplify (parse \"(x * 1) + 0\"))";
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var s = Assert.IsType<StringVal>(v).Value;
            Assert.Equal("x", s);

            // Division by zero constant fold should raise Div
            var src2 = LoadSymCalcPrelude() + "\nopen SymCalc\n simplify (parse \"1 / 0\")";
            var (decls2, mods2, expr2, type2) = fe.Compile(src2);
            ev.LoadModules(mods2);
            ev.LoadAdts(mods2);
            ev.ApplyOpen(decls2);
            Assert.Throws<AttoException>(() => ev.Eval(expr2!, ev.GlobalEnv));
        }
    }
}
