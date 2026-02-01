using System;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class PatternMatchingTests : AttoMLTestBase
    {
        [Fact]
        public void MatchesTuplePatternBindsVar()
        {
            var src = "match (1, true) with (x, _) -> x";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void AdtMatchOptionReturnsPayload()
        {
            var src = "type Option = None | Some of int\nlet f = fun o -> match o with None -> 0 | Some x -> x in f (Some 3)";
            var (_, ev, expr, type) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
            Assert.NotNull(type);
        }

        [Fact]
        public void TypeInferenceUnifiesBranchTypes()
        {
            var src = "type T = A | B\nmatch A with A -> 1 | B -> 2";
            var (_, ev, expr, type) = CompileAndInitialize(src);
            Assert.NotNull(type);
            Assert.Contains("int", type!.ToString());
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void NonExhaustiveMatchThrows()
        {
            var src = "type Option = None | Some of int\nmatch Some 1 with None -> 0";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            Assert.Throws<Exception>(() => ev.Eval(expr!, ev.GlobalEnv));
        }
    }
}
