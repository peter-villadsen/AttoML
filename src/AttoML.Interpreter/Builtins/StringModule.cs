using System;
using System.Collections.Generic;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class StringModule
    {
        public static ModuleVal Build()
        {
            var m = new Dictionary<string, Value>();
            // SML-like operations
            // ^ is concatenation of two strings; we expose as concat2 (already named concat)
            m["concat"] = Curry2((a, b) => new StringVal(((StringVal)a).Value + ((StringVal)b).Value));
            // ofInt : int -> string
            m["ofInt"] = new ClosureVal(a => new StringVal(((IntVal)a).Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            // ofFloat : float -> string
            m["ofFloat"] = new ClosureVal(a => new StringVal(((FloatVal)a).Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            // toInt : string -> int (throws on invalid)
            m["toInt"] = new ClosureVal(a =>
            {
                var s = ((StringVal)a).Value;
                if (!int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var iv))
                    throw new Exception("String.toInt: invalid integer");
                return new IntVal(iv);
            });
            // toFloat : string -> float (throws on invalid)
            m["toFloat"] = new ClosureVal(a =>
            {
                var s = ((StringVal)a).Value;
                if (!double.TryParse(s, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var dv))
                    throw new Exception("String.toFloat: invalid float");
                return new FloatVal(dv);
            });
            // size : string -> int
            m["size"] = new ClosureVal(a => new IntVal(((StringVal)a).Value.Length));
            // length : string -> int (alias for size)
            m["length"] = new ClosureVal(a => new IntVal(((StringVal)a).Value.Length));
            // sub : string -> int -> string (1-char string)
            m["sub"] = Curry2((a, b) =>
            {
                var s = ((StringVal)a).Value;
                var i = ((IntVal)b).Value;
                if (i < 0 || i >= s.Length) throw new Exception("String.sub: index out of bounds");
                return new StringVal(s[i].ToString());
            });
            // substring : string -> int -> int -> string
            m["substring"] = new ClosureVal(a => new ClosureVal(b => new ClosureVal(c =>
            {
                var s = ((StringVal)a).Value;
                var start = ((IntVal)b).Value;
                var len = ((IntVal)c).Value;
                if (start < 0 || len < 0 || start + len > s.Length) throw new Exception("String.substring: range out of bounds");
                return new StringVal(s.Substring(start, len));
            })));
            // explode : string -> [string]
            m["explode"] = new ClosureVal(a =>
            {
                var s = ((StringVal)a).Value;
                var list = new List<Value>();
                foreach (var ch in s)
                {
                    list.Add(new StringVal(ch.ToString()));
                }
                return new ListVal(list);
            });
            // implode : [string] -> string
            m["implode"] = new ClosureVal(a =>
            {
                var list = (ListVal)a;
                var sb = new System.Text.StringBuilder();
                foreach (var v in list.Items)
                {
                    var sv = v as StringVal ?? throw new Exception("String.implode: expected list of strings");
                    if (sv.Value.Length != 1) throw new Exception("String.implode: elements must be single-character strings");
                    sb.Append(sv.Value);
                }
                return new StringVal(sb.ToString());
            });
            // concatList : [string] -> string (SML String.concat)
            m["concatList"] = new ClosureVal(a =>
            {
                var list = (ListVal)a;
                var sb = new System.Text.StringBuilder();
                foreach (var v in list.Items)
                {
                    var sv = v as StringVal ?? throw new Exception("String.concatList: expected list of strings");
                    sb.Append(sv.Value);
                }
                return new StringVal(sb.ToString());
            });
            // isPrefix : string -> string -> bool
            m["isPrefix"] = Curry2((a, b) => new BoolVal(((StringVal)b).Value.StartsWith(((StringVal)a).Value)));
            // isSuffix : string -> string -> bool
            m["isSuffix"] = Curry2((a, b) => new BoolVal(((StringVal)b).Value.EndsWith(((StringVal)a).Value)));
            // contains : string -> string -> bool (substring containment)
            m["contains"] = Curry2((a, b) => new BoolVal(((StringVal)b).Value.Contains(((StringVal)a).Value)));
            // translate : (string -> string) -> string -> string (maps each 1-char string)
            m["translate"] = new ClosureVal(f => new ClosureVal(s =>
            {
                var func = f as ClosureVal ?? throw new Exception("String.translate: expected function");
                var input = ((StringVal)s).Value;
                var sb = new System.Text.StringBuilder();
                foreach (var ch in input)
                {
                    var res = func.Invoke(new StringVal(ch.ToString())) as StringVal
                              ?? throw new Exception("String.translate: function must return string");
                    sb.Append(res.Value);
                }
                return new StringVal(sb.ToString());
            }));
            // compare : string -> string -> int (ordinal)
            m["compare"] = Curry2((a, b) => new IntVal(string.Compare(((StringVal)a).Value, ((StringVal)b).Value, System.StringComparison.Ordinal)));
            // equalsIgnoreCase : string -> string -> bool
            m["equalsIgnoreCase"] = Curry2((a, b) => new BoolVal(string.Equals(((StringVal)a).Value, ((StringVal)b).Value, System.StringComparison.OrdinalIgnoreCase)));
            return new ModuleVal(m);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a, b)));
        }
    }
}