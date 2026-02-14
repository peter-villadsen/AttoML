using System;
using System.Collections.Generic;
using System.Linq;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    /// <summary>
    /// Low-level Map implementation module.
    /// Returns simple types (lists) instead of ADTs to avoid type system conflicts.
    /// The high-level Map wrapper (in Prelude) provides the idiomatic ML API.
    /// </summary>
    public static class MapImplementationModule
    {
        public static ModuleVal Build()
        {
            var members = new Dictionary<string, Value>
            {
                ["empty"] = new MapVal(new Dictionary<Value, Value>(ValueEqualityComparer.Instance)),

                ["singleton"] = new ClosureVal(k =>
                {
                    return new ClosureVal(v =>
                    {
                        var map = new Dictionary<Value, Value>(ValueEqualityComparer.Instance) { [k] = v };
                        return new MapVal(map);
                    });
                }),

                ["add"] = new ClosureVal(k =>
                {
                    return new ClosureVal(v =>
                    {
                        return new ClosureVal(m =>
                        {
                            if (m is MapVal mv)
                            {
                                var newMap = new Dictionary<Value, Value>(mv.Entries, ValueEqualityComparer.Instance);
                                newMap[k] = v;
                                return new MapVal(newMap);
                            }
                            throw new Exception("add expects a key, value, and a map");
                        });
                    });
                }),

                ["remove"] = new ClosureVal(k =>
                {
                    return new ClosureVal(m =>
                    {
                        if (m is MapVal mv)
                        {
                            var newMap = new Dictionary<Value, Value>(mv.Entries, ValueEqualityComparer.Instance);
                            newMap.Remove(k);
                            return new MapVal(newMap);
                        }
                        throw new Exception("remove expects a key and a map");
                    });
                }),

                // get returns 'v list: [] = not found, [value] = found
                ["get"] = new ClosureVal(k =>
                {
                    return new ClosureVal(m =>
                    {
                        if (m is MapVal mv)
                        {
                            if (mv.Entries.TryGetValue(k, out var val))
                                return new ListVal(new[] { val }); // Found = [value]
                            return new ListVal(Array.Empty<Value>()); // Not found = []
                        }
                        throw new Exception("get expects a key and a map");
                    });
                }),

                ["contains"] = new ClosureVal(k =>
                {
                    return new ClosureVal(m =>
                    {
                        if (m is MapVal mv)
                        {
                            return new BoolVal(mv.Entries.ContainsKey(k));
                        }
                        throw new Exception("contains expects a key and a map");
                    });
                }),

                ["size"] = new ClosureVal(m =>
                {
                    if (m is MapVal mv)
                        return new IntVal(mv.Entries.Count);
                    throw new Exception("size expects a map");
                }),

                ["isEmpty"] = new ClosureVal(m =>
                {
                    if (m is MapVal mv)
                        return new BoolVal(mv.Entries.Count == 0);
                    throw new Exception("isEmpty expects a map");
                }),

                ["keys"] = new ClosureVal(m =>
                {
                    if (m is MapVal mv)
                    {
                        return new ListVal(mv.Entries.Keys.ToList());
                    }
                    throw new Exception("keys expects a map");
                }),

                ["values"] = new ClosureVal(m =>
                {
                    if (m is MapVal mv)
                    {
                        return new ListVal(mv.Entries.Values.ToList());
                    }
                    throw new Exception("values expects a map");
                }),

                ["toList"] = new ClosureVal(m =>
                {
                    if (m is MapVal mv)
                    {
                        var pairs = mv.Entries.Select(kv =>
                            new TupleVal(new List<Value> { kv.Key, kv.Value }) as Value
                        ).ToList();
                        return new ListVal(pairs);
                    }
                    throw new Exception("toList expects a map");
                }),

                ["fromList"] = new ClosureVal(lst =>
                {
                    if (lst is ListVal lv)
                    {
                        var map = new Dictionary<Value, Value>(ValueEqualityComparer.Instance);
                        foreach (var v in lv.Items)
                        {
                            if (v is TupleVal tv && tv.Items.Count == 2)
                            {
                                map[tv.Items[0]] = tv.Items[1];
                            }
                            else
                                throw new Exception("fromList expects a list of tuples");
                        }
                        return new MapVal(map);
                    }
                    throw new Exception("fromList expects a list");
                }),

                ["mapValues"] = new ClosureVal(f =>
                {
                    return new ClosureVal(m =>
                    {
                        if (f is ClosureVal fv && m is MapVal mv)
                        {
                            var newMap = new Dictionary<Value, Value>(ValueEqualityComparer.Instance);
                            foreach (var kv in mv.Entries)
                            {
                                var result = fv.Invoke(kv.Value);
                                newMap[kv.Key] = result;
                            }
                            return new MapVal(newMap);
                        }
                        throw new Exception("mapValues expects a function and a map");
                    });
                }),

                ["fold"] = new ClosureVal(f =>
                {
                    return new ClosureVal(acc =>
                    {
                        return new ClosureVal(m =>
                        {
                            if (f is ClosureVal fv && m is MapVal mv)
                            {
                                var current = acc;
                                foreach (var kv in mv.Entries)
                                {
                                    var partial = fv.Invoke(kv.Key);
                                    if (partial is ClosureVal cv)
                                    {
                                        var partial2 = cv.Invoke(kv.Value);
                                        if (partial2 is ClosureVal cv2)
                                        {
                                            current = cv2.Invoke(current);
                                        }
                                        else
                                            throw new Exception("fold function must be curried: key -> value -> acc -> acc");
                                    }
                                    else
                                        throw new Exception("fold function must be curried: key -> value -> acc -> acc");
                                }
                                return current;
                            }
                            throw new Exception("fold expects a function, initial value, and a map");
                        });
                    });
                })
            };

            return new ModuleVal(members);
        }
    }
}
