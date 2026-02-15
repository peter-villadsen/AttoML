using System;
using System.Collections.Generic;
using System.Numerics;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class IntInfModule
    {
        public static ModuleVal Build()
        {
            var m = new Dictionary<string, Value>();

            // Conversion functions
            m["fromInt"] = new ClosureVal(a =>
            {
                if (a is IntVal iv)
                    return new IntInfVal(new BigInteger(iv.Value));
                throw new Exception("IntInf.fromInt requires int argument");
            });

            m["toInt"] = new ClosureVal(a =>
            {
                if (a is IntInfVal iiv)
                {
                    if (iiv.Value > int.MaxValue || iiv.Value < int.MinValue)
                        throw new AttoException(new AdtVal("Overflow", null));
                    return new IntVal((int)iiv.Value);
                }
                throw new Exception("IntInf.toInt requires intinf argument");
            });

            // String conversion
            m["toString"] = new ClosureVal(a =>
            {
                if (a is IntInfVal iiv)
                    return new StringVal(iiv.Value.ToString());
                throw new Exception("IntInf.toString requires intinf argument");
            });

            m["fromString"] = new ClosureVal(a =>
            {
                if (a is StringVal sv)
                {
                    if (BigInteger.TryParse(sv.Value, out var result))
                        return new AdtVal("Some", new IntInfVal(result));
                    return new AdtVal("None", null);
                }
                throw new Exception("IntInf.fromString requires string argument");
            });

            // Arithmetic operations (for completeness, though Base module handles operators)
            m["add"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntInfVal bb)
                    return new IntInfVal(aa.Value + bb.Value);
                throw new Exception("IntInf.add requires intinf arguments");
            });

            m["sub"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntInfVal bb)
                    return new IntInfVal(aa.Value - bb.Value);
                throw new Exception("IntInf.sub requires intinf arguments");
            });

            m["mul"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntInfVal bb)
                    return new IntInfVal(aa.Value * bb.Value);
                throw new Exception("IntInf.mul requires intinf arguments");
            });

            m["div"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntInfVal bb)
                {
                    if (bb.Value.IsZero)
                        throw new AttoException(new AdtVal("Div", null));
                    return new IntInfVal(aa.Value / bb.Value);
                }
                throw new Exception("IntInf.div requires intinf arguments");
            });

            m["mod"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntInfVal bb)
                {
                    if (bb.Value.IsZero)
                        throw new AttoException(new AdtVal("Div", null));
                    return new IntInfVal(aa.Value % bb.Value);
                }
                throw new Exception("IntInf.mod requires intinf arguments");
            });

            // Negation
            m["neg"] = new ClosureVal(a =>
            {
                if (a is IntInfVal iiv)
                    return new IntInfVal(-iiv.Value);
                throw new Exception("IntInf.neg requires intinf argument");
            });

            // Absolute value
            m["abs"] = new ClosureVal(a =>
            {
                if (a is IntInfVal iiv)
                    return new IntInfVal(BigInteger.Abs(iiv.Value));
                throw new Exception("IntInf.abs requires intinf argument");
            });

            // Comparison
            m["compare"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntInfVal bb)
                {
                    int cmp = aa.Value.CompareTo(bb.Value);
                    if (cmp < 0) return new AdtVal("LESS", null);
                    if (cmp > 0) return new AdtVal("GREATER", null);
                    return new AdtVal("EQUAL", null);
                }
                throw new Exception("IntInf.compare requires intinf arguments");
            });

            // Min/Max
            m["min"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntInfVal bb)
                    return new IntInfVal(BigInteger.Min(aa.Value, bb.Value));
                throw new Exception("IntInf.min requires intinf arguments");
            });

            m["max"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntInfVal bb)
                    return new IntInfVal(BigInteger.Max(aa.Value, bb.Value));
                throw new Exception("IntInf.max requires intinf arguments");
            });

            // Power (base^exponent, where exponent is int)
            m["pow"] = Curry2((a,b) => {
                if (a is IntInfVal aa && b is IntVal bb)
                {
                    if (bb.Value < 0)
                        throw new Exception("IntInf.pow exponent must be non-negative");
                    return new IntInfVal(BigInteger.Pow(aa.Value, bb.Value));
                }
                throw new Exception("IntInf.pow requires (intinf, int) arguments");
            });

            // Sign
            m["sign"] = new ClosureVal(a =>
            {
                if (a is IntInfVal iiv)
                    return new IntVal(iiv.Value.Sign);
                throw new Exception("IntInf.sign requires intinf argument");
            });

            return new ModuleVal(m);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a, b)));
        }
    }
}
