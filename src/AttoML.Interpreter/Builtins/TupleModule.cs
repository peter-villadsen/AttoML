using System.Collections.Generic;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
	public static class TupleModule
	{
		public static ModuleVal Build()
		{
			var members = new Dictionary<string, Value>
			{
				// fst : ('a * 'b) -> 'a
				["fst"] = new ClosureVal(tuple =>
				{
					if (tuple is TupleVal tv && tv.Items.Count == 2)
					{
						return tv.Items[0];
					}
					throw new System.Exception("fst expects a 2-tuple");
				}),

				// snd : ('a * 'b) -> 'b
				["snd"] = new ClosureVal(tuple =>
				{
					if (tuple is TupleVal tv && tv.Items.Count == 2)
					{
						return tv.Items[1];
					}
					throw new System.Exception("snd expects a 2-tuple");
				}),

				// swap : ('a * 'b) -> ('b * 'a)
				["swap"] = new ClosureVal(tuple =>
				{
					if (tuple is TupleVal tv && tv.Items.Count == 2)
					{
						return new TupleVal(new List<Value> { tv.Items[1], tv.Items[0] });
					}
					throw new System.Exception("swap expects a 2-tuple");
				}),

				// curry : (('a * 'b) -> 'c) -> 'a -> 'b -> 'c
				["curry"] = new ClosureVal(func =>
				{
					return new ClosureVal(a =>
					{
						return new ClosureVal(b =>
						{
							var tuple = new TupleVal(new List<Value> { a, b });
							if (func is ClosureVal cv)
							{
								return cv.Invoke(tuple);
							}
							throw new System.Exception("curry expects a function as first argument");
						});
					});
				}),

				// uncurry : ('a -> 'b -> 'c) -> ('a * 'b) -> 'c
				["uncurry"] = new ClosureVal(func =>
				{
					return new ClosureVal(tuple =>
					{
						if (tuple is TupleVal tv && tv.Items.Count == 2)
						{
							if (func is ClosureVal cv1)
							{
								var partial = cv1.Invoke(tv.Items[0]);
								if (partial is ClosureVal cv2)
								{
									return cv2.Invoke(tv.Items[1]);
								}
							}
							throw new System.Exception("uncurry expects a curried function");
						}
						throw new System.Exception("uncurry expects a 2-tuple");
					});
				}),

				// fst3 : ('a * 'b * 'c) -> 'a
				["fst3"] = new ClosureVal(tuple =>
				{
					if (tuple is TupleVal tv && tv.Items.Count == 3)
					{
						return tv.Items[0];
					}
					throw new System.Exception("fst3 expects a 3-tuple");
				}),

				// snd3 : ('a * 'b * 'c) -> 'b
				["snd3"] = new ClosureVal(tuple =>
				{
					if (tuple is TupleVal tv && tv.Items.Count == 3)
					{
						return tv.Items[1];
					}
					throw new System.Exception("snd3 expects a 3-tuple");
				}),

				// thd3 : ('a * 'b * 'c) -> 'c
				["thd3"] = new ClosureVal(tuple =>
				{
					if (tuple is TupleVal tv && tv.Items.Count == 3)
					{
						return tv.Items[2];
					}
					throw new System.Exception("thd3 expects a 3-tuple");
				})
			};
			return new ModuleVal(members);
		}
	}
}
