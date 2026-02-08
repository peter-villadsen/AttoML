using System;
using System.Collections.Generic;
using System.Linq;

namespace AttoML.Interpreter.Runtime
{
    public abstract class Value
    {
        public abstract override string ToString();
    }

    public sealed class IntVal : Value
    {
        public int Value;
        public IntVal(int v)
        {
            Value = v;
        }
        public override string ToString() => Value.ToString();
    }

    public sealed class FloatVal : Value
    {
        public double Value;
        public FloatVal(double v)
        {
            Value = v;
        }
        public override string ToString() => Value.ToString();
    }

    public sealed class StringVal : Value
    {
        public string Value;
        public StringVal(string v)
        {
            Value = v;
        }
        public override string ToString() => "\"" + Value + "\"";
    }

    public sealed class BoolVal : Value
    {
        public bool Value;
        public BoolVal(bool v)
        {
            Value = v;
        }
        public override string ToString() => Value ? "true" : "false";
    }

    public sealed class UnitVal : Value
    {
        public static readonly UnitVal Instance = new UnitVal();
        private UnitVal()
        {
        }
        public override string ToString() => "()";
    }

    public sealed class TupleVal : Value
    {
        public IReadOnlyList<Value> Items;
        public TupleVal(IReadOnlyList<Value> items)
        {
            Items = items;
        }
        public override string ToString() => "(" + string.Join(", ", Items.Select(x => x.ToString())) + ")";
    }

    public sealed class ListVal : Value
    {
        public IReadOnlyList<Value> Items;
        public ListVal(IReadOnlyList<Value> items)
        {
            Items = items;
        }
        public override string ToString() => "[" + string.Join(", ", Items.Select(x => x.ToString())) + "]";
    }

    public sealed class SetVal : Value
    {
        public HashSet<int> Elements;
        public SetVal(HashSet<int> elements)
        {
            Elements = elements;
        }
        public override string ToString() => "{" + string.Join(", ", Elements.OrderBy(x => x)) + "}";
    }

    public sealed class MapVal : Value
    {
        public Dictionary<int, int> Entries;
        public MapVal(Dictionary<int, int> entries)
        {
            Entries = entries;
        }
        public override string ToString() => "{" + string.Join(", ", Entries.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} -> {kv.Value}")) + "}";
    }

    public sealed class RecordVal : Value
    {
        public IReadOnlyDictionary<string, Value> Fields;
        public RecordVal(IReadOnlyDictionary<string, Value> fields)
        {
            Fields = fields;
        }
        public override string ToString() => "{" + string.Join(", ", Fields.Select(kv => kv.Key + " = " + kv.Value)) + "}";
    }

    public sealed class AdtVal : Value
    {
        public string Ctor;
        public Value? Payload;
        public AdtVal(string ctor, Value? payload)
        {
            Ctor = ctor;
            Payload = payload;
        }
        public override string ToString() => Payload == null ? $"<{Ctor}>" : $"<{Ctor} {Payload}>";

        public override bool Equals(object? obj)
        {
            if (obj is not AdtVal other) return false;
            if (Ctor != other.Ctor) return false;
            if (Payload == null && other.Payload == null) return true;
            if (Payload == null || other.Payload == null) return false;
            return ValuesEqual(Payload, other.Payload);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Ctor.GetHashCode();
                if (Payload != null)
                {
                    hash = hash * 397 ^ GetValueHashCode(Payload);
                }
                return hash;
            }
        }

        private static bool ValuesEqual(Value v1, Value v2)
        {
            return (v1, v2) switch
            {
                (IntVal i1, IntVal i2) => i1.Value == i2.Value,
                (FloatVal f1, FloatVal f2) => f1.Value == f2.Value,
                (StringVal s1, StringVal s2) => s1.Value == s2.Value,
                (BoolVal b1, BoolVal b2) => b1.Value == b2.Value,
                (UnitVal, UnitVal) => true,
                (TupleVal t1, TupleVal t2) => t1.Items.Count == t2.Items.Count &&
                    t1.Items.Zip(t2.Items).All(pair => ValuesEqual(pair.First, pair.Second)),
                (ListVal l1, ListVal l2) => l1.Items.Count == l2.Items.Count &&
                    l1.Items.Zip(l2.Items).All(pair => ValuesEqual(pair.First, pair.Second)),
                (AdtVal a1, AdtVal a2) => a1.Equals(a2),
                (RecordVal r1, RecordVal r2) => r1.Fields.Count == r2.Fields.Count &&
                    r1.Fields.All(kv => r2.Fields.ContainsKey(kv.Key) && ValuesEqual(kv.Value, r2.Fields[kv.Key])),
                _ => ReferenceEquals(v1, v2)
            };
        }

        private static int GetValueHashCode(Value v)
        {
            return v switch
            {
                IntVal i => i.Value.GetHashCode(),
                FloatVal f => f.Value.GetHashCode(),
                StringVal s => s.Value.GetHashCode(),
                BoolVal b => b.Value.GetHashCode(),
                UnitVal => 0,
                TupleVal t => t.Items.Aggregate(0, (hash, elem) => unchecked(hash * 397 ^ GetValueHashCode(elem))),
                ListVal l => l.Items.Aggregate(0, (hash, elem) => unchecked(hash * 397 ^ GetValueHashCode(elem))),
                AdtVal a => a.GetHashCode(),
                RecordVal r => r.Fields.Aggregate(0, (hash, kv) => unchecked(hash * 397 ^ kv.Key.GetHashCode() ^ GetValueHashCode(kv.Value))),
                _ => v.GetHashCode()
            };
        }
    }

    public sealed class ClosureVal : Value
    {
        public Func<Value, Value> Func
        {
            get;
        }
        public ClosureVal(Func<Value, Value> f)
        {
            Func = f;
        }
        public override string ToString() => "<fun>";
        public Value Invoke(Value arg) => Func(arg);
    }

    public sealed class ModuleVal : Value
    {
        public IReadOnlyDictionary<string, Value> Members
        {
            get;
        }
        public ModuleVal(IReadOnlyDictionary<string, Value> members)
        {
            Members = members;
        }
        public override string ToString() => "{ " + string.Join(", ", Members.Keys.OrderBy(k => k)) + " }";
    }

    public sealed class PlaceholderVal : Value
    {
        public Value? ActualValue
        {
            get; set;
        }
        public override string ToString() => ActualValue?.ToString() ?? "<placeholder>";
    }
}
