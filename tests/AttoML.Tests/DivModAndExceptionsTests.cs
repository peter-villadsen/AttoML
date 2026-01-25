using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class DivModAndExceptionsTests
    {
        private static void LoadBuiltins(Evaluator ev)
        {
            var baseMod = AttoML.Interpreter.Builtins.BaseModule.Build();
            ev.Modules["Base"] = baseMod;
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set($"Base.{kv.Key}", kv.Value);
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
            var mathMod = AttoML.Interpreter.Builtins.MathModule.Build();
            ev.Modules["Math"] = mathMod;
            foreach (var kv in mathMod.Members)
            {
                ev.GlobalEnv.Set($"Math.{kv.Key}", kv.Value);
            }
            // Exceptions available
            ev.GlobalEnv.Set("Div", new AdtVal("Div", null));
            ev.GlobalEnv.Set("Domain", new AdtVal("Domain", null));
            ev.GlobalEnv.Set("Fail", new ClosureVal(arg => new AdtVal("Fail", arg)));
        }

        [Fact]
        public void InfixDivAndModWork()
        {
            var src = "10 div 3";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);

            var src2 = "10 mod 3";
            var (decls2, mods2, expr2, type2) = fe.Compile(src2);
            ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods2);
            var v2 = ev.Eval(expr2!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v2);
            Assert.Equal(1, ((IntVal)v2).Value);
        }

        [Fact]
        public void DivByZeroWithSlashRaisesDiv()
        {
            var src = "(1 / 0) handle Div -> 42";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void DivByZeroWithDivRaisesDiv()
        {
            var src = "(10 div 0) handle Div -> 42";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void ModByZeroRaisesDiv()
        {
            var src = "(10 mod 0) handle Div -> 42";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void SqrtDomainErrorRaisesDomain()
        {
            var src = "(Math.sqrt ~1.0) handle Domain -> 0.0";
            // Our language has no unary '-', so use 0.0 - 1.0
            src = "(Math.sqrt (0.0 - 1.0)) handle Domain -> 0.0";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(0.0, ((FloatVal)v).Value);
        }

        [Fact]
        public void LogDomainErrorRaisesDomain()
        {
            var src = "(Math.log 0.0) handle Domain -> 1.0";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(1.0, ((FloatVal)v).Value);
        }

        [Fact]
        public void LogNegativeRaisesDomain()
        {
            var src = "(Math.log (0.0 - 1.0)) handle Domain -> 2.0";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(2.0, ((FloatVal)v).Value);
        }

        [Fact]
        public void FloatDivisionByZeroDoesNotRaise()
        {
            var src = "1.0 / 0.0";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.True(double.IsInfinity(((FloatVal)v).Value));
        }

        [Fact]
        public void HandleOrderingMatchesSecondCase()
        {
            var src = "(raise Div) handle Fail s -> 0 | Div -> 1";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void UncaughtDomainPropagates()
        {
            var src = "Math.log 0.0";
            var fe = new Frontend();
            var (decls, mods, expr, type) = fe.Compile(src);
            var ev = new Evaluator();
            LoadBuiltins(ev);
            ev.LoadModules(mods);
            ev.LoadExceptions(mods);
            Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
        }
    }
}
