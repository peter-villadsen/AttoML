using System;
using System.Collections.Generic;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class MathModule
    {
        public static ModuleVal Build()
        {
            var m = new Dictionary<string, Value>();
            m["exp"] = new ClosureVal(a => new FloatVal(Math.Exp(((FloatVal)a).Value)));
            m["log"] = new ClosureVal(a =>
            {
                var x = ((FloatVal)a).Value;
                if (x <= 0.0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Domain", null));
                return new FloatVal(Math.Log(x));
            });
            m["sin"] = new ClosureVal(a => new FloatVal(Math.Sin(((FloatVal)a).Value)));
            m["cos"] = new ClosureVal(a => new FloatVal(Math.Cos(((FloatVal)a).Value)));
            m["sqrt"] = new ClosureVal(a =>
            {
                var x = ((FloatVal)a).Value;
                if (x < 0.0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Domain", null));
                return new FloatVal(Math.Sqrt(x));
            });
            m["atan"] = new ClosureVal(a => new FloatVal(Math.Atan(((FloatVal)a).Value)));
            m["atan2"] = Curry2((a,b) => new FloatVal(Math.Atan2(((FloatVal)a).Value, ((FloatVal)b).Value)));
            return new ModuleVal(m);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a, b)));
        }
    }
}
