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

            // Mathematical constant pi
            m["pi"] = new FloatVal(Math.PI);

            // Exponential and logarithmic functions
            m["exp"] = new ClosureVal(a => new FloatVal(Math.Exp(((FloatVal)a).Value)));
            m["log"] = new ClosureVal(a =>
            {
                var x = ((FloatVal)a).Value;
                if (x <= 0.0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Domain", null));
                return new FloatVal(Math.Log(x));
            });

            // Trigonometric functions
            m["sin"] = new ClosureVal(a => new FloatVal(Math.Sin(((FloatVal)a).Value)));
            m["cos"] = new ClosureVal(a => new FloatVal(Math.Cos(((FloatVal)a).Value)));
            m["atan"] = new ClosureVal(a => new FloatVal(Math.Atan(((FloatVal)a).Value)));
            m["atan2"] = Curry2((a,b) => new FloatVal(Math.Atan2(((FloatVal)a).Value, ((FloatVal)b).Value)));

            // Inverse trigonometric functions
            m["asin"] = new ClosureVal(a =>
            {
                var x = ((FloatVal)a).Value;
                if (x < -1.0 || x > 1.0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Domain", null));
                return new FloatVal(Math.Asin(x));
            });
            m["acos"] = new ClosureVal(a =>
            {
                var x = ((FloatVal)a).Value;
                if (x < -1.0 || x > 1.0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Domain", null));
                return new FloatVal(Math.Acos(x));
            });

            // Hyperbolic functions
            m["sinh"] = new ClosureVal(a => new FloatVal(Math.Sinh(((FloatVal)a).Value)));
            m["cosh"] = new ClosureVal(a => new FloatVal(Math.Cosh(((FloatVal)a).Value)));
            m["tanh"] = new ClosureVal(a => new FloatVal(Math.Tanh(((FloatVal)a).Value)));

            // Other functions
            m["sqrt"] = new ClosureVal(a =>
            {
                var x = ((FloatVal)a).Value;
                if (x < 0.0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Domain", null));
                return new FloatVal(Math.Sqrt(x));
            });

            return new ModuleVal(m);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a, b)));
        }
    }
}
