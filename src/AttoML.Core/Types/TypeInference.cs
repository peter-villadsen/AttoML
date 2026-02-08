using System;
using System.Collections.Generic;
using System.Linq;
using AttoML.Core.Parsing;

namespace AttoML.Core.Types
{
    public sealed class TypeInference
    {
        private int _fresh = 0;
        private TVar FreshVar() => new TVar();

        public (Subst, Type) Infer(TypeEnv env, Expr expr)
        {
            var subst = new Subst();
            var type = InferExpr(env, expr, subst);
            return (subst, type);
        }

        private Type InferExpr(TypeEnv env, Expr expr, Subst subst)
        {
            switch (expr)
            {
                case IntLit:
                    return TConst.Int;
                case FloatLit:
                    return TConst.Float;
                case StringLit:
                    return TConst.String;
                case BoolLit:
                    return TConst.Bool;
                case UnitLit:
                    return TConst.Unit;
                case Var v:
                    if (!env.TryGet(v.Name, out var sch))
                        throw new Exception($"Unbound variable '{v.Name}'");
                    return Instantiate(sch);
                case Fun f:
                    var tv = FreshVar();
                    var env2 = env.Clone();
                    env2.Add(f.Param, new Scheme(Array.Empty<TVar>(), tv));
                    var bodyT = InferExpr(env2, f.Body, subst);
                    return new TFun(subst.Apply(tv), bodyT);
                case App a:
                    var tFun = InferExpr(env, a.Func, subst);
                    var tArg = InferExpr(env, a.Arg, subst);
                    var tRes = FreshVar();
                    try
                    {
                        var s2 = Unify(tFun, new TFun(tArg, tRes));
                        subst.Compose(s2);
                        return subst.Apply(tRes);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Application type mismatch: func {tFun} applied to arg {tArg}. {ex.Message}");
                    }
                case Let l:
                    var t1 = InferExpr(env, l.Expr, subst);
                    if (l.TypeAnn != null)
                    {
                        var annTy = Modules.ModuleSystem.TypeFromTypeExpr(l.TypeAnn);
                        var sAnn = Unify(t1, annTy);
                        subst.Compose(sAnn);
                        t1 = subst.Apply(annTy);
                    }
                    var gen = Generalize(env, t1);
                    var env3 = env.Clone();
                    env3.Add(l.Name, gen);
                    return InferExpr(env3, l.Body, subst);
                case LetRec lr:
                    var tvf = FreshVar();
                    var env4 = env.Clone();
                    env4.Add(lr.Name, new Scheme(Array.Empty<TVar>(), tvf));
                    var tparam = FreshVar();
                    env4.Add(lr.Param, new Scheme(Array.Empty<TVar>(), tparam));
                    var tbody = InferExpr(env4, lr.FuncBody, subst);
                    var s3 = Unify(tvf, new TFun(tparam, tbody));
                    subst.Compose(s3);
                    if (lr.TypeAnn != null)
                    {
                        var annTy = Modules.ModuleSystem.TypeFromTypeExpr(lr.TypeAnn);
                        var sAnn = Unify(subst.Apply(tvf), annTy);
                        subst.Compose(sAnn);
                        tvf = subst.Apply(annTy) as TVar ?? tvf; // ensure tvf aligns; not strictly needed
                    }
                    var env5 = env.Clone();
                    env5.Add(lr.Name, Generalize(env, subst.Apply(tvf)));
                    return InferExpr(env5, lr.InBody, subst);
                case IfThenElse ite:
                    var tc = InferExpr(env, ite.Cond, subst);
                    var s4 = Unify(tc, TConst.Bool);
                    subst.Compose(s4);
                    var tt = InferExpr(env, ite.Then, subst);
                    var te = InferExpr(env, ite.Else, subst);
                    var s5 = Unify(tt, te);
                    subst.Compose(s5);
                    return subst.Apply(tt);
                case AttoML.Core.Parsing.Tuple tup:
                    var items = tup.Items.Select(x => InferExpr(env, x, subst)).ToList();
                    return new TTuple(items);
                case ListLit ll:
                    if (ll.Items.Count == 0)
                    {
                        var tvl = FreshVar();
                        return new TList(tvl);
                    }
                    var firstT = InferExpr(env, ll.Items[0], subst);
                    for (int i = 1; i < ll.Items.Count; i++)
                    {
                        var ti2 = InferExpr(env, ll.Items[i], subst);
                        var si = Unify(firstT, ti2);
                        subst.Compose(si);
                        firstT = subst.Apply(firstT);
                    }
                    return new TList(subst.Apply(firstT));
                case RecordLit rl:
                    var fields = new Dictionary<string, Type>();
                    foreach (var (name, e) in rl.Fields)
                    {
                        var ft = InferExpr(env, e, subst);
                        fields[name] = ft;
                    }
                    return new TRecord(fields);
                case RecordAccess ra:
                    var recT = InferExpr(env, ra.Record, subst);
                    if (recT is not TRecord trec)
                        throw new Exception($"Cannot access field '{ra.Field}' of non-record type {recT}");
                    if (!trec.Fields.TryGetValue(ra.Field, out var fieldType))
                        throw new Exception($"Record does not have field '{ra.Field}'");
                    return fieldType;
                case Qualify q:
                    // Check if this is actually record field access
                    // If q.Module is a variable with record type, treat as record access
                    if (env.TryGet(q.Module, out var varSch))
                    {
                        var varType = Instantiate(varSch);
                        if (varType is TRecord trecQual)
                        {
                            // This is record access, not module qualification
                            if (!trecQual.Fields.TryGetValue(q.Name, out var fieldTypeQual))
                                throw new Exception($"Record does not have field '{q.Name}'");
                            return fieldTypeQual;
                        }
                    }
                    // Otherwise treat as qualified module name
                    var qn = $"{q.Module}.{q.Name}";
                    if (!env.TryGet(qn, out var sch2))
                        throw new Exception($"Unbound qualified name '{qn}'");
                    return Instantiate(sch2);
                case Match m:
                    var tScrutinee = InferExpr(env, m.Scrutinee, subst);
                    // string scrutExpr = m.Scrutinee is Var vv ? vv.Name : m.Scrutinee.GetType().Name;
                    // Console.WriteLine($"[MATCH DEBUG] Match on '{scrutExpr}' ({m.Cases.Count} cases): Initial scrutinee type: {tScrutinee}");
                    Type? tResult = null;
                    int caseNum = 0;
                    foreach (var (pat, branch) in m.Cases)
                    {
                        var envCase = env.Clone();
                        // CRITICAL FIX: Apply current substitution to scrutinee type before pattern matching
                        // As we process cases, subst gets updated, so we need the current view of the scrutinee type
                        var currentScrutineeType = subst.Apply(tScrutinee);
                        // Console.WriteLine($"[MATCH DEBUG] Case #{caseNum}: scrutinee type after subst.Apply = {currentScrutineeType}");
                        // if (tScrutinee.ToString() != currentScrutineeType.ToString())
                        // {
                        //     Console.WriteLine($"[MATCH DEBUG]   Scrutinee type CHANGED from {tScrutinee} to {currentScrutineeType}");
                        // }
                        try
                        {
                            InferPattern(envCase, subst, currentScrutineeType, pat);
                        }
                        catch (Exception ex)
                        {
                            // Console.WriteLine($"[TYPE DEBUG] Error in InferPattern for case #{caseNum}:");
                            // Console.WriteLine($"  Scrutinee type: {currentScrutineeType}");
                            // Console.WriteLine($"  Pattern: {pat}");
                            throw;
                        }
                        var bt = InferExpr(envCase, branch, subst);
                        if (tResult == null)
                        {
                            tResult = bt;
                        }
                        else
                        {
                            var sm = Unify(tResult, bt);
                            subst.Compose(sm);
                            tResult = subst.Apply(tResult);
                        }
                        caseNum++;
                    }
                    if (tResult == null) throw new Exception("match must have at least one case");
                    return subst.Apply(tResult);
                case Raise r:
                    var tEx = InferExpr(env, r.Expr, subst);
                    var srx = Unify(tEx, TConst.Exn);
                    subst.Compose(srx);
                    // 'raise e' can have any type; return a fresh var so it unifies with context
                    return FreshVar();
                case Handle h:
                    var tBody = InferExpr(env, h.Expr, subst);
                    foreach (var (pat, branch) in h.Cases)
                    {
                        var envCase = env.Clone();
                        // Patterns in handle match exceptions
                        InferPattern(envCase, subst, TConst.Exn, pat);
                        var bt = InferExpr(envCase, branch, subst);
                        var su = Unify(tBody, bt);
                        subst.Compose(su);
                        tBody = subst.Apply(tBody);
                    }
                    return subst.Apply(tBody);
                default:
                    throw new NotSupportedException($"Cannot infer type for {expr.GetType().Name}");
            }
        }

