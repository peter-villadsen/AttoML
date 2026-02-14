using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Syntax
{
    public class PipeOperatorTests : AttoMLTestBase
    {
        [Fact]
        public void PipeOperator_BasicPipe_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                5 |> (fun x -> x + 1)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(6, result.Value);
        }

        [Fact]
        public void PipeOperator_ChainedPipes_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                10 |> (fun x -> x * 2) |> (fun x -> x + 5)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(25, result.Value);
        }

        [Fact]
        public void PipeOperator_ThreePipes_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                1 |> (fun x -> x + 1) |> (fun x -> x * 2) |> (fun x -> x * x)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(16, result.Value);  // ((1 + 1) * 2) ^ 2 = 16
        }

        [Fact]
        public void PipeOperator_WithNamedFunction_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let double = fun x -> x * 2 in
                5 |> double
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(10, result.Value);
        }

        [Fact]
        public void PipeOperator_WithListMap_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                [1, 2, 3] |> List.map (fun x -> x * 2)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lst = Assert.IsType<ListVal>(v);
            Assert.Equal(3, lst.Items.Count);
            Assert.Equal(2, ((IntVal)lst.Items[0]).Value);
            Assert.Equal(4, ((IntVal)lst.Items[1]).Value);
            Assert.Equal(6, ((IntVal)lst.Items[2]).Value);
        }

        [Fact]
        public void PipeOperator_ChainedListOperations_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                [1, 2, 3, 4, 5]
                |> List.map (fun x -> x * 2)
                |> List.filter (fun x -> x > 5)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var lst = Assert.IsType<ListVal>(v);
            Assert.Equal(3, lst.Items.Count);
            Assert.Equal(6, ((IntVal)lst.Items[0]).Value);
            Assert.Equal(8, ((IntVal)lst.Items[1]).Value);
            Assert.Equal(10, ((IntVal)lst.Items[2]).Value);
        }

        [Fact]
        public void PipeOperator_WithListFold_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                [1, 2, 3, 4, 5]
                |> List.map (fun x -> x * 2)
                |> List.filter (fun x -> x > 5)
                |> List.foldl (fun acc -> fun x -> acc + x) 0
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(24, result.Value);  // 6 + 8 + 10 = 24
        }

        [Fact]
        public void PipeOperator_WithArithmetic_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                10 + 5 |> (fun x -> x * 2)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(30, result.Value);  // (10 + 5) * 2 = 30
        }

        [Fact]
        public void PipeOperator_WithStringOperations_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                ""hello"" |> (fun s -> s ^ "" world"") |> (fun s -> s ^ ""!"")
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<StringVal>(v);
            Assert.Equal("hello world!", result.Value);
        }

        [Fact]
        public void PipeOperator_WithOption_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                Some 42 |> Option.map (fun x -> x + 1)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var adt = Assert.IsType<AdtVal>(v);
            Assert.Equal("Some", adt.Ctor);
            var payload = Assert.IsType<IntVal>(adt.Payload!);
            Assert.Equal(43, payload.Value);
        }

        [Fact]
        public void PipeOperator_ChainedOptionOperations_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                Some 10
                |> Option.map (fun x -> x * 2)
                |> Option.bind (fun x -> if x > 15 then Some (x + 100) else None)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var adt = Assert.IsType<AdtVal>(v);
            Assert.Equal("Some", adt.Ctor);
            var payload = Assert.IsType<IntVal>(adt.Payload!);
            Assert.Equal(120, payload.Value);  // (10 * 2) + 100 = 120
        }

        [Fact]
        public void PipeOperator_WithTuple_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                (3, 4) |> (fun p -> match p with (a, b) -> a + b end)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(7, result.Value);
        }

        [Fact]
        public void PipeOperator_InLetBinding_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let result = 5 |> (fun x -> x * 3) in
                result + 10
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(25, result.Value);  // (5 * 3) + 10 = 25
        }

        [Fact]
        public void PipeOperator_WithPartialApplication_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let add = fun x -> fun y -> x + y in
                5 |> add 10
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(15, result.Value);  // add 10 5 = 15
        }

        [Fact]
        public void PipeOperator_ComplexExpression_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                [1, 2, 3]
                |> List.map (fun x -> x * 2)
                |> List.map (fun x -> x + 1)
                |> List.filter (fun x -> x > 3)
                |> List.length
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(2, result.Value);  // [3, 5, 7] -> filter > 3 -> [5, 7] has length 2
        }

        [Fact]
        public void PipeOperator_WithMathFunctions_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                16.0 |> Math.sqrt |> (fun x -> x + 1.0)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<FloatVal>(v);
            Assert.Equal(5.0, result.Value);  // sqrt(16) + 1 = 5
        }

        [Fact]
        public void PipeOperator_WithNesting_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let processValue = fun v ->
                    v |> (fun x -> x * 2) |> (fun x -> x + 5)
                in
                processValue 10
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(25, result.Value);  // (10 * 2) + 5 = 25
        }

        [Fact]
        public void PipeOperator_WithConditional_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                10 |> (fun x -> if x > 5 then x * 2 else x)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(20, result.Value);
        }

        [Fact]
        public void PipeOperator_WithHigherOrderFunction_Works()
        {
            var (_, ev, _, _, expr, _) = CompileAndInitializeFull(@"
                let apply = fun f -> fun x -> f x in
                5 |> apply (fun x -> x * 3)
            ");
            var v = ev.Eval(expr!, ev.GlobalEnv);
            var result = Assert.IsType<IntVal>(v);
            Assert.Equal(15, result.Value);
        }
    }
}
