using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class StringModuleTests : AttoMLTestBase
    {
        [Fact]
        public void StringConcat_Works()
        {
            var (_, ev, expr, _) = CompileAndInitialize("String.concat \"a\" \"b\"");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("ab", sv.Value);
        }

        [Fact]
        public void StringLength_Works()
        {
            var (_, ev, expr, _) = CompileAndInitialize("String.length \"hello\"");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var iv = Assert.IsType<IntVal>(v);
            Assert.Equal(5, iv.Value);
        }

        [Fact]
        public void StringSize_Works()
        {
            var (_, ev, expr, _) = CompileAndInitialize("String.size \"hello\"");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var iv = Assert.IsType<IntVal>(v);
            Assert.Equal(5, iv.Value);
        }

        [Fact]
        public void StringSub_Works()
        {
            var (_, ev, expr, _) = CompileAndInitialize("String.sub \"abc\" 1");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("b", sv.Value);
        }

        [Fact]
        public void StringSubstring_Works()
        {
            var (_, ev, expr, _) = CompileAndInitialize("String.substring \"abcdef\" 2 3");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("cde", sv.Value);
        }

        [Fact]
        public void StringExplode_Implode_RoundTrip()
        {
            var (_, ev, expr1, _) = CompileAndInitialize("String.explode \"hi\"");
            var v1 = ev.Eval(expr1!, ev.GlobalEnv);
            var list = Assert.IsType<ListVal>(v1);
            Assert.Equal(2, list.Items.Count);
            Assert.Equal("h", ((StringVal)list.Items[0]).Value);
            Assert.Equal("i", ((StringVal)list.Items[1]).Value);

            var (_, ev2, expr2, _) = CompileAndInitialize("String.implode [\"h\", \"i\"]");
            var v2 = ev2.Eval(expr2!, ev2.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v2);
            Assert.Equal("hi", sv.Value);
        }

        [Fact]
        public void StringConcatList_Works()
        {
            var (_, ev, expr, _) = CompileAndInitialize("String.concatList [\"a\", \"b\", \"c\"]");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("abc", sv.Value);
        }

        [Fact]
        public void StringIsPrefix_IsSuffix_Contains_Work()
        {
            var (_, ev1, expr1, _) = CompileAndInitialize("String.isPrefix \"pre\" \"prefix\"");
            var v1 = ev1.Eval(expr1!, ev1.GlobalEnv);
            Assert.True(((BoolVal)v1).Value);

            var (_, ev2, expr2, _) = CompileAndInitialize("String.isSuffix \"fix\" \"prefix\"");
            var v2 = ev2.Eval(expr2!, ev2.GlobalEnv);
            Assert.True(((BoolVal)v2).Value);

            var (_, ev3, expr3, _) = CompileAndInitialize("String.contains \"ef\" \"prefix\"");
            var v3 = ev3.Eval(expr3!, ev3.GlobalEnv);
            Assert.True(((BoolVal)v3).Value);
        }

        [Fact]
        public void StringTranslate_Works()
        {
            var (_, ev, expr, _) = CompileAndInitialize("String.translate (fun c -> String.concat c c) \"ab\"");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var sv = Assert.IsType<StringVal>(v);
            Assert.Equal("aabb", sv.Value);
        }

        [Fact]
        public void StringCompare_Works()
        {
            {
                var (_, ev, expr, _) = CompileAndInitialize("String.compare \"a\" \"b\"");
                var v = ev.Eval(expr!, ev.GlobalEnv);
                var iv = Assert.IsType<IntVal>(v);
                Assert.True(iv.Value < 0);
            }
            {
                var (_, ev, expr, _) = CompileAndInitialize("String.compare \"a\" \"a\"");
                var v = ev.Eval(expr!, ev.GlobalEnv);
                var iv = Assert.IsType<IntVal>(v);
                Assert.True(iv.Value == 0);
            }
            {
                var (_, ev, expr, _) = CompileAndInitialize("String.compare \"b\" \"a\"");
                var v = ev.Eval(expr!, ev.GlobalEnv);
                var iv = Assert.IsType<IntVal>(v);
                Assert.True(iv.Value > 0);
            }
        }
    }
}
