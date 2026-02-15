using System;
using System.Collections.Generic;
using System.Numerics;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class BaseModule
    {
        public static ModuleVal Build()
        {
            var m = new Dictionary<string, Value>();
            m["add"] = Curry2(Numeric((x,y) => x + y, (x,y) => x + y));
            m["sub"] = Curry2(Numeric((x,y) => x - y, (x,y) => x - y));
            m["mul"] = Curry2(Numeric((x,y) => x * y, (x,y) => x * y));
            m["div"] = Curry2(NumericDiv());
            m["idiv"] = Curry2(IntDiv());
            m["mod"] = Curry2(IntMod());
            m["eq"]  = Curry2((a,b) => new BoolVal(EqualsVal(a,b)));
            m["lt"]  = Curry2(NumericCmp((x,y) => x < y, (x,y) => x < y));
            m["and"] = Curry2((a,b) => new BoolVal(((BoolVal)a).Value && ((BoolVal)b).Value));
            m["or"]  = Curry2((a,b) => new BoolVal(((BoolVal)a).Value || ((BoolVal)b).Value));
            m["not"] = new ClosureVal(a => new BoolVal(!((BoolVal)a).Value));
            return new ModuleVal(m);
        }

        private static bool EqualsVal(Value a, Value b)
        {
            if (a is IntVal ai && b is IntVal bi) return ai.Value == bi.Value;
            if (a is FloatVal af && b is FloatVal bf) return af.Value == bf.Value;
            if (a is IntInfVal aii && b is IntInfVal bii) return aii.Value == bii.Value;
            if (a is StringVal asv && b is StringVal bsv) return asv.Value == bsv.Value;
            if (a is BoolVal ab && b is BoolVal bb) return ab.Value == bb.Value;
            if (a is UnitVal && b is UnitVal) return true;
            // Use the Equals override for ADT values (and other types)
            if (a is AdtVal || b is AdtVal) return a.Equals(b);
            // Fallback to reference equality for other types
            return ReferenceEquals(a, b);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a,b)));
        }

        private static Func<Value, Value, Value> Numeric(Func<int,int,int> intOp, Func<double,double,double> floatOp)
        {
            return (a,b) =>
            {
                if (a is IntVal ai && b is IntVal bi) return new IntVal(intOp(ai.Value, bi.Value));
                if (a is FloatVal af && b is FloatVal bf) return new FloatVal(floatOp(af.Value, bf.Value));
                if (a is IntInfVal aii && b is IntInfVal bii)
                {
                    // Map int operations to BigInteger
                    return new IntInfVal(intOp switch
                    {
                        _ when intOp(1, 1) == 2 => aii.Value + bii.Value,  // add
                        _ when intOp(1, 1) == 0 => aii.Value - bii.Value,  // sub
                        _ when intOp(2, 3) == 6 => aii.Value * bii.Value,  // mul
                        _ => throw new Exception("Unknown operation")
                    });
                }
                throw new Exception("Numeric operation requires matching types (int, float, or intinf)");
            };
        }

        private static Func<Value, Value, Value> NumericDiv()
        {
            return (a,b) =>
            {
                if (a is IntVal ai && b is IntVal bi)
                {
                    if (bi.Value == 0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Div", null));
                    return new IntVal(ai.Value / bi.Value);
                }
                if (a is FloatVal af && b is FloatVal bf)
                {
                    return new FloatVal(af.Value / bf.Value);
                }
                if (a is IntInfVal aii && b is IntInfVal bii)
                {
                    if (bii.Value.IsZero) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Div", null));
                    return new IntInfVal(aii.Value / bii.Value);
                }
                throw new Exception("Numeric division requires matching types (int, float, or intinf)");
            };
        }

        private static Func<Value, Value, Value> IntDiv()
        {
            return (a,b) =>
            {
                if (a is IntVal ai && b is IntVal bi)
                {
                    if (bi.Value == 0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Div", null));
                    return new IntVal(ai.Value / bi.Value);
                }
                throw new Exception("div operator requires integer operands");
            };
        }

        private static Func<Value, Value, Value> IntMod()
        {
            return (a,b) =>
            {
                if (a is IntVal ai && b is IntVal bi)
                {
                    if (bi.Value == 0) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Div", null));
                    return new IntVal(ai.Value % bi.Value);
                }
                if (a is IntInfVal aii && b is IntInfVal bii)
                {
                    if (bii.Value.IsZero) throw new AttoML.Interpreter.Runtime.AttoException(new AdtVal("Div", null));
                    return new IntInfVal(aii.Value % bii.Value);
                }
                throw new Exception("mod operator requires integer operands (int or intinf)");
            };
        }

        private static Func<Value, Value, Value> NumericCmp(Func<int,int,bool> intPred, Func<double,double,bool> floatPred)
        {
            return (a,b) =>
            {
                if (a is IntVal ai && b is IntVal bi) return new BoolVal(intPred(ai.Value, bi.Value));
                if (a is FloatVal af && b is FloatVal bf) return new BoolVal(floatPred(af.Value, bf.Value));
                if (a is IntInfVal aii && b is IntInfVal bii)
                {
                    // Map int comparison to BigInteger
                    int cmp = aii.Value.CompareTo(bii.Value);
                    return new BoolVal(intPred switch
                    {
                        _ when intPred(1, 2) == true => cmp < 0,  // less than
                        _ => throw new Exception("Unknown comparison")
                    });
                }
                throw new Exception("Numeric comparison requires matching types (int, float, or intinf)");
            };
        }
    }
}
