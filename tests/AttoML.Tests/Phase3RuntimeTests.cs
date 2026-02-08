using Xunit;
using AttoML.Core;
using AttoML.Core.Parsing;
using AttoML.Core.Types;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests
{
	public class Phase3RuntimeTests : AttoMLTestBase
	{
		// Tuple Module Tests

		[Fact]
		public void TupleModule_Fst_ReturnFirstElement()
		{
			var src = @"Tuple.fst (1, 2)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(1, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Fst_WithDifferentTypes()
		{
			var src = @"Tuple.fst (""hello"", 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<StringVal>(v);
			Assert.Equal("hello", ((StringVal)v).Value);
		}

		[Fact]
		public void TupleModule_Snd_ReturnSecondElement()
		{
			var src = @"Tuple.snd (1, 2)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(2, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Snd_WithDifferentTypes()
		{
			var src = @"Tuple.snd (42, ""world"")";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<StringVal>(v);
			Assert.Equal("world", ((StringVal)v).Value);
		}

		[Fact]
		public void TupleModule_Swap_SwapsTupleElements()
		{
			var src = @"Tuple.swap (1, 2)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<TupleVal>(v);
			var tv = (TupleVal)v;
			Assert.Equal(2, tv.Items.Count);
			Assert.Equal(2, ((IntVal)tv.Items[0]).Value);
			Assert.Equal(1, ((IntVal)tv.Items[1]).Value);
		}

		[Fact]
		public void TupleModule_Swap_WithDifferentTypes()
		{
			var src = @"Tuple.swap (""first"", 123)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<TupleVal>(v);
			var tv = (TupleVal)v;
			Assert.Equal(2, tv.Items.Count);
			Assert.Equal(123, ((IntVal)tv.Items[0]).Value);
			Assert.Equal("first", ((StringVal)tv.Items[1]).Value);
		}

		[Fact]
		public void TupleModule_Curry_ConvertsTupledFunction()
		{
			var src = @"
				let addPair = fn (x, y) => x + y in
				let curriedAdd = Tuple.curry addPair in
				curriedAdd 3 5
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(8, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Curry_PartialApplication()
		{
			var src = @"
				let addPair = fn (x, y) => x + y in
				let curriedAdd = Tuple.curry addPair in
				let add5 = curriedAdd 5 in
				add5 10
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(15, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Uncurry_ConvertsCurriedFunction()
		{
			var src = @"
				let add = fn x => fn y => x + y in
				let uncurriedAdd = Tuple.uncurry add in
				uncurriedAdd (3, 5)
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(8, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Uncurry_WithMultiplication()
		{
			var src = @"
				let mul = fn x => fn y => x * y in
				let uncurriedMul = Tuple.uncurry mul in
				uncurriedMul (4, 7)
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(28, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Fst3_ReturnFirstOfThree()
		{
			var src = @"Tuple.fst3 (1, 2, 3)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(1, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Snd3_ReturnSecondOfThree()
		{
			var src = @"Tuple.snd3 (1, 2, 3)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(2, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Thd3_ReturnThirdOfThree()
		{
			var src = @"Tuple.thd3 (1, 2, 3)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(3, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Fst3_WithMixedTypes()
		{
			var src = @"Tuple.fst3 (""first"", 2, true)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<StringVal>(v);
			Assert.Equal("first", ((StringVal)v).Value);
		}

		[Fact]
		public void TupleModule_Snd3_WithMixedTypes()
		{
			var src = @"Tuple.snd3 (""first"", 2, true)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(2, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_Thd3_WithMixedTypes()
		{
			var src = @"Tuple.thd3 (""first"", 2, true)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.True(((BoolVal)v).Value);
		}

		[Fact]
		public void TupleModule_CombinedUsage_FstSndSwap()
		{
			var src = @"
				let pair = (10, 20) in
				let swapped = Tuple.swap pair in
				(Tuple.fst swapped) + (Tuple.snd swapped)
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(30, ((IntVal)v).Value);
		}

		[Fact]
		public void TupleModule_CurriedAddition_HigherOrder()
		{
			var src = @"
				let addPair = fn (x, y) => x + y in
				let curriedAdd = Tuple.curry addPair in
				map (curriedAdd 10) [1, 2, 3]
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<ListVal>(v);
			var lv = (ListVal)v;
			Assert.Equal(3, lv.Items.Count);
			Assert.Equal(11, ((IntVal)lv.Items[0]).Value);
			Assert.Equal(12, ((IntVal)lv.Items[1]).Value);
			Assert.Equal(13, ((IntVal)lv.Items[2]).Value);
		}

		[Fact]
		public void TupleModule_Triple_ExtractAll()
		{
			var src = @"
				let triple = (100, 200, 300) in
				(Tuple.fst3 triple) + (Tuple.snd3 triple) + (Tuple.thd3 triple)
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(600, ((IntVal)v).Value);
		}

		// Option Module Tests (from prelude)
		// Note: These tests use qualified names (Option.function) because the prelude
		// is not loaded in the test environment. In real usage, users can "open Option"
		// to use unqualified names.

		[Fact]
		public void OptionModule_IsSome_WithSome()
		{
			var src = @"Option.isSome (Some 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.True(((BoolVal)v).Value);
		}

		[Fact]
		public void OptionModule_IsSome_WithNone()
		{
			var src = @"Option.isSome None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.False(((BoolVal)v).Value);
		}

		[Fact]
		public void OptionModule_IsNone_WithSome()
		{
			var src = @"Option.isNone (Some 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.False(((BoolVal)v).Value);
		}

		[Fact]
		public void OptionModule_IsNone_WithNone()
		{
			var src = @"Option.isNone None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.True(((BoolVal)v).Value);
		}

		[Fact]
		public void OptionModule_GetOr_WithSome()
		{
			var src = @"Option.getOr (Some 42) 0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(42, ((IntVal)v).Value);
		}

		[Fact]
		public void OptionModule_GetOr_WithNone()
		{
			var src = @"Option.getOr None 99";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(99, ((IntVal)v).Value);
		}

		[Fact]
		public void OptionModule_Map_WithSome()
		{
			var src = @"Option.map (fn x => x * 2) (Some 21)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("Some", av.Ctor);
			Assert.IsType<IntVal>(av.Payload);
			Assert.Equal(42, ((IntVal)av.Payload!).Value);
		}

		[Fact]
		public void OptionModule_Map_WithNone()
		{
			var src = @"Option.map (fn x => x * 2) None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
		}

		[Fact]
		public void OptionModule_Bind_WithSome()
		{
			var src = @"Option.bind (fn x => if x > 0 then Some (x * 2) else None) (Some 21)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("Some", av.Ctor);
			Assert.Equal(42, ((IntVal)av.Payload!).Value);
		}

		[Fact]
		public void OptionModule_Bind_WithNone()
		{
			var src = @"Option.bind (fn x => Some (x * 2)) None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
		}

		[Fact]
		public void OptionModule_Filter_PassesPredicate()
		{
			var src = @"Option.filter (fn x => x > 10) (Some 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("Some", av.Ctor);
			Assert.Equal(42, ((IntVal)av.Payload!).Value);
		}

		[Fact]
		public void OptionModule_Filter_FailsPredicate()
		{
			var src = @"Option.filter (fn x => x > 50) (Some 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
		}

		[Fact]
		public void OptionModule_Filter_WithNone()
		{
			var src = @"Option.filter (fn x => x > 10) None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
		}

		[Fact]
		public void OptionModule_Fold_WithSome()
		{
			var src = @"Option.fold (fn x => x * 3) (fn _ => 0) (Some 14)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(42, ((IntVal)v).Value);
		}

		[Fact]
		public void OptionModule_Fold_WithNone()
		{
			var src = @"Option.fold (fn x => x * 3) (fn _ => 99) None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(99, ((IntVal)v).Value);
		}

		[Fact]
		public void OptionModule_ToList_WithSome()
		{
			var src = @"Option.toList (Some 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<ListVal>(v);
			var lv = (ListVal)v;
			Assert.Single(lv.Items);
			Assert.Equal(42, ((IntVal)lv.Items[0]).Value);
		}

		[Fact]
		public void OptionModule_ToList_WithNone()
		{
			var src = @"Option.toList None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<ListVal>(v);
			var lv = (ListVal)v;
			Assert.Empty(lv.Items);
		}

		[Fact]
		public void OptionModule_FromList_WithNonEmptyList()
		{
			var src = @"Option.fromList [1, 2, 3]";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("Some", av.Ctor);
			Assert.Equal(1, ((IntVal)av.Payload!).Value);
		}

		[Fact]
		public void OptionModule_FromList_WithEmptyList()
		{
			var src = @"Option.fromList []";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
		}

		[Fact]
		public void OptionModule_Map2_WithTwoSomes()
		{
			var src = @"Option.map2 (fn x => fn y => x + y) (Some 10) (Some 32)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("Some", av.Ctor);
			Assert.Equal(42, ((IntVal)av.Payload!).Value);
		}

		[Fact]
		public void OptionModule_Map2_WithFirstNone()
		{
			var src = @"Option.map2 (fn x => fn y => x + y) None (Some 32)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
		}

		[Fact]
		public void OptionModule_Map2_WithSecondNone()
		{
			var src = @"Option.map2 (fn x => fn y => x + y) (Some 10) None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
		}

		[Fact]
		public void OptionModule_OrElse_FirstSome()
		{
			var src = @"Option.orElse (Some 42) (Some 99)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("Some", av.Ctor);
			Assert.Equal(42, ((IntVal)av.Payload!).Value);
		}

		[Fact]
		public void OptionModule_OrElse_FirstNone()
		{
			var src = @"Option.orElse None (Some 99)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("Some", av.Ctor);
			Assert.Equal(99, ((IntVal)av.Payload!).Value);
		}

		[Fact]
		public void OptionModule_OrElse_BothNone()
		{
			var src = @"Option.orElse None None";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
		}

		[Fact]
		public void OptionModule_ChainedOperations()
		{
			var src = @"
				let opt = Some 10 in
				let doubled = Option.map (fn x => x * 2) opt in
				let filtered = Option.filter (fn x => x > 15) doubled in
				Option.getOr filtered 0
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(20, ((IntVal)v).Value);
		}
	}
}
