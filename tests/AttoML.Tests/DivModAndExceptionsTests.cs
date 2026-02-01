using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class DivModAndExceptionsTests : AttoMLTestBase
    {
        private void LoadExceptionBuiltins(Evaluator ev)
        {
            ev.GlobalEnv.Set("Div", new AdtVal("Div", null));
            ev.GlobalEnv.Set("Domain", new AdtVal("Domain", null));
            ev.GlobalEnv.Set("Fail", new ClosureVal(arg => new AdtVal("Fail", arg)));
        }

        [Fact]
        public void InfixDivAndModWork()
        {
            var src = "10 div 3";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);

            var src2 = "10 mod 3";
            var (_, ev2, expr2, _) = CompileAndInitialize(src2);
            var v2 = ev2.Eval(expr2!, ev2.GlobalEnv);
            Assert.IsType<IntVal>(v2);
            Assert.Equal(1, ((IntVal)v2).Value);
        }

        [Fact]
        public void DivByZeroWithSlashRaisesDiv()
        {
            var src = "(1 / 0) handle Div -> 42";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void DivByZeroWithDivRaisesDiv()
        {
            var src = "(10 div 0) handle Div -> 42";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void ModByZeroRaisesDiv()
        {
            var src = "(10 mod 0) handle Div -> 42";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void SqrtDomainErrorRaisesDomain()
        {
            var src = "(Math.sqrt (0.0 - 1.0)) handle Domain -> 0.0";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(0.0, ((FloatVal)v).Value);
        }

        [Fact]
        public void LogDomainErrorRaisesDomain()
        {
            var src = "(Math.log 0.0) handle Domain -> 1.0";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(1.0, ((FloatVal)v).Value);
        }

        [Fact]
        public void LogNegativeRaisesDomain()
        {
            var src = "(Math.log (0.0 - 1.0)) handle Domain -> 2.0";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(2.0, ((FloatVal)v).Value);
        }

        [Fact]
        public void FloatDivisionByZeroDoesNotRaise()
        {
            var src = "1.0 / 0.0";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.True(double.IsInfinity(((FloatVal)v).Value));
        }

        [Fact]
        public void HandleOrderingMatchesSecondCase()
        {
            var src = "(raise Div) handle Fail s -> 0 | Div -> 1";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void UncaughtDomainPropagates()
        {
            var src = "Math.log 0.0";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            LoadExceptionBuiltins(ev);
            ev.LoadExceptions(mods);
            Assert.Throws<AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
        }
    }
}