        // Exposed unify helper for annotation checks
        public void AssertUnify(Type a, Type b)
        {
            // Throws if types mismatch
            Unify(a, b);
        }

        private void InferPattern(TypeEnv env, Subst subst, Type scrutineeType, Pattern pat)
        {
            switch (pat)
            {
                case PWildcard:
                    // no constraints
                    return;
                case PVar pv:
                    var tv = FreshVar();
                    var s = Unify(scrutineeType, tv);
                    subst.Compose(s);
                    var finalType = subst.Apply(tv);
                    env.Add(pv.Name, new Scheme(Array.Empty<TVar>(), finalType));
                    return;
                case PInt pi:
                    subst.Compose(Unify(scrutineeType, TConst.Int));
                    return;
                case PFloat pf:
                    subst.Compose(Unify(scrutineeType, TConst.Float));
                    return;
                case PString ps:
                    subst.Compose(Unify(scrutineeType, TConst.String));
                    return;
                case PBool pb:
                    subst.Compose(Unify(scrutineeType, TConst.Bool));
                    return;
                case PUnit:
                    subst.Compose(Unify(scrutineeType, TConst.Unit));
                    return;
                case PTuple pt:
                    // First, if scrutinee is already a concrete tuple type, use it directly
                    // This avoids creating fresh variables that might get corrupted later
                    if (scrutineeType is TTuple tt && tt.Items.Count == pt.Items.Count)
                    {
                        // Scrutinee is already a tuple with the right arity - use its element types directly
                        for (int i = 0; i < pt.Items.Count; i++)
                        {
                            InferPattern(env, subst, tt.Items[i], pt.Items[i]);
                        }
                        return;
                    }

                    // Otherwise, create fresh variables and unify
                    var elemTypes = pt.Items.Select(_ => (Type)FreshVar()).ToList();
                    var s0 = Unify(scrutineeType, new TTuple(elemTypes));
                    subst.Compose(s0);
                    for (int i = 0; i < pt.Items.Count; i++)
                    {
                        var appliedType = subst.Apply(elemTypes[i]);
                        InferPattern(env, subst, appliedType, pt.Items[i]);
                    }
                    return;
                case PCtor pc:
                    var cname = pc.Module != null ? $"{pc.Module}.{pc.Name}" : pc.Name;
                    if (!env.TryGet(cname, out var sch))
                    {
                        throw new Exception($"Unknown constructor '{cname}' in pattern");
                    }
                    var ctorT = Instantiate(sch);
                    if (ctorT is TAdt at)
                    {
                        if (pc.Payload != null) throw new Exception($"Constructor '{cname}' does not take a payload");
                        subst.Compose(Unify(scrutineeType, at));
                        return;
                    }
                    // Exception constructors return TConst.Exn (with or without payload)
                    if (ctorT is TConst cex && cex.Name == TConst.Exn.Name)
                    {
                        if (pc.Payload != null) throw new Exception($"Constructor '{cname}' does not take a payload");
                        subst.Compose(Unify(scrutineeType, TConst.Exn));
                        return;
                    }
                    if (ctorT is TFun tf && (tf.To is TAdt at2 || tf.To is TConst cex2 && cex2.Name == TConst.Exn.Name))
                    {
                        // Unify the scrutinee with the result (ADT or exn)
                        subst.Compose(Unify(scrutineeType, tf.To));
                        if (pc.Payload != null)
                        {
                            InferPattern(env, subst, tf.From, pc.Payload);
                        }
                        else
                        {
                            // No payload provided; require unit?
                            // We will not enforce unit here; simply allow omission only if from is Unit
                            // Optionally unify with Unit if omitted
                            // subst.Compose(Unify(tf.From, TConst.Unit));
                        }
                        return;
                    }
                    throw new Exception($"Invalid constructor type for '{cname}'");
                case PList pl:
                    var elemType = FreshVar();
                    var listType = new TList(elemType);
                    subst.Compose(Unify(scrutineeType, listType));
                    foreach (var item in pl.Items)
                    {
                        InferPattern(env, subst, subst.Apply(elemType), item);
                    }
                    return;
                case PListCons plc:
                    var headType = FreshVar();
                    var tailType = new TList(headType);
                    subst.Compose(Unify(scrutineeType, tailType));
                    InferPattern(env, subst, subst.Apply(headType), plc.Head);
                    InferPattern(env, subst, subst.Apply(tailType), plc.Tail);
                    return;
                case PRecord pr:
                    var fieldTypes = new Dictionary<string, Type>();
                    foreach (var (fieldName, fieldPat) in pr.Fields)
                    {
                        var ft = FreshVar();
                        fieldTypes[fieldName] = ft;
                    }
                    var recordType = new TRecord(fieldTypes);
                    subst.Compose(Unify(scrutineeType, recordType));
                    foreach (var (fieldName, fieldPat) in pr.Fields)
                    {
                        InferPattern(env, subst, subst.Apply(fieldTypes[fieldName]), fieldPat);
                    }
                    return;
                default:
                    throw new NotSupportedException($"Unsupported pattern {pat.GetType().Name}");
            }
        }

