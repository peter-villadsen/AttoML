using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;
using System.Linq;

namespace AttoML.Tests.Features
{
    public class ConsOperatorTests : AttoMLTestBase
    {
        [Fact]
        public void Cons_SingleElement_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let lst = [2, 3, 4] in
                1 :: lst
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v);
            Assert.Equal(4, list.Items.Count);
            Assert.Equal(1, ((IntVal)list.Items[0]).Value);
            Assert.Equal(2, ((IntVal)list.Items[1]).Value);
        }

        [Fact]
        public void Cons_Chained_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("1 :: 2 :: [3, 4]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v);
            Assert.Equal(4, list.Items.Count);
            Assert.Equal(1, ((IntVal)list.Items[0]).Value);
            Assert.Equal(2, ((IntVal)list.Items[1]).Value);
            Assert.Equal(3, ((IntVal)list.Items[2]).Value);
            Assert.Equal(4, ((IntVal)list.Items[3]).Value);
        }

        [Fact]
        public void Cons_EmptyList_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("42 :: []");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v);
            Assert.Single(list.Items);
            Assert.Equal(42, ((IntVal)list.Items[0]).Value);
        }

        [Fact]
        public void Cons_BuildList_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("1 :: 2 :: 3 :: []");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v);
            Assert.Equal(3, list.Items.Count);
            Assert.Equal(1, ((IntVal)list.Items[0]).Value);
            Assert.Equal(2, ((IntVal)list.Items[1]).Value);
            Assert.Equal(3, ((IntVal)list.Items[2]).Value);
        }

        [Fact]
        public void Cons_String_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                ""first"" :: [""second"", ""third""]
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v);
            Assert.Equal(3, list.Items.Count);
            Assert.Equal("first", ((StringVal)list.Items[0]).Value);
        }

        [Fact]
        public void Cons_WithAppend_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let lst1 = 1 :: [2] in
                let lst2 = [3, 4] in
                lst1 @ lst2
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v);
            Assert.Equal(4, list.Items.Count);
            Assert.Equal(1, ((IntVal)list.Items[0]).Value);
            Assert.Equal(4, ((IntVal)list.Items[3]).Value);
        }

        [Fact]
        public void Cons_InFunction_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let prepend = fun x -> fun lst -> x :: lst in
                prepend 10 [20, 30]
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v);
            Assert.Equal(3, list.Items.Count);
            Assert.Equal(10, ((IntVal)list.Items[0]).Value);
        }

        [Fact]
        public void ListCons_Function_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                List.cons 5 [6, 7, 8]
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v);
            Assert.Equal(4, list.Items.Count);
            Assert.Equal(5, ((IntVal)list.Items[0]).Value);
        }
    }
}
