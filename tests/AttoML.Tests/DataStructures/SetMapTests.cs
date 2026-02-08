using Xunit;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.DataStructures
{
	public class SetMapTests : AttoMLTestBase
	{
		// Set Module Tests
		[Fact]
		public void SetModule_Empty_CreatesEmptySet()
		{
			var src = @"Set.empty";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			Assert.Empty(((SetVal)v).Elements);
		}

		[Fact]
		public void SetModule_Singleton_CreatesSetWithOneElement()
		{
			var src = @"Set.singleton 42";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			var sv = (SetVal)v;
			Assert.Single(sv.Elements);
			Assert.Contains(new IntVal(42), sv.Elements, ValueEqualityComparer.Instance);
		}

		[Fact]
		public void SetModule_Add_AddsElementToSet()
		{
			var src = @"Set.add 3 (Set.singleton 1)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			var sv = (SetVal)v;
			Assert.Equal(2, sv.Elements.Count);
			Assert.Contains(new IntVal(1), sv.Elements, ValueEqualityComparer.Instance);
			Assert.Contains(new IntVal(3), sv.Elements, ValueEqualityComparer.Instance);
		}

		[Fact]
		public void SetModule_Add_IgnoresDuplicates()
		{
			var src = @"Set.add 1 (Set.singleton 1)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			var sv = (SetVal)v;
			Assert.Single(sv.Elements);
			Assert.Contains(new IntVal(1), sv.Elements, ValueEqualityComparer.Instance);
		}

		[Fact]
		public void SetModule_Remove_RemovesElement()
		{
			var src = @"
				let s = Set.add 2 (Set.singleton 1) in
				Set.remove 1 s
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			var sv = (SetVal)v;
			Assert.Single(sv.Elements);
			Assert.Contains(new IntVal(2), sv.Elements, ValueEqualityComparer.Instance);
		}

		[Fact]
		public void SetModule_Contains_ChecksMembership()
		{
			var src = @"
				let s = Set.add 2 (Set.singleton 1) in
				Set.contains 2 s
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.True(((BoolVal)v).Value);
		}

		[Fact]
		public void SetModule_Contains_ReturnsFalseForNonMember()
		{
			var src = @"
				let s = Set.singleton 1 in
				Set.contains 2 s
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.False(((BoolVal)v).Value);
		}

		[Fact]
		public void SetModule_Size_ReturnsCount()
		{
			var src = @"
				let s = Set.add 3 (Set.add 2 (Set.singleton 1)) in
				Set.size s
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(3, ((IntVal)v).Value);
		}

		[Fact]
		public void SetModule_IsEmpty_ReturnsTrueForEmpty()
		{
			var src = @"Set.isEmpty Set.empty";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.True(((BoolVal)v).Value);
		}

		[Fact]
		public void SetModule_IsEmpty_ReturnsFalseForNonEmpty()
		{
			var src = @"Set.isEmpty (Set.singleton 1)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.False(((BoolVal)v).Value);
		}

		[Fact]
		public void SetModule_Union_CombinesSets()
		{
			var src = @"
				let s1 = Set.add 2 (Set.singleton 1) in
				let s2 = Set.add 4 (Set.singleton 3) in
				Set.union s1 s2
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			var sv = (SetVal)v;
			Assert.Equal(4, sv.Elements.Count);
			Assert.Contains(new IntVal(1), sv.Elements, ValueEqualityComparer.Instance);
			Assert.Contains(new IntVal(2), sv.Elements, ValueEqualityComparer.Instance);
			Assert.Contains(new IntVal(3), sv.Elements, ValueEqualityComparer.Instance);
			Assert.Contains(new IntVal(4), sv.Elements, ValueEqualityComparer.Instance);
		}

		[Fact]
		public void SetModule_Intersect_FindsCommonElements()
		{
			var src = @"
				let s1 = Set.add 3 (Set.add 2 (Set.singleton 1)) in
				let s2 = Set.add 4 (Set.add 3 (Set.singleton 2)) in
				Set.intersect s1 s2
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			var sv = (SetVal)v;
			Assert.Equal(2, sv.Elements.Count);
			Assert.Contains(new IntVal(2), sv.Elements, ValueEqualityComparer.Instance);
			Assert.Contains(new IntVal(3), sv.Elements, ValueEqualityComparer.Instance);
		}

		[Fact]
		public void SetModule_Diff_ComputesDifference()
		{
			var src = @"
				let s1 = Set.add 3 (Set.add 2 (Set.singleton 1)) in
				let s2 = Set.add 3 (Set.singleton 2) in
				Set.diff s1 s2
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			var sv = (SetVal)v;
			Assert.Single(sv.Elements);
			Assert.Contains(new IntVal(1), sv.Elements, ValueEqualityComparer.Instance);
		}

		[Fact]
		public void SetModule_IsSubset_ReturnsTrueForSubset()
		{
			var src = @"
				let s1 = Set.add 2 (Set.singleton 1) in
				let s2 = Set.add 3 (Set.add 2 (Set.singleton 1)) in
				Set.isSubset s1 s2
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.True(((BoolVal)v).Value);
		}

		[Fact]
		public void SetModule_IsSubset_ReturnsFalseForNonSubset()
		{
			var src = @"
				let s1 = Set.add 4 (Set.singleton 1) in
				let s2 = Set.add 3 (Set.singleton 2) in
				Set.isSubset s1 s2
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.False(((BoolVal)v).Value);
		}

		[Fact]
		public void SetModule_ToList_ConvertsSetToList()
		{
			var src = @"
				let s = Set.add 3 (Set.add 2 (Set.singleton 1)) in
				Set.toList s
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<ListVal>(v);
			var lv = (ListVal)v;
			Assert.Equal(3, lv.Items.Count);
		}

		[Fact]
		public void SetModule_FromList_CreatesSetFromList()
		{
			var src = @"Set.fromList [1, 2, 3, 2, 1]";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<SetVal>(v);
			var sv = (SetVal)v;
			Assert.Equal(3, sv.Elements.Count);
			Assert.Contains(new IntVal(1), sv.Elements, ValueEqualityComparer.Instance);
			Assert.Contains(new IntVal(2), sv.Elements, ValueEqualityComparer.Instance);
			Assert.Contains(new IntVal(3), sv.Elements, ValueEqualityComparer.Instance);
		}

		// Map Module Tests
		[Fact]
		public void MapModule_Empty_CreatesEmptyMap()
		{
			var src = @"Map.empty";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<MapVal>(v);
			Assert.Empty(((MapVal)v).Entries);
		}

		[Fact]
		public void MapModule_Singleton_CreatesMapWithOneEntry()
		{
			var src = @"Map.singleton 1 42";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<MapVal>(v);
			var mv = (MapVal)v;
			Assert.Single(mv.Entries);
			Assert.Equal(new IntVal(42), mv.Entries[new IntVal(1)], ValueEqualityComparer.Instance);
		}

		[Fact]
		public void MapModule_Add_AddsEntry()
		{
			var src = @"Map.add 2 99 (Map.singleton 1 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<MapVal>(v);
			var mv = (MapVal)v;
			Assert.Equal(2, mv.Entries.Count);
			Assert.Equal(new IntVal(42), mv.Entries[new IntVal(1)], ValueEqualityComparer.Instance);
			Assert.Equal(new IntVal(99), mv.Entries[new IntVal(2)], ValueEqualityComparer.Instance);
		}

		[Fact]
		public void MapModule_Add_UpdatesExistingKey()
		{
			var src = @"Map.add 1 100 (Map.singleton 1 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<MapVal>(v);
			var mv = (MapVal)v;
			Assert.Single(mv.Entries);
			Assert.Equal(new IntVal(100), mv.Entries[new IntVal(1)], ValueEqualityComparer.Instance);
		}

		[Fact]
		public void MapModule_Remove_RemovesEntry()
		{
			var src = @"
				let m = Map.add 2 99 (Map.singleton 1 42) in
				Map.remove 1 m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<MapVal>(v);
			var mv = (MapVal)v;
			Assert.Single(mv.Entries);
			Assert.Equal(new IntVal(99), mv.Entries[new IntVal(2)], ValueEqualityComparer.Instance);
		}

		[Fact]
		public void MapModule_Get_ReturnsSomeForExistingKey()
		{
			var src = @"
				let m = Map.singleton 1 42 in
				Map.get 1 m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("Some", av.Ctor);
			Assert.IsType<IntVal>(av.Payload);
			Assert.Equal(42, ((IntVal)av.Payload!).Value);
		}

		[Fact]
		public void MapModule_Get_ReturnsNoneForMissingKey()
		{
			var src = @"
				let m = Map.singleton 1 42 in
				Map.get 2 m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<AdtVal>(v);
			var av = (AdtVal)v;
			Assert.Equal("None", av.Ctor);
			Assert.Null(av.Payload);
		}

		[Fact]
		public void MapModule_Contains_ChecksKeyExistence()
		{
			var src = @"
				let m = Map.singleton 1 42 in
				Map.contains 1 m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.True(((BoolVal)v).Value);
		}

		[Fact]
		public void MapModule_Contains_ReturnsFalseForMissingKey()
		{
			var src = @"
				let m = Map.singleton 1 42 in
				Map.contains 2 m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.False(((BoolVal)v).Value);
		}

		[Fact]
		public void MapModule_Size_ReturnsEntryCount()
		{
			var src = @"
				let m = Map.add 3 7 (Map.add 2 6 (Map.singleton 1 5)) in
				Map.size m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(3, ((IntVal)v).Value);
		}

		[Fact]
		public void MapModule_IsEmpty_ReturnsTrueForEmpty()
		{
			var src = @"Map.isEmpty Map.empty";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.True(((BoolVal)v).Value);
		}

		[Fact]
		public void MapModule_IsEmpty_ReturnsFalseForNonEmpty()
		{
			var src = @"Map.isEmpty (Map.singleton 1 42)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<BoolVal>(v);
			Assert.False(((BoolVal)v).Value);
		}

		[Fact]
		public void MapModule_Keys_ReturnsListOfKeys()
		{
			var src = @"
				let m = Map.add 2 6 (Map.singleton 1 5) in
				Map.keys m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<ListVal>(v);
			var lv = (ListVal)v;
			Assert.Equal(2, lv.Items.Count);
		}

		[Fact]
		public void MapModule_Values_ReturnsListOfValues()
		{
			var src = @"
				let m = Map.add 2 6 (Map.singleton 1 5) in
				Map.values m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<ListVal>(v);
			var lv = (ListVal)v;
			Assert.Equal(2, lv.Items.Count);
		}

		[Fact]
		public void MapModule_ToList_ConvertsMapToListOfPairs()
		{
			var src = @"
				let m = Map.add 2 6 (Map.singleton 1 5) in
				Map.toList m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<ListVal>(v);
			var lv = (ListVal)v;
			Assert.Equal(2, lv.Items.Count);
			Assert.IsType<TupleVal>(lv.Items[0]);
			var tv = (TupleVal)lv.Items[0];
			Assert.Equal(2, tv.Items.Count);
			Assert.IsType<IntVal>(tv.Items[0]);
			Assert.IsType<IntVal>(tv.Items[1]);
		}

		[Fact]
		public void MapModule_FromList_CreatesMapFromPairs()
		{
			var src = @"Map.fromList [(1, 5), (2, 6), (3, 7)]";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<MapVal>(v);
			var mv = (MapVal)v;
			Assert.Equal(3, mv.Entries.Count);
			Assert.Equal(new IntVal(5), mv.Entries[new IntVal(1)], ValueEqualityComparer.Instance);
			Assert.Equal(new IntVal(6), mv.Entries[new IntVal(2)], ValueEqualityComparer.Instance);
			Assert.Equal(new IntVal(7), mv.Entries[new IntVal(3)], ValueEqualityComparer.Instance);
		}

		[Fact]
		public void MapModule_FromList_OverwritesDuplicateKeys()
		{
			var src = @"Map.fromList [(1, 5), (1, 10)]";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<MapVal>(v);
			var mv = (MapVal)v;
			Assert.Single(mv.Entries);
			Assert.Equal(new IntVal(10), mv.Entries[new IntVal(1)], ValueEqualityComparer.Instance);
		}

		[Fact]
		public void MapModule_MapValues_AppliesFunctionToValues()
		{
			var src = @"
				let m = Map.add 2 3 (Map.singleton 1 2) in
				Map.mapValues (fn x => x * 10) m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<MapVal>(v);
			var mv = (MapVal)v;
			Assert.Equal(2, mv.Entries.Count);
			Assert.Equal(new IntVal(20), mv.Entries[new IntVal(1)], ValueEqualityComparer.Instance);
			Assert.Equal(new IntVal(30), mv.Entries[new IntVal(2)], ValueEqualityComparer.Instance);
		}

		[Fact]
		public void MapModule_Fold_AccumulatesOverEntries()
		{
			var src = @"
				let m = Map.add 2 20 (Map.singleton 1 10) in
				Map.fold (fn k => fn v => fn acc => acc + v) 0 m
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<IntVal>(v);
			Assert.Equal(30, ((IntVal)v).Value);
		}
	}
}