        private Subst Unify(Type a, Type b)
        {
            a = a is TVar ? a : a;
            b = b is TVar ? b : b;
            if (a is TVar va) return Bind(va, b);
            if (b is TVar vb) return Bind(vb, a);
            if (a is TConst ca && b is TConst cb)
            {
                if (ca.Name != cb.Name) throw new Exception($"Type mismatch: {ca} vs {cb}");
                return new Subst();
            }
            if (a is TFun fa && b is TFun fb)
            {
                var s1 = Unify(fa.From, fb.From);
                var s2 = Unify(s1.Apply(fa.To), s1.Apply(fb.To));
                s2.Compose(s1);
                return s2;
            }
            if (a is TTuple ta && b is TTuple tb)
            {
                if (ta.Items.Count != tb.Items.Count) throw new Exception("Tuple lengths differ");
                var s = new Subst();
                for (int i=0;i<ta.Items.Count;i++)
                {
                    var si = Unify(s.Apply(ta.Items[i]), s.Apply(tb.Items[i]));
                    s.Compose(si);
                }
                return s;
            }
            if (a is TList la && b is TList lb)
            {
                return Unify(la.Elem, lb.Elem);
            }
            if (a is TSet sa && b is TSet sb)
            {
                return Unify(sa.Elem, sb.Elem);
            }
            if (a is TMap ma && b is TMap mb)
            {
                var s1 = Unify(ma.Key, mb.Key);
                var s2 = Unify(s1.Apply(ma.Value), s1.Apply(mb.Value));
                s2.Compose(s1);
                return s2;
            }
            if (a is TRecord ra && b is TRecord rb)
            {
                if (ra.Fields.Count != rb.Fields.Count) throw new Exception("Record field counts differ");
                var s = new Subst();
                foreach (var kv in ra.Fields)
                {
                    if (!rb.Fields.TryGetValue(kv.Key, out var bt)) throw new Exception($"Record missing field {kv.Key}");
                    var si = Unify(s.Apply(kv.Value), s.Apply(bt));
                    s.Compose(si);
                }
                return s;
            }
            if (a is TAdt aa && b is TAdt bb)
            {
                if (aa.Name != bb.Name || aa.TypeArgs.Count != bb.TypeArgs.Count) throw new Exception("ADT mismatch");
                var s = new Subst();
                for (int i = 0; i < aa.TypeArgs.Count; i++)
                {
                    var si = Unify(s.Apply(aa.TypeArgs[i]), s.Apply(bb.TypeArgs[i]));
                    s.Compose(si);
                }
                return s;
            }
            throw new Exception($"Cannot unify types: {a} and {b}");
        }

