using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Types
{
    public class OptionTypeTests : AttoMLTestBase
    {
        [Fact]
        public void Datatype_DefinesOption_ConstructorsUsable()
        {
            var (_, ev, expr, _) = CompileAndInitialize("datatype option = NONE | SOME of int\nSOME 3");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            Assert.Equal("SOME", av.Ctor);
            var payload = Assert.IsType<IntVal>(av.Payload);
            Assert.Equal(3, payload.Value);
        }

        [Fact]
        public void MatchOptionExtractsPayload()
        {
            var src = "datatype option = NONE | SOME of int\nlet f = fun o -> match o with NONE -> 0 | SOME x -> x end in f (SOME 42)";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(42, Assert.IsType<IntVal>(v).Value);
        }

        [Fact]
        public void NonExhaustiveMatchThrowsOnSOME()
        {
            var src = "datatype option = NONE | SOME of int\nmatch SOME 1 with NONE -> 0 end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            Assert.Throws<Exception>(() => ev.Eval(expr!, ev.GlobalEnv));
        }

        [Fact]
        public void QualifiedConstructorPatternsWork()
        {
            var src = "datatype option = NONE | SOME of int\nmatch option.SOME 2 with option.NONE -> 0 | option.SOME x -> x end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(2, Assert.IsType<IntVal>(v).Value);
        }

        [Fact]
        public void OptionMapExample()
        {
            var src = @"datatype option = NONE | SOME of int
structure Option = {
    let map = fun f -> fun o ->
        match o with NONE -> NONE | SOME x -> SOME (f x) end
}
let inc = fun x -> x + 1 in Option.map inc (SOME 4)";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var av = Assert.IsType<AdtVal>(v);
            Assert.Equal("SOME", av.Ctor);
            Assert.Equal(5, Assert.IsType<IntVal>(av.Payload).Value);
        }
    }
}
