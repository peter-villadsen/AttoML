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
                ["empty"] = new SetVal(new HashSet<int>()),

                ["singleton"] = new ClosureVal(x =>
                {
                    if (x is IntVal iv)
                    {
                        var set = new HashSet<int> { iv.Value };
                        return new SetVal(set);
                    }
                    throw new Exception("singleton expects an int");
                }),

                ["add"] = new ClosureVal(x =>
                {
                    return new ClosureVal(s =>
                    {
                        if (x is IntVal iv && s is SetVal sv)
                        {
                            var newSet = new HashSet<int>(sv.Elements);
                            newSet.Add(iv.Value);
                            return new SetVal(newSet);
                        }
                        throw new Exception("add expects an int and a set");
                    });
                }),

                ["remove"] = new ClosureVal(x =>
                {
                    return new ClosureVal(s =>
                    {
                        if (x is IntVal iv && s is SetVal sv)
                        {
                            var newSet = new HashSet<int>(sv.Elements);
                            newSet.Remove(iv.Value);
                            return new SetVal(newSet);
                        }
                        throw new Exception("remove expects an int and a set");
                    });
                }),

                ["contains"] = new ClosureVal(x =>
                {
                    return new ClosureVal(s =>
                    {
                        if (x is IntVal iv && s is SetVal sv)
                        {
                            return new BoolVal(sv.Elements.Contains(iv.Value));
                        }
                        throw new Exception("contains expects an int and a set");
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
                            var newSet = new HashSet<int>(sv1.Elements);
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
                            var newSet = new HashSet<int>(sv1.Elements);
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
                            var newSet = new HashSet<int>(sv1.Elements);
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
                        var values = sv.Elements.Select(i => new IntVal(i) as Value).ToList();
                        return new ListVal(values);
                    }
                    throw new Exception("toList expects a set");
                }),

                ["fromList"] = new ClosureVal(lst =>
                {
                    if (lst is ListVal lv)
                    {
                        var set = new HashSet<int>();
                        foreach (var v in lv.Items)
                        {
                            if (v is IntVal iv)
                                set.Add(iv.Value);
                            else
                                throw new Exception("fromList expects a list of ints");
                        }
                        return new SetVal(set);
                    }
                    throw new Exception("fromList expects a list");
                })
            };

            return new ModuleVal(members);
        }
    }
}
