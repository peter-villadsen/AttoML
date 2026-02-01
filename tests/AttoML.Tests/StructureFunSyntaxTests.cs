using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
    public class StructureFunSyntaxTests : AttoMLTestBase
    {
        [Fact]
        public void StructureWithFunSyntax_SimpleFunction()
        {
            var src = @"
structure M = {
  fun square x = x * x
}
open M
square 5
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(25, ((IntVal)v).Value);
        }

        [Fact]
        public void StructureWithFunSyntax_MultipleParams()
        {
            var src = @"
structure M = {
  fun add x y = x + y
}
open M
add 3 4
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(7, ((IntVal)v).Value);
        }

        [Fact]
        public void StructureWithFunSyntax_TuplePattern()
        {
            var src = @"
structure M = {
  fun fst (a, b) = a
}
open M
fst (10, 20)
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(10, ((IntVal)v).Value);
        }

        [Fact]
        public void StructureWithValAndFun_Mixed()
        {
            var src = @"
structure M = {
  val pi = 3.14,
  fun double x = x * 2.0
}
open M
double pi
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<FloatVal>(v);
            Assert.Equal(6.28, ((FloatVal)v).Value, 2);
        }

        [Fact]
        public void StructureWithFunSyntax_RecursiveFunction()
        {
            var src = @"
structure M = {
  fun fact n = if n = 0 then 1 else n * fact (n - 1)
}
open M
fact 5
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(120, ((IntVal)v).Value);
        }

        [Fact]
        public void StructureWithFunSyntax_CallingOtherMember()
        {
            var src = @"
structure M = {
  fun square x = x * x,
  fun sumSquares x y = square x + square y
}
open M
sumSquares 3 4
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(25, ((IntVal)v).Value);
        }

        [Fact]
        public void StructureWithFunSyntax_WithLetExpression()
        {
            var src = @"
structure M = {
  fun compute x =
    let y = x * 2 in
    y + 1
}
open M
compute 5
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(11, ((IntVal)v).Value);
        }

        [Fact]
        public void StructureWithFunSyntax_PatternMatching()
        {
            var src = @"
structure M = {
  fun isZero x =
    match x with
        0 -> true
      | _ -> false
}
open M
isZero 0
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<BoolVal>(v);
            Assert.Equal(true, ((BoolVal)v).Value);
        }

        [Fact]
        public void StructureWithLet_BackwardCompatibility()
        {
            // Ensure existing 'let' syntax still works
            var src = @"
structure M = {
  let x = 10
}
open M
x
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(10, ((IntVal)v).Value);
        }

        [Fact]
        public void StructureWithVal_BasicValue()
        {
            var src = @"
structure M = {
  val answer = 42
}
open M
answer
";
            var (_, ev, decls, _, expr, _) = CompileAndInitializeFull(src);
            ev.ApplyOpen(decls);
            var v = ev.Eval(expr!, ev.GlobalEnv);
            Assert.IsType<IntVal>(v);
            Assert.Equal(42, ((IntVal)v).Value);
        }
    }
}
