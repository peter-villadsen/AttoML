using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class ListFunctionsTests
    {
        private (Frontend, Evaluator) Setup()
        {
            var fe = new Frontend();
            var ev = new Evaluator();
            var baseMod = AttoML.Interpreter.Builtins.BaseModule.Build();
            ev.Modules["Base"] = baseMod;
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set($"Base.{kv.Key}", kv.Value);
            }
            foreach (var kv in baseMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
            var listMod = AttoML.Interpreter.Builtins.ListModule.Build();
            ev.Modules["List"] = listMod;
            foreach (var kv in listMod.Members)
            {
                ev.GlobalEnv.Set($"List.{kv.Key}", kv.Value);
            }
            foreach (var kv in listMod.Members)
            {
                ev.GlobalEnv.Set(kv.Key, kv.Value);
            }
            return (fe, ev);
        }

        [Fact]
        public void AppendOperator()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("[1,2] @ [3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lv = Assert.IsType<ListVal>(v);
            Assert.Equal(3, lv.Items.Count);
        }

        [Fact]
        public void MapIncrements()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.map (fun x -> Base.add x 1) [1,2]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lv = Assert.IsType<ListVal>(v);
            Assert.Equal(2, lv.Items.Count);
            Assert.Equal(2, ((IntVal)lv.Items[0]).Value);
            Assert.Equal(3, ((IntVal)lv.Items[1]).Value);
        }

        [Fact]
        public void NullOnEmpty()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.null []");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.True(((BoolVal)v).Value);
        }

        [Fact]
        public void ExistsWorks()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.exists (fun x -> Base.eq x 2) [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.True(((BoolVal)v).Value);
        }

        [Fact]
        public void AllWorks()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.all (fun x -> Base.lt x 10) [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.True(((BoolVal)v).Value);
        }

        [Fact]
        public void FoldlSum()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.foldl (fun acc -> fun x -> Base.add acc x) 0 [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(6, ((IntVal)v).Value);
        }

        [Fact]
        public void FoldrSum()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.foldr (fun x -> fun acc -> Base.add acc x) 0 [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(6, ((IntVal)v).Value);
        }

        [Fact]
        public void LengthAndHeadTail()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("let xs = [1,2,3] in Base.add (List.length xs) (List.head xs)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.Equal(4, ((IntVal)v).Value);

            var (decls2, mods2, expr2, type2) = fe.Compile("List.tail [1,2,3]");
            var v2 = ev.Eval(expr2!, ev.GlobalEnv);
            var lv2 = Assert.IsType<ListVal>(v2);
            Assert.Equal(2, lv2.Items.Count);
            Assert.Equal(2, ((IntVal)lv2.Items[0]).Value);
        }

        [Fact]
        public void FilterGreaterThanOne()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.filter (fun x -> Base.lt 1 x) [1,2,3]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lv = Assert.IsType<ListVal>(v);
            Assert.Equal(2, lv.Items.Count);
            Assert.Equal(2, ((IntVal)lv.Items[0]).Value);
            Assert.Equal(3, ((IntVal)lv.Items[1]).Value);
        }

        [Fact]
        public void HdOnEmptyListRaisesFail()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("hd []");
            var ax = Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exn = Assert.IsType<AdtVal>(ax.Exn);
            Assert.Equal("Fail", exn.Ctor);
            var payload = Assert.IsType<StringVal>(exn.Payload);
            Assert.Equal("empty list", payload.Value);
        }

        [Fact]
        public void TlOnEmptyListRaisesFail()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("tl []");
            var ax = Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exn = Assert.IsType<AdtVal>(ax.Exn);
            Assert.Equal("Fail", exn.Ctor);
            var payload = Assert.IsType<StringVal>(exn.Payload);
            Assert.Equal("empty list", payload.Value);
        }

        [Fact]
        public void QualifiedListHdOnEmptyListRaisesFail()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.hd []");
            var ax = Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exn = Assert.IsType<AdtVal>(ax.Exn);
            Assert.Equal("Fail", exn.Ctor);
            var payload = Assert.IsType<StringVal>(exn.Payload);
            Assert.Equal("empty list", payload.Value);
        }

        [Fact]
        public void QualifiedListTlOnEmptyListRaisesFail()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("List.tl []");
            var ax = Assert.Throws<AttoML.Interpreter.Runtime.AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exn = Assert.IsType<AdtVal>(ax.Exn);
            Assert.Equal("Fail", exn.Ctor);
            var payload = Assert.IsType<StringVal>(exn.Payload);
            Assert.Equal("empty list", payload.Value);
        }

        [Fact]
        public void HdEmptyListHandledReturnsMessage()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("((hd []) handle Fail s -> s)");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("empty list", sv.Value);
        }

        [Fact]
        public void TlEmptyListHandledReturnsMessage()
        {
            var (fe, ev) = Setup();
            var (decls, mods, expr, type) = fe.Compile("((tl []) handle Fail s -> [])");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lv = Assert.IsType<ListVal>(v);
            Assert.Empty(lv.Items);
        }
    }
}
