using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class StringModuleTests
    {
        [Fact]
        public void StringConcat_Works()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls, mods, expr, type) = fe.Compile("String.concat \"a\" \"b\"");
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("ab", sv.Value);
        }

        [Fact]
        public void StringLength_Works()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls, mods, expr, type) = fe.Compile("String.length \"hello\"");
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var iv = Assert.IsType<IntVal>(v);
            Assert.Equal(5, iv.Value);
        }

        [Fact]
        public void StringSize_Works()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls, mods, expr, type) = fe.Compile("String.size \"hello\"");
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var iv = Assert.IsType<IntVal>(v);
            Assert.Equal(5, iv.Value);
        }

        [Fact]
        public void StringSub_Works()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls, mods, expr, type) = fe.Compile("String.sub \"abc\" 1");
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("b", sv.Value);
        }

        [Fact]
        public void StringSubstring_Works()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls, mods, expr, type) = fe.Compile("String.substring \"abcdef\" 2 3");
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("cde", sv.Value);
        }

        [Fact]
        public void StringExplode_Implode_RoundTrip()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls1, mods1, expr1, type1) = fe.Compile("String.explode \"hi\"");
            ev.LoadModules(mods1);
            var v1 = ev.Eval(expr1!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v1);
            Assert.Equal(2, list.Items.Count);
            Assert.Equal("h", ((StringVal)list.Items[0]).Value);
            Assert.Equal("i", ((StringVal)list.Items[1]).Value);

            var (decls2, mods2, expr2, type2) = fe.Compile("String.implode [\"h\", \"i\"]");
            ev.LoadModules(mods2);
            var v2 = ev.Eval(expr2!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v2);
            Assert.Equal("hi", sv.Value);
        }

        [Fact]
        public void StringConcatList_Works()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls, mods, expr, type) = fe.Compile("String.concatList [\"a\", \"b\", \"c\"]");
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("abc", sv.Value);
        }

        [Fact]
        public void StringIsPrefix_IsSuffix_Contains_Work()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            var (decls1, mods1, expr1, type1) = fe.Compile("String.isPrefix \"pre\" \"prefix\"");
            ev.LoadModules(mods1);
            var v1 = ev.Eval(expr1!, ev.GlobalEnv);
            Assert.True(((BoolVal)v1).Value);

            var (decls2, mods2, expr2, type2) = fe.Compile("String.isSuffix \"fix\" \"prefix\"");
            ev.LoadModules(mods2);
            var v2 = ev.Eval(expr2!, ev.GlobalEnv);
            Assert.True(((BoolVal)v2).Value);

            var (decls3, mods3, expr3, type3) = fe.Compile("String.contains \"ef\" \"prefix\"");
            ev.LoadModules(mods3);
            var v3 = ev.Eval(expr3!, ev.GlobalEnv);
            Assert.True(((BoolVal)v3).Value);
        }

        [Fact]
        public void StringTranslate_Works()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            // Duplicate each character
            var (decls, mods, expr, type) = fe.Compile("String.translate (fun c -> String.concat c c) \"ab\"");
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("aabb", sv.Value);
        }

        [Fact]
        public void StringCompare_Works()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            Program_LoadBuiltins(ev);

            // less than
            {
                var (decls, mods, expr, type) = fe.Compile("String.compare \"a\" \"b\"");
                ev.LoadModules(mods);
                var v = ev.Eval(expr!, ev.GlobalEnv);
                var iv = Assert.IsType<IntVal>(v);
                Assert.True(iv.Value < 0);
            }
            // equal
            {
                var (decls, mods, expr, type) = fe.Compile("String.compare \"a\" \"a\"");
                ev.LoadModules(mods);
                var v = ev.Eval(expr!, ev.GlobalEnv);
                var iv = Assert.IsType<IntVal>(v);
                Assert.True(iv.Value == 0);
            }
            // greater than
            {
                var (decls, mods, expr, type) = fe.Compile("String.compare \"b\" \"a\"");
                ev.LoadModules(mods);
                var v = ev.Eval(expr!, ev.GlobalEnv);
                var iv = Assert.IsType<IntVal>(v);
                Assert.True(iv.Value > 0);
            }
        }

        private static void Program_LoadBuiltins(Evaluator ev)
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
    }
}
