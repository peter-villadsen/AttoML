using Xunit;
using AttoML.Core;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class MatchWithEndTests : AttoMLTestBase
    {
        [Fact]
        public void SimpleMatchWithEnd()
        {
            var src = @"
                type Color = Red | Green | Blue
                match Red with
                  Red -> 1
                | Green -> 2
                | Blue -> 3
                end
            ";

            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void NestedMatchWithEnd()
        {
            var src = @"
                type Option = Some of int | None
                match Some 42 with
                  Some x ->
                    match Some 10 with
                      Some y -> x + y
                    | None -> x
                    end
                | None -> 0
                end
            ";

            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            Assert.IsType<IntVal>(v);
            Assert.Equal(52, ((IntVal)v).Value);
        }

        [Fact]
        public void MatchWithEndInsideLetBinding()
        {
            var src = @"
                type Option = Some of int | None
                let inner = match Some 5 with Some x -> x | None -> 0 end in
                inner + 10
            ";

            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            Assert.IsType<IntVal>(v);
            Assert.Equal(15, ((IntVal)v).Value);
        }

        [Fact]
        public void OldCaseSyntaxStillWorks()
        {
            var src = @"
                type Color = Red | Green | Blue
                case Red of Red -> 1 | Green -> 2 | Blue -> 3
            ";

            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void MatchWithEndInFunctionBody()
        {
            var src = @"
                type Option = Some of int | None
                let f = fun x ->
                    match x with
                      Some v -> v * 2
                    | None -> 0
                    end
                in f (Some 21)
            ";

            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }
    }
}
