using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Types
{
    public class OptionTypesTests : AttoMLTestBase
    {
        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionInt_SomeInt_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("SomeInt 42");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var adt = Assert.IsType<AdtVal>(v);
            Assert.Equal("SomeInt", adt.Ctor);
            Assert.NotNull(adt.Payload);
            Assert.Equal(42, ((IntVal)adt.Payload!).Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionInt_NoneInt_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("NoneInt");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var adt = Assert.IsType<AdtVal>(v);
            Assert.Equal("NoneInt", adt.Ctor);
            Assert.Null(adt.Payload);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionInt_GetOr_Some_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                OptionInt.getOr (SomeInt 99) 0
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(99, result.Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionInt_GetOr_None_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                OptionInt.getOr NoneInt 42
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(42, result.Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionInt_Map_Some_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let opt = SomeInt 10 in
                let doubled = OptionInt.map (fun x -> x * 2) opt in
                OptionInt.getOr doubled 0
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(20, result.Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionInt_Map_None_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let opt = NoneInt in
                let doubled = OptionInt.map (fun x -> x * 2) opt in
                OptionInt.getOr doubled 99
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(99, result.Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionFloat_SomeFloat_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("SomeFloat 3.14");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var adt = Assert.IsType<AdtVal>(v);
            Assert.Equal("SomeFloat", adt.Ctor);
            Assert.NotNull(adt.Payload);
            Assert.Equal(3.14, ((FloatVal)adt.Payload!).Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionString_SomeString_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"SomeString ""hello""");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var adt = Assert.IsType<AdtVal>(v);
            Assert.Equal("SomeString", adt.Ctor);
            Assert.NotNull(adt.Payload);
            Assert.Equal("hello", ((StringVal)adt.Payload!).Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionBool_SomeBool_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull("SomeBool true");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var adt = Assert.IsType<AdtVal>(v);
            Assert.Equal("SomeBool", adt.Ctor);
            Assert.NotNull(adt.Payload);
            Assert.True(((BoolVal)adt.Payload!).Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionInt_IsSome_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                OptionInt.isSome (SomeInt 5)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<BoolVal>(v);
            Assert.True(result.Value);
        }

        [Fact(Skip = "Tests monomorphic OptionInt/OptionFloat - now superseded by polymorphic option type")]
        public void OptionInt_IsNone_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                OptionInt.isNone NoneInt
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<BoolVal>(v);
            Assert.True(result.Value);
        }
    }
}
