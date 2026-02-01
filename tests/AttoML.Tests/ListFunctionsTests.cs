using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class ListFunctionsTests : AttoMLTestBase
    {
        [Fact]
        public void AppendOperator()
        {
            var (_, ev, expr, _) = CompileAndInitialize("[1,2] @ [3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lv = Assert.IsType<ListVal>(v);
            Assert.Equal(3, lv.Items.Count);
        }

        [Fact]
        public void MapIncrements()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.map (fun x -> Base.add x 1) [1,2]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lv = Assert.IsType<ListVal>(v);
            Assert.Equal(2, lv.Items.Count);
            Assert.Equal(2, ((IntVal)lv.Items[0]).Value);
            Assert.Equal(3, ((IntVal)lv.Items[1]).Value);
        }

        [Fact]
        public void NullOnEmpty()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.null []");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.True(((BoolVal)v).Value);
        }

        [Fact]
        public void ExistsWorks()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.exists (fun x -> Base.eq x 2) [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.True(((BoolVal)v).Value);
        }

        [Fact]
        public void AllWorks()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.all (fun x -> Base.lt x 10) [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.True(((BoolVal)v).Value);
        }

        [Fact]
        public void FoldlSum()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.foldl (fun acc -> fun x -> Base.add acc x) 0 [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(6, ((IntVal)v).Value);
        }

        [Fact]
        public void FoldrSum()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.foldr (fun x -> fun acc -> Base.add acc x) 0 [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(6, ((IntVal)v).Value);
        }

        [Fact]
        public void LengthAndHeadTail()
        {
            var (fe, ev, expr, _) = CompileAndInitialize("let xs = [1,2,3] in Base.add (List.length xs) (List.head xs)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(4, ((IntVal)v).Value);

            var (_, _, expr2, _) = CompileAndInitialize("List.tail [1,2,3]");
            var v2 = ev.Eval(expr2!, ev.GlobalEnv);
            var lv2 = Assert.IsType<ListVal>(v2);
            Assert.Equal(2, lv2.Items.Count);
            Assert.Equal(2, ((IntVal)lv2.Items[0]).Value);
        }

        [Fact]
        public void FilterGreaterThanOne()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.filter (fun x -> Base.lt 1 x) [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lv = Assert.IsType<ListVal>(v);
            Assert.Equal(2, lv.Items.Count);
            Assert.Equal(2, ((IntVal)lv.Items[0]).Value);
            Assert.Equal(3, ((IntVal)lv.Items[1]).Value);
        }

        [Fact]
        public void HdOnEmptyListRaisesFail()
        {
            var (_, ev, expr, _) = CompileAndInitialize("hd []");
            var ax = Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exn = Assert.IsType<AdtVal>(ax.Exn);
            Assert.Equal("Fail", exn.Ctor);
            var payload = Assert.IsType<StringVal>(exn.Payload);
            Assert.Equal("empty list", payload.Value);
        }

        [Fact]
        public void TlOnEmptyListRaisesFail()
        {
            var (_, ev, expr, _) = CompileAndInitialize("tl []");
            var ax = Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exn = Assert.IsType<AdtVal>(ax.Exn);
            Assert.Equal("Fail", exn.Ctor);
            var payload = Assert.IsType<StringVal>(exn.Payload);
            Assert.Equal("empty list", payload.Value);
        }

        [Fact]
        public void QualifiedListHdOnEmptyListRaisesFail()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.hd []");
            var ax = Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exn = Assert.IsType<AdtVal>(ax.Exn);
            Assert.Equal("Fail", exn.Ctor);
            var payload = Assert.IsType<StringVal>(exn.Payload);
            Assert.Equal("empty list", payload.Value);
        }

        [Fact]
        public void QualifiedListTlOnEmptyListRaisesFail()
        {
            var (_, ev, expr, _) = CompileAndInitialize("List.tl []");
            var ax = Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exn = Assert.IsType<AdtVal>(ax.Exn);
            Assert.Equal("Fail", exn.Ctor);
            var payload = Assert.IsType<StringVal>(exn.Payload);
            Assert.Equal("empty list", payload.Value);
        }

        [Fact]
        public void HdEmptyListHandledReturnsMessage()
        {
            var (_, ev, expr, _) = CompileAndInitialize("((hd []) handle Fail s -> s)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("empty list", sv.Value);
        }

        [Fact]
        public void TlEmptyListHandledReturnsMessage()
        {
            var (_, ev, expr, _) = CompileAndInitialize("((tl []) handle Fail s -> [])");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lv = Assert.IsType<ListVal>(v);
            Assert.Empty(lv.Items);
        }
    }
}
