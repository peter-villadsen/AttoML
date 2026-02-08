using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class TupleTests : AttoMLTestBase
    {
        [Fact]
        public void Tuple2_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("(1, 2)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var tuple = Assert.IsType<TupleVal>(v);
            Assert.Equal(2, tuple.Items.Count);
            Assert.Equal(1, ((IntVal)tuple.Items[0]).Value);
            Assert.Equal(2, ((IntVal)tuple.Items[1]).Value);
        }

        [Fact]
        public void Tuple3_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("(1, 2, 3)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var tuple = Assert.IsType<TupleVal>(v);
            Assert.Equal(3, tuple.Items.Count);
        }

        [Fact]
        public void Tuple5_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("(1, 2, 3, 4, 5)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var tuple = Assert.IsType<TupleVal>(v);
            Assert.Equal(5, tuple.Items.Count);
            Assert.Equal(1, ((IntVal)tuple.Items[0]).Value);
            Assert.Equal(5, ((IntVal)tuple.Items[4]).Value);
        }

        [Fact]
        public void Tuple7_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("(1, 2, 3, 4, 5, 6, 7)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var tuple = Assert.IsType<TupleVal>(v);
            Assert.Equal(7, tuple.Items.Count);
        }

        [Fact]
        public void Tuple_PatternMatching_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let tup = (1, 2, 3, 4, 5) in
                let (a, b, c, d, e) = tup in
                a + b + c + d + e
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(15, result.Value);
        }

        [Fact]
        public void NestedTuples_Work()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("((1, 2), (3, 4))");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var tuple = Assert.IsType<TupleVal>(v);
            Assert.Equal(2, tuple.Items.Count);
            var first = Assert.IsType<TupleVal>(tuple.Items[0]);
            Assert.Equal(2, first.Items.Count);
        }
    }
}
