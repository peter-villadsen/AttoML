using System;
using System.Collections.Generic;
using System.Linq;
using AttoML.Core.Modules;
using AttoML.Core.Parsing;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter
{
    public sealed class Evaluator
    {
        public Env GlobalEnv { get; } = new Env();
        public Dictionary<string, ModuleVal> Modules { get; } = new();

        // Uses public Runtime.AttoException for cross-module throwing/handling

        public void LoadModules(ModuleSystem ms)
        {
            // Build runtime module values from structures
            // Ensure ADT constructors are available before evaluating structure bindings,
            // so closures capture an environment that includes them.
            LoadAdts(ms);
            foreach (var s in ms.Structures.Values)
            {
                var members = new Dictionary<string, Value>();
                // Evaluate bindings in a fresh env scoped to module
                var localEnv = GlobalEnv.Clone();
                
                // Pre-declare all bindings to support mutual recursion
                // Create placeholder values that will be updated
                var placeholders = new Dictionary<string, PlaceholderVal>();
                foreach (var (bn, _, _) in s.OrderedBindings)
                {
                    var placeholder = new PlaceholderVal();
                    placeholders[bn] = placeholder;
                    localEnv.Set(bn, placeholder);
                }
                
                // Now evaluate all bindings - they can reference each other
                foreach (var (bn, bexpr, _) in s.OrderedBindings)
                {
                    try
                    {
                        var v = Eval(bexpr, localEnv);
                        members[bn] = v;
                        // Update the placeholder so any closures that captured it will see the real value
                        placeholders[bn].ActualValue = v;
                        // Also update localEnv for subsequent bindings
                        localEnv.Set(bn, v);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error evaluating {s.Name}.{bn}: {ex.Message}");
                        throw;
                    }
                }
                
                var modVal = new ModuleVal(members);
                Modules[s.Name] = modVal;
                Console.WriteLine($"Loaded module {s.Name} with {members.Count} members");
                // Inject qualified names
                foreach (var kv in members)
                {
                    GlobalEnv.Set($"{s.Name}.{kv.Key}", kv.Value);
                }
            }
        }

        public void ApplyOpen(IEnumerable<ModuleDecl> decls)
        {
            foreach (var d in decls)
            {
                if (d is OpenDecl od)
                {
                    Console.WriteLine($"Attempting to open module: {od.Name}");
                    if (!Modules.TryGetValue(od.Name, out var mv))
                    {
                        Console.WriteLine($"  Available modules: {string.Join(", ", Modules.Keys)}");
                        throw new Exception($"Unknown module {od.Name}");
                    }
                    Console.WriteLine($"  Found module with {mv.Members.Count} members");
                    foreach (var kv in mv.Members)
                    {
                        GlobalEnv.Set(kv.Key, kv.Value);
                        Console.WriteLine($"    Added to GlobalEnv: {kv.Key}");
                    }
                }
            }
        }

        public Value? ApplyValDecls(IEnumerable<ModuleDecl> decls)
        {
            Value? last = null;
            foreach (var d in decls)
            {
                if (d is ValDecl vd)
                {
                    var v = Eval(vd.Expr, GlobalEnv);
                    GlobalEnv.Set(vd.Name, v);
                    last = v;
                }
            }
            if (last != null)
            {
                GlobalEnv.Set("it", last);
            }
            return last;
        }

        public Value Eval(Expr expr, Env env)
        {
            switch (expr)
            {
                case IntLit i: return new IntVal(i.Value);
                case FloatLit f: return new FloatVal(f.Value);
                case StringLit s: return new StringVal(s.Value);
                case BoolLit b: return new BoolVal(b.Value);
                case UnitLit: return UnitVal.Instance;
                case Var v:
                    if (!env.TryGet(v.Name, out var vv)) throw new Exception($"Unbound variable {v.Name}");
                    // If it's a placeholder, return the actual value
                    if (vv is PlaceholderVal ph)
                    {
                        if (ph.ActualValue == null) throw new Exception($"Variable {v.Name} used before initialization");
                        return ph.ActualValue;
                    }
                    return vv;
                case Fun fun:
                    return new ClosureVal(arg =>
                    {
                        var env2 = env.Clone();
                        env2.Set(fun.Param, arg);
                        return Eval(fun.Body, env2);
                    });
                case App app:
                    var vf = Eval(app.Func, env) as ClosureVal;
                    if (vf == null) throw new Exception("Attempting to apply non-function");
                    var va = Eval(app.Arg, env);
                    return vf.Invoke(va);
                case Let let:
                    var v1 = Eval(let.Expr, env);
                    var env3 = env.Clone();
                    env3.Set(let.Name, v1);
                    return Eval(let.Body, env3);
                case LetRec lr:
                    // create a closure that refers to itself
                    ClosureVal? recFun = null;
                    var env4 = env.Clone();
                    recFun = new ClosureVal(arg =>
                    {
                        var env5 = env4.Clone();
                        env5.Set(lr.Name, recFun!);
                        env5.Set(lr.Param, arg);
                        return Eval(lr.FuncBody, env5);
                    });
                    env4.Set(lr.Name, recFun);
                    return Eval(lr.InBody, env4);
                case IfThenElse ite:
                    var vc = Eval(ite.Cond, env) as BoolVal;
                    if (vc == null) throw new Exception("Condition is not a bool");
                    return Eval(vc.Value ? ite.Then : ite.Else, env);
                case AttoML.Core.Parsing.Tuple tup:
                    var items = tup.Items.Select(x => Eval(x, env)).ToList();
                    return new TupleVal(items);
                case ListLit ll:
                    {
                        var items2 = ll.Items.Select(x => Eval(x, env)).ToList();
                        return new ListVal(items2);
                    }
                case RecordLit rl:
                    {
                        var dict = new Dictionary<string, Value>();
                        foreach (var (name, e) in rl.Fields)
                        {
                            dict[name] = Eval(e, env);
                        }
                        return new RecordVal(dict);
                    }
                case Qualify q:
                    var qname = $"{q.Module}.{q.Name}";
                    if (!env.TryGet(qname, out var qv))
                    {
                        // Fallback: allow unqualified lookup if qualified binding not present (e.g., constructors)
                        if (!env.TryGet(q.Name, out qv)) throw new Exception($"Unbound qualified name {qname}");
                    }
                    return qv;
                case Match m:
                    var scrut = Eval(m.Scrutinee, env);
                    string DescribePat(Pattern p)
                    {
                        return p switch
                        {
                            AttoML.Core.Parsing.PCtor pc => pc.Module != null ? $"{pc.Module}.{pc.Name}" : pc.Name,
                            AttoML.Core.Parsing.PTuple pt => $"({string.Join(", ", pt.Items.Select(DescribePat))})",
                            AttoML.Core.Parsing.PVar pv => pv.Name,
                            AttoML.Core.Parsing.PWildcard => "_",
                            AttoML.Core.Parsing.PInt pi => pi.Value.ToString(),
                            AttoML.Core.Parsing.PFloat pf => pf.Value.ToString(),
                            AttoML.Core.Parsing.PString ps => '"' + ps.Value + '"',
                            AttoML.Core.Parsing.PBool pb => pb.Value ? "true" : "false",
                            AttoML.Core.Parsing.PUnit => "()",
                            _ => p.GetType().Name
                        };
                    }
                    foreach (var (pat, branch) in m.Cases)
                    {
                        if (TryMatch(scrut, pat, env, out var envExt))
                        {
                            return Eval(branch, envExt);
                        }
                    }
                    var casesDesc = string.Join(" | ", m.Cases.Select(c => DescribePat(c.Item1)));
                    throw new Exception($"Non-exhaustive match on {scrut} with cases {casesDesc}");
                case Raise r:
                    {
                        var evx = Eval(r.Expr, env);
                        // Expect an ADT-like value as exception; we won't enforce here
                        throw new AttoException(evx);
                    }
                case Handle h:
                    {
                        try
                        {
                            return Eval(h.Expr, env);
                        }
                        catch (AttoML.Interpreter.Runtime.AttoException ax)
                        {
                            var exnVal = ax.Exn;
                            foreach (var (pat, branch) in h.Cases)
                            {
                                if (TryMatch(exnVal, pat, env, out var envExt))
                                {
                                    return Eval(branch, envExt);
                                }
                            }
                            throw; // rethrow if not handled
                        }
                    }
                default:
                    throw new NotSupportedException($"Cannot evaluate {expr.GetType().Name}");
            }
        }

        private bool TryMatch(Value v, Pattern pat, Env env, out Env extendedEnv)
        {
            // Start from a clone so bindings in patterns do not leak unless matched
            var e = env.Clone();
            switch (pat)
            {
                case PWildcard:
                    extendedEnv = e; return true;
                case PVar pv:
                    e.Set(pv.Name, v);
                    extendedEnv = e; return true;
                case PInt pi:
                    if (v is IntVal iv && iv.Value == pi.Value) { extendedEnv = e; return true; }
                    extendedEnv = env; return false;
                case PFloat pf:
                    if (v is FloatVal fv && fv.Value == pf.Value) { extendedEnv = e; return true; }
                    extendedEnv = env; return false;
                case PString ps:
                    if (v is StringVal sv && sv.Value == ps.Value) { extendedEnv = e; return true; }
                    extendedEnv = env; return false;
                case PBool pb:
                    if (v is BoolVal bv && bv.Value == pb.Value) { extendedEnv = e; return true; }
                    extendedEnv = env; return false;
                case PUnit:
                    if (v is UnitVal) { extendedEnv = e; return true; }
                    extendedEnv = env; return false;
                case PTuple pt:
                    if (v is TupleVal tv && tv.Items.Count == pt.Items.Count)
                    {
                        var currEnv = e;
                        for (int i = 0; i < pt.Items.Count; i++)
                        {
                            if (!TryMatch(tv.Items[i], pt.Items[i], currEnv, out var nextEnv))
                            {
                                extendedEnv = env; return false;
                            }
                            currEnv = nextEnv;
                        }
                        extendedEnv = currEnv; return true;
                    }
                    extendedEnv = env; return false;
                case PCtor pc:
                    if (v is AdtVal av)
                    {
                        // Compare by constructor name only; module qualifier ignored at runtime
                        if (av.Ctor != pc.Name) { extendedEnv = env; return false; }
                        if (pc.Payload == null)
                        {
                            if (av.Payload == null) { extendedEnv = e; return true; }
                            extendedEnv = env; return false;
                        }
                        else
                        {
                            if (av.Payload == null) { extendedEnv = env; return false; }
                            return TryMatch(av.Payload, pc.Payload, e, out extendedEnv);
                        }
                    }
                    extendedEnv = env; return false;
                case PList pl:
                    if (v is ListVal lv && lv.Items.Count == pl.Items.Count)
                    {
                        var currEnv = e;
                        for (int i = 0; i < pl.Items.Count; i++)
                        {
                            if (!TryMatch(lv.Items[i], pl.Items[i], currEnv, out var nextEnv))
                            {
                                extendedEnv = env;
                                return false;
                            }
                            currEnv = nextEnv;
                        }
                        extendedEnv = currEnv;
                        return true;
                    }
                    extendedEnv = env;
                    return false;
                case PListCons plc:
                    if (v is ListVal lv2 && lv2.Items.Count > 0)
                    {
                        var head = lv2.Items[0];
                        var tail = new ListVal(lv2.Items.Skip(1).ToList());
                        if (!TryMatch(head, plc.Head, e, out var headEnv))
                        {
                            extendedEnv = env;
                            return false;
                        }
                        if (!TryMatch(tail, plc.Tail, headEnv, out var tailEnv))
                        {
                            extendedEnv = env;
                            return false;
                        }
                        extendedEnv = tailEnv;
                        return true;
                    }
                    extendedEnv = env;
                    return false;
                case PRecord pr:
                    if (v is RecordVal rv)
                    {
                        var currEnv = e;
                        foreach (var (fieldName, fieldPat) in pr.Fields)
                        {
                            if (!rv.Fields.TryGetValue(fieldName, out var fieldVal))
                            {
                                extendedEnv = env;
                                return false;
                            }
                            if (!TryMatch(fieldVal, fieldPat, currEnv, out var nextEnv))
                            {
                                extendedEnv = env;
                                return false;
                            }
                            currEnv = nextEnv;
                        }
                        extendedEnv = currEnv;
                        return true;
                    }
                    extendedEnv = env;
                    return false;
                default:
                    extendedEnv = env; return false;
            }
        }

        public void LoadAdts(AttoML.Core.Modules.ModuleSystem ms)
        {
            foreach (var adt in ms.Adts.Values)
            {
                var typeName = adt.TypeName;
                foreach (var (ctor, payload) in adt.Ctors)
                {
                    if (payload == null)
                    {
                        var val = new AdtVal(ctor, null);
                        GlobalEnv.Set(ctor, val);
                        GlobalEnv.Set($"{typeName}.{ctor}", val);
                    }
                    else
                    {
                        var clo = new ClosureVal(arg => new AdtVal(ctor, arg));
                        GlobalEnv.Set(ctor, clo);
                        GlobalEnv.Set($"{typeName}.{ctor}", clo);
                    }
                }
            }
        }

        public void LoadExceptions(AttoML.Core.Modules.ModuleSystem ms)
        {
            foreach (var kv in ms.Exceptions)
            {
                var name = kv.Key;
                var payload = kv.Value;
                if (payload == null)
                {
                    GlobalEnv.Set(name, new AdtVal(name, null));
                }
                else
                {
                    GlobalEnv.Set(name, new ClosureVal(arg => new AdtVal(name, arg)));
                }
            }
        }
    }
}
