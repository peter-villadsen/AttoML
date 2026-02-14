using Xunit;
using AttoML.Core;
using AttoML.Core.Parsing;
using AttoML.Core.Types;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Patterns
{
    public class Phase2PatternTests : AttoMLTestBase
    {
        // List Cons Pattern Tests
        [Fact]
        public void ListConsPattern_BasicHeadTail_Works()
        {
            var src = @"
                match [1, 2, 3] with
                    h::t -> h
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void ListConsPattern_GetTail_Works()
        {
            var src = @"
                match [1, 2, 3] with
                    h::t -> t
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<ListVal>(v);
            var lv = (ListVal)v;
            Assert.Equal(2, lv.Items.Count);
            Assert.Equal(2, ((IntVal)lv.Items[0]).Value);
            Assert.Equal(3, ((IntVal)lv.Items[1]).Value);
        }

        [Fact]
        public void ListConsPattern_NestedCons_Works()
        {
            var src = @"
                match [1, 2, 3, 4] with
                    a::b::rest -> a + b
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
        }

        [Fact]
        public void ListConsPattern_EmptyListFails_MatchesSecondBranch()
        {
            var src = @"
                match [] with
                    h::t -> 1
                  | [] -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(0, ((IntVal)v).Value);
        }

        [Fact]
        public void ListConsPattern_SingletonList_TailEmpty()
        {
            var src = @"
                let checkTail = fn t -> match t with [] -> true | _ -> false end in
                match [42] with
                    h::t -> if checkTail t then h else 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void ListConsPattern_TypeInference_Correct()
        {
            var src = @"
                fn lst -> match lst with
                    h::t -> h
                  | [] -> 0
                end";
            var (_, _, _, type) = CompileAndInitialize(src);
            Assert.IsType<TFun>(type);
            var funTy = (TFun)type!;
            Assert.IsType<TList>(funTy.From);
            Assert.IsType<TConst>(funTy.To);
        }

        // List Literal Pattern Tests
        [Fact]
        public void ListLiteralPattern_EmptyList_Works()
        {
            var src = @"
                match [] with
                    [] -> true
                  | _ -> false
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<BoolVal>(v);
            Assert.True(((BoolVal)v).Value);
        }

        [Fact]
        public void ListLiteralPattern_SingleElement_Works()
        {
            var src = @"
                match [42] with
                    [x] -> x
                  | _ -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void ListLiteralPattern_TwoElements_Works()
        {
            var src = @"
                match [1, 2] with
                    [a, b] -> a + b
                  | _ -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
        }

        [Fact]
        public void ListLiteralPattern_ThreeElements_Works()
        {
            var src = @"
                match [10, 20, 30] with
                    [x, y, z] -> x + y + z
                  | _ -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(60, ((IntVal)v).Value);
        }

        [Fact]
        public void ListLiteralPattern_WrongLength_MatchesFallthrough()
        {
            var src = @"
                match [1, 2, 3] with
                    [a, b] -> 1
                  | _ -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(0, ((IntVal)v).Value);
        }

        [Fact]
        public void ListLiteralPattern_NestedPatterns_Works()
        {
            var src = @"
                match [[1, 2], [3, 4]] with
                    [[a, b], [c, d]] -> a + b + c + d
                  | _ -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(10, ((IntVal)v).Value);
        }

        // Record Pattern Tests
        [Fact]
        public void RecordPattern_AllFields_Works()
        {
            var src = @"
                match {x = 10, y = 20} with
                    {x = a, y = b} -> a + b
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(30, ((IntVal)v).Value);
        }

        // Note: Partial record matching not currently supported - all fields must match
        // [Fact]
        // public void RecordPattern_PartialMatch_Works() { ... }

        [Fact]
        public void RecordPattern_SwappedFieldOrder_Works()
        {
            var src = @"
                match {x = 10, y = 20} with
                    {y = b, x = a} -> a - b
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(-10, ((IntVal)v).Value);
        }

        [Fact]
        public void RecordPattern_NestedRecords_Works()
        {
            var src = @"
                match {outer = {inner = 42}} with
                    {outer = {inner = x}} -> x
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        [Fact]
        public void RecordPattern_WithTuples_Works()
        {
            var src = @"
                match {point = (10, 20)} with
                    {point = (x, y)} -> x + y
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(30, ((IntVal)v).Value);
        }

        [Fact]
        public void RecordPattern_EmptyRecord_Works()
        {
            var src = @"
                match {} with
                    {} -> 42
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }

        // Combined Pattern Tests
        [Fact]
        public void CombinedPatterns_ListConsInRecord_Works()
        {
            var src = @"
                match {data = [1, 2, 3]} with
                    {data = h::t} -> h
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void CombinedPatterns_RecordInList_Works()
        {
            var src = @"
                match [{x = 1}, {x = 2}] with
                    [{x = a}, {x = b}] -> a + b
                  | _ -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(3, ((IntVal)v).Value);
        }

        [Fact]
        public void CombinedPatterns_ConsWithRecords_Works()
        {
            var src = @"
                match [{x = 1}, {x = 2}, {x = 3}] with
                    {x = first}::rest -> first
                  | _ -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void CombinedPatterns_ComplexNesting_Works()
        {
            var src = @"
                match {coords = [(1, 2), (3, 4)]} with
                    {coords = [(x1, y1), (x2, y2)]} -> x1 + y1 + x2 + y2
                  | _ -> 0
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(10, ((IntVal)v).Value);
        }

        // Function with Patterns Tests
        // Note: fn only supports identifier and tuple patterns in parameters currently
        // List and record patterns in fn parameters not yet supported
        // Can use match in function body instead:
        [Fact]
        public void ListConsPattern_InFunctionBody_Works()
        {
            var src = @"
                (fn lst -> match lst with h::t -> h end) [1, 2, 3]
                ";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void ListLiteralPattern_InFunctionBody_Works()
        {
            var src = @"
                (fn lst -> match lst with [a, b] -> a + b end) [10, 20]
                ";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(30, ((IntVal)v).Value);
        }

        [Fact]
        public void RecordPattern_InFunctionBody_Works()
        {
            var src = @"
                (fn r -> match r with {x = a, y = b} -> a * b end) {x = 5, y = 6}
                ";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(30, ((IntVal)v).Value);
        }

        // Let binding with patterns (tuple patterns already supported)
        // Note: Let currently only supports identifier and tuple patterns
        // Can use match as workaround:
        [Fact]
        public void ListConsPattern_WithLetAndMatch_Works()
        {
            var src = @"
                let lst = [1, 2, 3] in match lst with h::t -> h
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(1, ((IntVal)v).Value);
        }

        [Fact]
        public void RecordPattern_WithLetAndMatch_Works()
        {
            var src = @"
                let r = {x = 10, y = 20} in match r with {x = a, y = b} -> a + b
                end";
            var (_, ev, expr, _) = CompileAndInitialize(src);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(30, ((IntVal)v).Value);
        }
    }
}
