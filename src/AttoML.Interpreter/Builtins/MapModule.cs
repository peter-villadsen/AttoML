using System;
using System.Collections.Generic;
using System.Linq;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class MapModule
    {
        public static ModuleVal Build()
        {
            var members = new Dictionary<string, Value>
            {
                ["empty"] = new MapVal(new Dictionary<int, int>()),

                ["singleton"] = new ClosureVal(k =>
                {
                    return new ClosureVal(v =>
                    {
                        if (k is IntVal kv && v is IntVal vv)
                        {
                            var map = new Dictionary<int, int> { [kv.Value] = vv.Value };
                            return new MapVal(map);
                        }
                        throw new Exception("singleton expects two ints");
                    });
                }),

                ["add"] = new ClosureVal(k =>
                {
                    return new ClosureVal(v =>
                    {
                        return new ClosureVal(m =>
                        {
                            if (k is IntVal kv && v is IntVal vv && m is MapVal mv)
                            {
                                var newMap = new Dictionary<int, int>(mv.Entries);
                                newMap[kv.Value] = vv.Value;
                                return new MapVal(newMap);
                            }
                            throw new Exception("add expects two ints and a map");
                        });
                    });
                }),

                ["remove"] = new ClosureVal(k =>
                {
                    return new ClosureVal(m =>
                    {
                        if (k is IntVal kv && m is MapVal mv)
                        {
                            var newMap = new Dictionary<int, int>(mv.Entries);
                            newMap.Remove(kv.Value);
                            return new MapVal(newMap);
                        }
                        throw new Exception("remove expects an int and a map");
                    });
                }),

                ["get"] = new ClosureVal(k =>
                {
                    return new ClosureVal(m =>
                    {
                        if (k is IntVal kv && m is MapVal mv)
                        {
                            if (mv.Entries.TryGetValue(kv.Value, out var val))
                                return new AdtVal("Some", new IntVal(val));
                            return new AdtVal("None", null);
                        }
                        throw new Exception("get expects an int and a map");
                    });
                }),

                ["contains"] = new ClosureVal(k =>
                {
                    return new ClosureVal(m =>
                    {
                        if (k is IntVal kv && m is MapVal mv)
                        {
                            return new BoolVal(mv.Entries.ContainsKey(kv.Value));
                        }
                        throw new Exception("contains expects an int and a map");
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
                        var values = mv.Entries.Keys.Select(k => new IntVal(k) as Value).ToList();
                        return new ListVal(values);
                    }
                    throw new Exception("keys expects a map");
                }),

                ["values"] = new ClosureVal(m =>
                {
                    if (m is MapVal mv)
                    {
                        var values = mv.Entries.Values.Select(v => new IntVal(v) as Value).ToList();
                        return new ListVal(values);
                    }
                    throw new Exception("values expects a map");
                }),

                ["toList"] = new ClosureVal(m =>
                {
                    if (m is MapVal mv)
                    {
                        var pairs = mv.Entries.Select(kv =>
                            new TupleVal(new List<Value> { new IntVal(kv.Key), new IntVal(kv.Value) }) as Value
                        ).ToList();
                        return new ListVal(pairs);
                    }
                    throw new Exception("toList expects a map");
                }),

                ["fromList"] = new ClosureVal(lst =>
                {
                    if (lst is ListVal lv)
                    {
                        var map = new Dictionary<int, int>();
                        foreach (var v in lv.Items)
                        {
                            if (v is TupleVal tv && tv.Items.Count == 2 &&
                                tv.Items[0] is IntVal kv && tv.Items[1] is IntVal vv)
                            {
                                map[kv.Value] = vv.Value;
                            }
                            else
                                throw new Exception("fromList expects a list of (int * int) tuples");
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
                            var newMap = new Dictionary<int, int>();
                            foreach (var kv in mv.Entries)
                            {
                                var result = fv.Invoke(new IntVal(kv.Value));
                                if (result is IntVal iv)
                                    newMap[kv.Key] = iv.Value;
                                else
                                    throw new Exception("mapValues function must return int");
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
                                    var keyVal = new IntVal(kv.Key);
                                    var valVal = new IntVal(kv.Value);
                                    var partial = fv.Invoke(keyVal);
                                    if (partial is ClosureVal cv)
                                    {
                                        var partial2 = cv.Invoke(valVal);
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
