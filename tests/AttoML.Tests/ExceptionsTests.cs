using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class ExceptionsTests : AttoMLTestBase
    {
        [Fact]
        public void BuiltinFailCanBeRaisedAndHandled()
        {
            var src = "((raise (Fail \"nope\")) handle Fail s -> s)";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            ev.GlobalEnv.Set("Fail", new ClosureVal(arg => new AdtVal("Fail", arg)));
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<StringVal>(v);
            Assert.Equal("nope", ((StringVal)v).Value);
        }

        [Fact]
        public void DeclaredExceptionHandled()
        {
            var src = "exception Oops of int\n((raise (Oops 3)) handle Oops n -> n)";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            ev.GlobalEnv.Set("Fail", new ClosureVal(arg => new AdtVal("Fail", arg)));
            ev.LoadExceptions(mods);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
        }

        [Fact]
        public void UncaughtExceptionPropagates()
        {
            var src = "exception E\nlet x = raise E in x";
            var (_, ev, decls, mods, expr, _) = CompileAndInitializeFull(src);
            ev.GlobalEnv.Set("Fail", new ClosureVal(arg => new AdtVal("Fail", arg)));
            ev.LoadExceptions(mods);
            Assert.Throws<AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
        }

        [Fact]
        public void RaiseRequiresExnType()
        {
            var fe = new Frontend();
            Assert.Throws<Exception>(() => fe.Compile("raise 1"));
        }
    }
}
