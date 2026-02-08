using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class RecordFieldAccessTests : AttoMLTestBase
    {
        [Fact]
        public void RecordFieldAccess_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let person = {name = ""Alice"", age = 30} in
                person.name
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var str = Assert.IsType<StringVal>(v);
            Assert.Equal("Alice", str.Value);
        }

        [Fact]
        public void RecordFieldAccess_Multiple_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let person = {name = ""Bob"", age = 25, active = true} in
                let n = person.name in
                let a = person.age in
                (n, a)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var tuple = Assert.IsType<TupleVal>(v);
            Assert.Equal("Bob", ((StringVal)tuple.Items[0]).Value);
            Assert.Equal(25, ((IntVal)tuple.Items[1]).Value);
        }

        [Fact]
        public void RecordFieldAccess_IntField_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let person = {name = ""Charlie"", age = 35} in
                person.age
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var age = Assert.IsType<IntVal>(v);
            Assert.Equal(35, age.Value);
        }

        [Fact]
        public void RecordFieldAccess_Chained_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let rec1 = {x = 10, y = 20} in
                let rec2 = {x = rec1.x, y = rec1.y} in
                rec2.x + rec2.y
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(30, result.Value);
        }

        [Fact]
        public void RecordFieldAccess_BoolField_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let state = {enabled = true, count = 5} in
                state.enabled
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var enabled = Assert.IsType<BoolVal>(v);
            Assert.True(enabled.Value);
        }

        [Fact]
        public void RecordFieldAccess_NestedRecords_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let inner = {value = 42} in
                let outer = {inner = inner, name = ""test""} in
                outer.inner
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var inner = Assert.IsType<RecordVal>(v);
            Assert.True(inner.Fields.ContainsKey("value"));
        }
    }
}
