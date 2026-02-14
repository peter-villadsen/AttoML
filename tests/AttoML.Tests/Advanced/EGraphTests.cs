using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Advanced
{
    public class EGraphTests : AttoMLTestBase
    {
        private static string LoadPrelude()
        {
            var baseDir = System.AppContext.BaseDirectory;
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var symCalcPath = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "SymCalc.atto");
            var optionPath = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "Option.atto");
            var egraphPath = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "EGraph.atto");
            return System.IO.File.ReadAllText(optionPath) + "\n" +
                   System.IO.File.ReadAllText(symCalcPath) + "\n" +
                   System.IO.File.ReadAllText(egraphPath);
        }

        [Fact]
        public void EGraph_Empty_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + "\nlet eg = EGraph.empty 0 in EGraph.nextId eg");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var intVal = Assert.IsType<IntVal>(v);
            Assert.Equal(0, intVal.Value);
        }

        [Fact]
        public void EGraph_AddConst_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                let eg0 = EGraph.empty 0 in
                let (id, eg1) = EGraph.add (eg0, Expr.Const 42.0) in
                id");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var intVal = Assert.IsType<IntVal>(v);
            Assert.Equal(0, intVal.Value);
        }

        [Fact]
        public void EGraph_AddMultiple_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                let eg0 = EGraph.empty 0 in
                let (id1, eg1) = EGraph.add (eg0, Expr.Const 1.0) in
                let (id2, eg2) = EGraph.add (eg1, Expr.Const 2.0) in
                EGraph.nextId eg2");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var intVal = Assert.IsType<IntVal>(v);
            Assert.Equal(2, intVal.Value);
        }

        [Fact]
        public void EGraph_HashConsing_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                let eg0 = EGraph.empty 0 in
                let (id1, eg1) = EGraph.add (eg0, Expr.Const 42.0) in
                let (id2, eg2) = EGraph.add (eg1, Expr.Const 42.0) in
                id1 = id2");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var boolVal = Assert.IsType<BoolVal>(v);
            Assert.True(boolVal.Value);
        }

        [Fact]
        public void EGraph_Union_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                let eg0 = EGraph.empty 0 in
                let (id1, eg1) = EGraph.add (eg0, Expr.Const 1.0) in
                let (id2, eg2) = EGraph.add (eg1, Expr.Const 2.0) in
                let eg3 = EGraph.union (eg2, id1, id2) in
                let (c1, eg4) = EGraph.find (eg3, id1) in
                let (c2, eg5) = EGraph.find (eg4, id2) in
                c1 = c2");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var boolVal = Assert.IsType<BoolVal>(v);
            Assert.True(boolVal.Value);
        }

        [Fact]
        public void EGraph_Extract_Simple_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                let costFn = fun node -> 1.0 in
                let eg0 = EGraph.empty 0 in
                let (id, eg1) = EGraph.add (eg0, Expr.Const 42.0) in
                let result = EGraph.extract (eg1, costFn, id) in
                match result with Expr.Const r -> r | _ -> 0.0 end");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var floatVal = Assert.IsType<FloatVal>(v);
            Assert.Equal(42.0, floatVal.Value);
        }

        [Fact]
        public void EGraph_AddExpression_Works()
        {
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(LoadPrelude() + @"
                let eg0 = EGraph.empty 0 in
                let e = Expr.Add (Expr.Const 1.0, Expr.Const 2.0) in
                let (id, eg1) = EGraph.add (eg0, e) in
                EGraph.nextId eg1");
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var intVal = Assert.IsType<IntVal>(v);
            // Should have 3 e-classes: const 1.0, const 2.0, and add
            Assert.Equal(3, intVal.Value);
        }
    }
}
