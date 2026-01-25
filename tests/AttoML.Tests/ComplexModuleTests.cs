using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class ComplexModuleTests
    {
        private static void LoadBuiltins(Evaluator ev)
        {
            var baseMod = AttoML.Interpreter.Builtins.BaseModule.Build();
            ev.Modules["Base"] = baseMod;
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set($"Base.{kv.Key}", kv.Value);
            }
            var mathMod = AttoML.Interpreter.Builtins.MathModule.Build();
            ev.Modules["Math"] = mathMod;
            foreach (var kv in mathMod.Members)
            {
                ev.GlobalEnv.Set($"Math.{kv.Key}", kv.Value);
            }
            var listMod = AttoML.Interpreter.Builtins.ListModule.Build();
            ev.Modules["List"] = listMod;
            foreach (var kv in listMod.Members)
            {
                ev.GlobalEnv.Set($"List.{kv.Key}", kv.Value);
            }
            var stringMod = AttoML.Interpreter.Builtins.StringModule.Build();
            ev.Modules["String"] = stringMod;
            foreach (var kv in stringMod.Members)
            {
                ev.GlobalEnv.Set($"String.{kv.Key}", kv.Value);
            }
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
            foreach (var kv in listMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
        }

        private static string LoadComplexPrelude()
        {
            var baseDir = System.AppContext.BaseDirectory;
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var path = System.IO.Path.Combine(repoRoot, "src", "AttoML.Interpreter", "Prelude", "Complex.atto");
            return System.IO.File.ReadAllText(path);
        }

        [Fact]
        public void ComplexAdd_Works()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(LoadComplexPrelude() + "\nopen Complex\nadd (C (1.0, 2.0)) (C (3.0, 4.0))");
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadAdts(mods);
            ev.LoadModules(mods);
            ev.ApplyOpen(decls);
            Assert.True(ev.GlobalEnv.TryGet("C", out var ctor));
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            Assert.Equal("C", av.Ctor);
            var t = Assert.IsType<TupleVal>(av.Payload);
            Assert.Equal(1.0 + 3.0, ((FloatVal)t.Items[0]).Value);
            Assert.Equal(2.0 + 4.0, ((FloatVal)t.Items[1]).Value);
        }

        [Fact]
        public void ComplexMul_Works()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(LoadComplexPrelude() + "\nopen Complex\nmul (C (2.0, 3.0)) (C (4.0, 5.0))");
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadAdts(mods);
            ev.LoadModules(mods);
            ev.ApplyOpen(decls);
            Assert.True(ev.GlobalEnv.TryGet("C", out var ctor));
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            var t = Assert.IsType<TupleVal>(av.Payload);
            // (2+3i)*(4+5i) = (8 - 15) + (10 + 12)i = -7 + 22i
            Assert.Equal(-7.0, ((FloatVal)t.Items[0]).Value);
            Assert.Equal(22.0, ((FloatVal)t.Items[1]).Value);
        }

        [Fact]
        public void ComplexConj_MagnitudeSquared_Works()
        {
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(LoadComplexPrelude() + "\nopen Complex\nlet c = C (3.0, 0.0 - 4.0) in magnitudeSquared (conj c)");
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadAdts(mods);
            ev.LoadModules(mods);
            ev.ApplyOpen(decls);
            Assert.True(ev.GlobalEnv.TryGet("C", out var ctor));
            var v = ev.Eval(expr!, ev.GlobalEnv);
            // conj(3,-4) = (3,4); magnitudeSquared = 3*3 + 4*4 = 25
            var fv = Assert.IsType<FloatVal>(v);
            Assert.Equal(25.0, fv.Value);
        }
    }
}
