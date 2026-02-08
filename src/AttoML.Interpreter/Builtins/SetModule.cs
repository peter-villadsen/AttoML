using System;
using System.Collections.Generic;
using System.Linq;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class SetModule
    {
        public static ModuleVal Build()
        {
            var members = new Dictionary<string, Value>
            {
                ["empty"] = new SetVal(new HashSet<Value>(ValueEqualityComparer.Instance)),

                ["singleton"] = new ClosureVal(x =>
                {
                    var set = new HashSet<Value>(ValueEqualityComparer.Instance) { x };
                    return new SetVal(set);
                }),

                ["add"] = new ClosureVal(x =>
                {
                    return new ClosureVal(s =>
                    {
                        if (s is SetVal sv)
                        {
                            var newSet = new HashSet<Value>(sv.Elements, ValueEqualityComparer.Instance);
                            newSet.Add(x);
                            return new SetVal(newSet);
                        }
                        throw new Exception("add expects a value and a set");
                    });
                }),

                ["remove"] = new ClosureVal(x =>
                {
                    return new ClosureVal(s =>
                    {
                        if (s is SetVal sv)
                        {
                            var newSet = new HashSet<Value>(sv.Elements, ValueEqualityComparer.Instance);
                            newSet.Remove(x);
                            return new SetVal(newSet);
                        }
                        throw new Exception("remove expects a value and a set");
                    });
                }),

                ["contains"] = new ClosureVal(x =>
                {
                    return new ClosureVal(s =>
                    {
                        if (s is SetVal sv)
                        {
                            return new BoolVal(sv.Elements.Contains(x, ValueEqualityComparer.Instance));
                        }
                        throw new Exception("contains expects a value and a set");
                    });
                }),

                ["size"] = new ClosureVal(s =>
                {
                    if (s is SetVal sv)
                        return new IntVal(sv.Elements.Count);
                    throw new Exception("size expects a set");
                }),

                ["isEmpty"] = new ClosureVal(s =>
                {
                    if (s is SetVal sv)
                        return new BoolVal(sv.Elements.Count == 0);
                    throw new Exception("isEmpty expects a set");
                }),

                ["union"] = new ClosureVal(s1 =>
                {
                    return new ClosureVal(s2 =>
                    {
                        if (s1 is SetVal sv1 && s2 is SetVal sv2)
                        {
                            var newSet = new HashSet<Value>(sv1.Elements, ValueEqualityComparer.Instance);
                            newSet.UnionWith(sv2.Elements);
                            return new SetVal(newSet);
                        }
                        throw new Exception("union expects two sets");
                    });
                }),

                ["intersect"] = new ClosureVal(s1 =>
                {
                    return new ClosureVal(s2 =>
                    {
                        if (s1 is SetVal sv1 && s2 is SetVal sv2)
                        {
                            var newSet = new HashSet<Value>(sv1.Elements, ValueEqualityComparer.Instance);
                            newSet.IntersectWith(sv2.Elements);
                            return new SetVal(newSet);
                        }
                        throw new Exception("intersect expects two sets");
                    });
                }),

                ["diff"] = new ClosureVal(s1 =>
                {
                    return new ClosureVal(s2 =>
                    {
                        if (s1 is SetVal sv1 && s2 is SetVal sv2)
                        {
                            var newSet = new HashSet<Value>(sv1.Elements, ValueEqualityComparer.Instance);
                            newSet.ExceptWith(sv2.Elements);
                            return new SetVal(newSet);
                        }
                        throw new Exception("diff expects two sets");
                    });
                }),

                ["isSubset"] = new ClosureVal(s1 =>
                {
                    return new ClosureVal(s2 =>
                    {
                        if (s1 is SetVal sv1 && s2 is SetVal sv2)
                        {
                            return new BoolVal(sv1.Elements.IsSubsetOf(sv2.Elements));
                        }
                        throw new Exception("isSubset expects two sets");
                    });
                }),

                ["toList"] = new ClosureVal(s =>
                {
                    if (s is SetVal sv)
                    {
                        return new ListVal(sv.Elements.ToList());
                    }
                    throw new Exception("toList expects a set");
                }),

                ["fromList"] = new ClosureVal(lst =>
                {
                    if (lst is ListVal lv)
                    {
                        var set = new HashSet<Value>(lv.Items, ValueEqualityComparer.Instance);
                        return new SetVal(set);
                    }
                    throw new Exception("fromList expects a list");
                })
            };

            return new ModuleVal(members);
        }
    }
}
