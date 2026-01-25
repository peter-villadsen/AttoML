using System;
using System.Collections.Generic;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class ComplexModule
    {
        // Complex numbers represented as records: { re = float, im = float }
        public static ModuleVal Build()
        {
            var m = new Dictionary<string, Value>();
            m["mk"] = Curry2((a, b) =>
            {
                var re = ((FloatVal)a).Value;
                var im = ((FloatVal)b).Value;
                return new RecordVal(new Dictionary<string, Value>
                {
                    { "re", new FloatVal(re) },
                    { "im", new FloatVal(im) }
                });
            });
            m["add"] = Curry2((x, y) =>
            {
                var xr = ((FloatVal)((RecordVal)x).Fields["re"]).Value;
                var xi = ((FloatVal)((RecordVal)x).Fields["im"]).Value;
                var yr = ((FloatVal)((RecordVal)y).Fields["re"]).Value;
                var yi = ((FloatVal)((RecordVal)y).Fields["im"]).Value;
                return new RecordVal(new Dictionary<string, Value>
                {
                    { "re", new FloatVal(xr + yr) },
                    { "im", new FloatVal(xi + yi) }
                });
            });
            m["sub"] = Curry2((x, y) =>
            {
                var xr = ((FloatVal)((RecordVal)x).Fields["re"]).Value;
                var xi = ((FloatVal)((RecordVal)x).Fields["im"]).Value;
                var yr = ((FloatVal)((RecordVal)y).Fields["re"]).Value;
                var yi = ((FloatVal)((RecordVal)y).Fields["im"]).Value;
                return new RecordVal(new Dictionary<string, Value>
                {
                    { "re", new FloatVal(xr - yr) },
                    { "im", new FloatVal(xi - yi) }
                });
            });
            m["mul"] = Curry2((x, y) =>
            {
                var xr = ((FloatVal)((RecordVal)x).Fields["re"]).Value;
                var xi = ((FloatVal)((RecordVal)x).Fields["im"]).Value;
                var yr = ((FloatVal)((RecordVal)y).Fields["re"]).Value;
                var yi = ((FloatVal)((RecordVal)y).Fields["im"]).Value;
                return new RecordVal(new Dictionary<string, Value>
                {
                    { "re", new FloatVal(xr * yr - xi * yi) },
                    { "im", new FloatVal(xr * yi + xi * yr) }
                });
            });
            m["div"] = Curry2((x, y) =>
            {
                var xr = ((FloatVal)((RecordVal)x).Fields["re"]).Value;
                var xi = ((FloatVal)((RecordVal)x).Fields["im"]).Value;
                var yr = ((FloatVal)((RecordVal)y).Fields["re"]).Value;
                var yi = ((FloatVal)((RecordVal)y).Fields["im"]).Value;
                var denom = yr * yr + yi * yi;
                if (denom == 0) throw new Exception("Complex.div: division by zero");
                return new RecordVal(new Dictionary<string, Value>
                {
                    { "re", new FloatVal((xr * yr + xi * yi) / denom) },
                    { "im", new FloatVal((xi * yr - xr * yi) / denom) }
                });
            });
            m["conj"] = new ClosureVal(x =>
            {
                var xr = ((FloatVal)((RecordVal)x).Fields["re"]).Value;
                var xi = ((FloatVal)((RecordVal)x).Fields["im"]).Value;
                return new RecordVal(new Dictionary<string, Value>
                {
                    { "re", new FloatVal(xr) },
                    { "im", new FloatVal(-xi) }
                });
            });
            m["abs"] = new ClosureVal(x =>
            {
                var xr = ((FloatVal)((RecordVal)x).Fields["re"]).Value;
                var xi = ((FloatVal)((RecordVal)x).Fields["im"]).Value;
                return new FloatVal(Math.Sqrt(xr * xr + xi * xi));
            });
            m["phase"] = new ClosureVal(x =>
            {
                var xr = ((FloatVal)((RecordVal)x).Fields["re"]).Value;
                var xi = ((FloatVal)((RecordVal)x).Fields["im"]).Value;
                return new FloatVal(Math.Atan2(xi, xr));
            });
            return new ModuleVal(m);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a, b)));
        }
    }
}