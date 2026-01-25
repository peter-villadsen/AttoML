using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class ExceptionsTests
    {
        private static void LoadBuiltinsWithFail(Evaluator ev)
        {
            // Base
            var baseMod = AttoML.Interpreter.Builtins.BaseModule.Build();
            ev.Modules["Base"] = baseMod;
            foreach (var kv in baseMod.Members)
                ev.GlobalEnv.Set($"Base.{kv.Key}", kv.Value);
            foreach (var kv in baseMod.Members)
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            // Math
            var mathMod = AttoML.Interpreter.Builtins.MathModule.Build();
            ev.Modules["Math"] = mathMod;
            foreach (var kv in mathMod.Members)
                ev.GlobalEnv.Set($"Math.{kv.Key}", kv.Value);
            // List
            var listMod = AttoML.Interpreter.Builtins.ListModule.Build();
            ev.Modules["List"] = listMod;
            foreach (var kv in listMod.Members)
                ev.GlobalEnv.Set($"List.{kv.Key}", kv.Value);
            foreach (var kv in listMod.Members)
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            // Built-in Fail : string -> exn at runtime
            ev.GlobalEnv.Set("Fail", new AttoML.Interpreter.Runtime.ClosureVal(arg => new AttoML.Interpreter.Runtime.AdtVal("Fail", arg)));
        }

        [Fact]
        public void BuiltinFailCanBeRaisedAndHandled()
        {
            var src = "((raise (Fail \"nope\")) handle Fail s -> s)";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltinsWithFail(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<StringVal>(v);
            Assert.Equal("nope", ((StringVal)v).Value);
        }

        [Fact]
        public void DeclaredExceptionHandled()
        {
            var src = "exception Oops of int\n((raise (Oops 3)) handle Oops n -> n)";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltinsWithFail(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
        }

        [Fact]
        public void UncaughtExceptionPropagates()
        {
            var src = "exception E\nlet x = raise E in x";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltinsWithFail(ev);
            ev.LoadModules(mods);
            ev.LoadAdts(mods);
            ev.LoadExceptions(mods);
            Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
        }

        [Fact]
        public void RaiseRequiresExnType()
        {
            var fe = new Frontend();
            Assert.Throws<Exception>(() => fe.Compile("raise 1"));
        }
    }
}