        private Subst Bind(TVar v, Type t)
        {
            if (t is TVar tv && tv.Id == v.Id) return new Subst();
            if (Occurs(v, t)) throw new Exception("Occurs check failed");
            var s = new Subst();
            s.Add(v.Id, t);
            return s;
        }

        private bool Occurs(TVar v, Type t)
        {
            return t switch
            {
                TVar tv => tv.Id == v.Id,
                TFun f => Occurs(v, f.From) || Occurs(v, f.To),
                TTuple tt => tt.Items.Any(i => Occurs(v, i)),
                TList tl => Occurs(v, tl.Elem),
                TSet ts => Occurs(v, ts.Elem),
                TMap tm => Occurs(v, tm.Key) || Occurs(v, tm.Value),
                TRecord tr => tr.Fields.Values.Any(i => Occurs(v, i)),
                TAdt ta => ta.TypeArgs.Any(a => Occurs(v, a)),
                _ => false
            };
        }

        private Scheme Generalize(TypeEnv env, Type t)
        {
            // For simplicity, generalize all free type vars in t
            var free = FreeTypeVars(t).Distinct().ToList();
            return new Scheme(free, t);
        }

        private Type Instantiate(Scheme s)
        {
            var map = new Dictionary<int, TVar>();
            Type Inst(Type t)
            {
                return t switch
                {
                    TVar v =>
                        s.Quantified.Any(q => q.Id == v.Id) ?
                            map.TryGetValue(v.Id, out var nv) ? nv : map[v.Id] = new TVar() : v,
                    TFun f => new TFun(Inst(f.From), Inst(f.To)),
                    TTuple tt => new TTuple(tt.Items.Select(Inst).ToList()),
                    TList tl => new TList(Inst(tl.Elem)),
                    TSet ts => new TSet(Inst(ts.Elem)),
                    TMap tm => new TMap(Inst(tm.Key), Inst(tm.Value)),
                    TRecord tr => new TRecord(tr.Fields.ToDictionary(kv => kv.Key, kv => Inst(kv.Value))),
                    TAdt ta => new TAdt(ta.Name, ta.TypeArgs.Select(Inst).ToList()),
                    _ => t
                };
            }
            return Inst(s.Type);
        }

        private IEnumerable<TVar> FreeTypeVars(Type t)
        {
            switch (t)
            {
                case TVar v: yield return v; break;
                case TFun f:
                    foreach (var v in FreeTypeVars(f.From)) yield return v;
                    foreach (var v in FreeTypeVars(f.To)) yield return v;
                    break;
                case TTuple tt:
                    foreach (var item in tt.Items)
                        foreach (var v in FreeTypeVars(item)) yield return v;
                    break;
                case TList tl:
                    foreach (var v in FreeTypeVars(tl.Elem)) yield return v;
                    break;
                case TSet ts:
                    foreach (var v in FreeTypeVars(ts.Elem)) yield return v;
                    break;
                case TMap tm:
                    foreach (var v in FreeTypeVars(tm.Key)) yield return v;
                    foreach (var v in FreeTypeVars(tm.Value)) yield return v;
                    break;
                case TRecord tr:
                    foreach (var item in tr.Fields.Values)
                        foreach (var v in FreeTypeVars(item)) yield return v;
                    break;
                case TAdt ta:
                    foreach (var a in ta.TypeArgs)
                        foreach (var v in FreeTypeVars(a)) yield return v;
                    break;
            }
        }
    }
}
