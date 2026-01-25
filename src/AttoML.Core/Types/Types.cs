using System;
using System.Collections.Generic;
using System.Linq;

namespace AttoML.Core.Types
{
    public abstract class Type
    {
        public abstract override string ToString();
    }

    public sealed class TVar : Type
    {
        private static int _nextId = 0;
        public int Id { get; }
        public TVar(){ Id = _nextId++; }
        public override string ToString() => $"'a{Id}";
    }

    public sealed class TConst : Type
    {
        public string Name { get; }
        public TConst(string name){ Name = name; }
        public override string ToString() => Name;
        public static readonly TConst Int = new("int");
        public static readonly TConst Bool = new("bool");
        public static readonly TConst Float = new("float");
        public static readonly TConst String = new("string");
        public static readonly TConst Unit = new("unit");
        public static readonly TConst Exn = new("exn");
    }

    public sealed class TFun : Type
    {
        public Type From { get; }
        public Type To { get; }
        public TFun(Type from, Type to){ From=from; To=to; }
        public override string ToString() => $"{From} -> {To}";
    }

    public sealed class TTuple : Type
    {
        public IReadOnlyList<Type> Items { get; }
        public TTuple(IReadOnlyList<Type> items){ Items=items; }
        public override string ToString() => $"(" + string.Join(", ", Items.Select(x=>x.ToString())) + ")";
    }

    public sealed class TList : Type
    {
        public Type Elem { get; }
        public TList(Type elem){ Elem=elem; }
        public override string ToString() => $"[" + Elem.ToString() + "]";
    }

    public sealed class TRecord : Type
    {
        public IReadOnlyDictionary<string, Type> Fields { get; }
        public TRecord(IReadOnlyDictionary<string, Type> fields){ Fields=fields; }
        public override string ToString() => "{" + string.Join(", ", Fields.Select(kv => kv.Key+": "+kv.Value)) + "}";
    }

    public sealed class TAdt : Type
    {
        public string Name { get; }
        public IReadOnlyList<Type> TypeArgs { get; }
        public TAdt(string name, IReadOnlyList<Type>? args = null){ Name=name; TypeArgs=args ?? Array.Empty<Type>(); }
        public override string ToString() => TypeArgs.Count==0 ? Name : Name + "<" + string.Join(", ", TypeArgs.Select(a=>a.ToString())) + ">";
    }

    public sealed class Scheme
    {
        public IReadOnlyList<TVar> Quantified { get; }
        public Type Type { get; }
        public Scheme(IReadOnlyList<TVar> qs, Type t){ Quantified=qs; Type=t; }
        public override string ToString() =>
            Quantified.Count == 0 ? Type.ToString() : $"forall {string.Join(" ", Quantified)}. {Type}";
    }

    public sealed class Subst
    {
        private readonly Dictionary<int, Type> _map = new();
        public void Add(int id, Type t) => _map[id] = t;
        public Type Apply(Type t)
        {
            return t switch
            {
                TVar v => _map.TryGetValue(v.Id, out var tv) ? Apply(tv) : v,
                TFun f => new TFun(Apply(f.From), Apply(f.To)),
                TTuple tt => new TTuple(tt.Items.Select(Apply).ToList()),
                TList tl => new TList(Apply(tl.Elem)),
                TRecord tr => new TRecord(tr.Fields.ToDictionary(kv => kv.Key, kv => Apply(kv.Value))),
                TAdt ta => new TAdt(ta.Name, ta.TypeArgs.Select(Apply).ToList()),
                _ => t
            };
        }
        public void Compose(Subst other)
        {
            foreach (var kv in other._map)
            {
                _map[kv.Key] = Apply(kv.Value);
            }
        }
    }
}
