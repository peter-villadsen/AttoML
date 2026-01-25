using System;
using System.Collections.Generic;
using System.Linq;

namespace AttoML.Interpreter.Runtime
{
    public abstract class Value
    {
        public abstract override string ToString();
    }

    public sealed class IntVal : Value { public int Value; public IntVal(int v){Value=v;} public override string ToString()=>Value.ToString(); }
    public sealed class FloatVal : Value { public double Value; public FloatVal(double v){Value=v;} public override string ToString()=>Value.ToString(); }
    public sealed class StringVal : Value { public string Value; public StringVal(string v){Value=v;} public override string ToString()=>"\""+Value+"\""; }
    public sealed class BoolVal : Value { public bool Value; public BoolVal(bool v){Value=v;} public override string ToString()=>Value?"true":"false"; }
    public sealed class UnitVal : Value { public static readonly UnitVal Instance = new UnitVal(); private UnitVal(){} public override string ToString()=>"()"; }
    public sealed class TupleVal : Value { public IReadOnlyList<Value> Items; public TupleVal(IReadOnlyList<Value> items){Items=items;} public override string ToString()=>"("+string.Join(", ", Items.Select(x=>x.ToString()))+")"; }
    public sealed class ListVal : Value { public IReadOnlyList<Value> Items; public ListVal(IReadOnlyList<Value> items){Items=items;} public override string ToString()=>"["+string.Join(", ", Items.Select(x=>x.ToString()))+"]"; }
    public sealed class RecordVal : Value { public IReadOnlyDictionary<string, Value> Fields; public RecordVal(IReadOnlyDictionary<string, Value> fields){Fields=fields;} public override string ToString()=>"{"+string.Join(", ", Fields.Select(kv=>kv.Key+" = "+kv.Value))+"}"; }
    public sealed class AdtVal : Value { public string Ctor; public Value? Payload; public AdtVal(string ctor, Value? payload){Ctor=ctor; Payload=payload;} public override string ToString()=> Payload==null? $"<{Ctor}>" : $"<{Ctor} {Payload}>"; }

    public sealed class ClosureVal : Value
    {
        public Func<Value, Value> Func { get; }
        public ClosureVal(Func<Value, Value> f){ Func=f; }
        public override string ToString()=>"<fun>";
        public Value Invoke(Value arg) => Func(arg);
    }

    public sealed class ModuleVal : Value
    {
        public IReadOnlyDictionary<string, Value> Members { get; }
        public ModuleVal(IReadOnlyDictionary<string, Value> members){ Members=members; }
        public override string ToString()=>"{ " + string.Join(", ", Members.Keys.OrderBy(k=>k)) + " }";
    }
}
