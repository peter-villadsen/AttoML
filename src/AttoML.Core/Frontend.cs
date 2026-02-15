using System;
using System.Linq;
using AttoML.Core.Lexer;
using AttoML.Core.Modules;
using AttoML.Core.Parsing;
using AttoML.Core.Types;

namespace AttoML.Core
{
    public sealed class Frontend
    {
        public TypeEnv BaseTypeEnv { get; } = new TypeEnv();

        public Frontend()
        {
            // Preload base types for builtins (qualified names)
            var intT = Types.TConst.Int;
            var boolT = Types.TConst.Bool;
            var floatT = Types.TConst.Float;
            var strT = Types.TConst.String;
            var intInfT = Types.TConst.IntInf;
            var unitT = Types.TConst.Unit;
            var exnT = Types.TConst.Exn;

            Types.Type Fun2(Types.Type a, Types.Type b, Types.Type c) => new Types.TFun(a, new Types.TFun(b, c));
            Types.Type Fun3(Types.Type a, Types.Type b, Types.Type c, Types.Type d) => new Types.TFun(a, new Types.TFun(b, new Types.TFun(c, d)));
            // Base (polymorphic numeric ops; runtime enforces int/float)
            var n1 = new Types.TVar();
            BaseTypeEnv.Add("Base.add", new Types.Scheme(new[] { n1 }, Fun2(n1, n1, n1)));
            var n2 = new Types.TVar();
            BaseTypeEnv.Add("Base.sub", new Types.Scheme(new[] { n2 }, Fun2(n2, n2, n2)));
            var n3 = new Types.TVar();
            BaseTypeEnv.Add("Base.mul", new Types.Scheme(new[] { n3 }, Fun2(n3, n3, n3)));
            var n4 = new Types.TVar();
            BaseTypeEnv.Add("Base.div", new Types.Scheme(new[] { n4 }, Fun2(n4, n4, n4)));
            // Integer division and modulus (monomorphic int)
            BaseTypeEnv.Add("Base.idiv", new Types.Scheme(System.Array.Empty<Types.TVar>(), Fun2(intT, intT, intT)));
            BaseTypeEnv.Add("Base.mod", new Types.Scheme(System.Array.Empty<Types.TVar>(), Fun2(intT, intT, intT)));
            var eqa = new Types.TVar();
            BaseTypeEnv.Add("Base.eq",  new Types.Scheme(new[] { eqa }, Fun2(eqa, eqa, boolT)));
            var lta = new Types.TVar();
            BaseTypeEnv.Add("Base.lt",  new Types.Scheme(new[] { lta }, Fun2(lta, lta, boolT)));
            BaseTypeEnv.Add("Base.and", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(boolT, boolT, boolT)));
            BaseTypeEnv.Add("Base.or",  new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(boolT, boolT, boolT)));
            BaseTypeEnv.Add("Base.not", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(boolT, boolT)));
            // Math
            // Math constants
            BaseTypeEnv.Add("Math.pi", new Types.Scheme(Array.Empty<Types.TVar>(), floatT));

            // Exponential and logarithmic
            BaseTypeEnv.Add("Math.exp", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));
            BaseTypeEnv.Add("Math.log", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));

            // Trigonometric functions
            BaseTypeEnv.Add("Math.sin", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));
            BaseTypeEnv.Add("Math.cos", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));
            BaseTypeEnv.Add("Math.atan", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));
            BaseTypeEnv.Add("Math.atan2", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(floatT, floatT, floatT)));

            // Inverse trigonometric functions
            BaseTypeEnv.Add("Math.asin", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));
            BaseTypeEnv.Add("Math.acos", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));

            // Hyperbolic functions
            BaseTypeEnv.Add("Math.sinh", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));
            BaseTypeEnv.Add("Math.cosh", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));
            BaseTypeEnv.Add("Math.tanh", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));

            // Other functions
            BaseTypeEnv.Add("Math.sqrt", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, floatT)));

            // List module polymorphic functions
            var a = new Types.TVar();
            var b = new Types.TVar();
            BaseTypeEnv.Add("List.append", new Types.Scheme(new[] { a }, Fun2(new Types.TList(a), new Types.TList(a), new Types.TList(a))));
            BaseTypeEnv.Add("List.map", new Types.Scheme(new[] { a, b }, Fun2(new Types.TFun(a, b), new Types.TList(a), new Types.TList(b))));
            BaseTypeEnv.Add("List.null", new Types.Scheme(new[] { a }, new Types.TFun(new Types.TList(a), boolT)));
            BaseTypeEnv.Add("List.exists", new Types.Scheme(new[] { a }, Fun2(new Types.TFun(a, boolT), new Types.TList(a), boolT)));
            BaseTypeEnv.Add("List.all", new Types.Scheme(new[] { a }, Fun2(new Types.TFun(a, boolT), new Types.TList(a), boolT)));
            BaseTypeEnv.Add("List.foldl", new Types.Scheme(new[] { a, b }, Fun3(new Types.TFun(b, new Types.TFun(a, b)), b, new Types.TList(a), b)));
            BaseTypeEnv.Add("List.foldr", new Types.Scheme(new[] { a, b }, Fun3(new Types.TFun(a, new Types.TFun(b, b)), b, new Types.TList(a), b)));
            BaseTypeEnv.Add("List.length", new Types.Scheme(new[] { a }, new Types.TFun(new Types.TList(a), intT)));
            BaseTypeEnv.Add("List.filter", new Types.Scheme(new[] { a }, Fun2(new Types.TFun(a, boolT), new Types.TList(a), new Types.TList(a))));
            BaseTypeEnv.Add("List.head", new Types.Scheme(new[] { a }, new Types.TFun(new Types.TList(a), a)));
            BaseTypeEnv.Add("List.tail", new Types.Scheme(new[] { a }, new Types.TFun(new Types.TList(a), new Types.TList(a))));
            BaseTypeEnv.Add("List.hd", new Types.Scheme(new[] { a }, new Types.TFun(new Types.TList(a), a)));
            BaseTypeEnv.Add("List.tl", new Types.Scheme(new[] { a }, new Types.TFun(new Types.TList(a), new Types.TList(a))));
            BaseTypeEnv.Add("List.cons", new Types.Scheme(new[] { a }, Fun2(a, new Types.TList(a), new Types.TList(a))));

            // String module
            BaseTypeEnv.Add("String.concat", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TFun(strT, strT))));
            BaseTypeEnv.Add("String.size", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, intT)));
            BaseTypeEnv.Add("String.length", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, intT)));
            BaseTypeEnv.Add("String.sub", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TFun(intT, strT))));
            BaseTypeEnv.Add("String.substring", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TFun(intT, new Types.TFun(intT, strT)))));
            BaseTypeEnv.Add("String.explode", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TList(strT))));
            BaseTypeEnv.Add("String.implode", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(new Types.TList(strT), strT)));
            BaseTypeEnv.Add("String.concatList", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(new Types.TList(strT), strT)));
            BaseTypeEnv.Add("String.isPrefix", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TFun(strT, boolT))));
            BaseTypeEnv.Add("String.isSuffix", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TFun(strT, boolT))));
            BaseTypeEnv.Add("String.contains", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TFun(strT, boolT))));
            BaseTypeEnv.Add("String.translate", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(new Types.TFun(strT, strT), new Types.TFun(strT, strT))));
            BaseTypeEnv.Add("String.compare", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TFun(strT, intT))));
            BaseTypeEnv.Add("String.equalsIgnoreCase", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, new Types.TFun(strT, boolT))));
            BaseTypeEnv.Add("String.ofInt", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(intT, strT)));
            BaseTypeEnv.Add("String.ofFloat", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(floatT, strT)));
            BaseTypeEnv.Add("String.toInt", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, intT)));
            BaseTypeEnv.Add("String.toFloat", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, floatT)));

            // IntInf module (arbitrary precision integers)
            BaseTypeEnv.Add("IntInf.fromInt", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(intT, intInfT)));
            BaseTypeEnv.Add("IntInf.toInt", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(intInfT, intT)));
            BaseTypeEnv.Add("IntInf.toString", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(intInfT, strT)));
            // fromString : string -> intinf option
            var optionIntInf = new Types.TAdt("option", new[] { intInfT });
            BaseTypeEnv.Add("IntInf.fromString", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, optionIntInf)));
            BaseTypeEnv.Add("IntInf.add", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intInfT, intInfT)));
            BaseTypeEnv.Add("IntInf.sub", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intInfT, intInfT)));
            BaseTypeEnv.Add("IntInf.mul", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intInfT, intInfT)));
            BaseTypeEnv.Add("IntInf.div", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intInfT, intInfT)));
            BaseTypeEnv.Add("IntInf.mod", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intInfT, intInfT)));
            BaseTypeEnv.Add("IntInf.neg", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(intInfT, intInfT)));
            BaseTypeEnv.Add("IntInf.abs", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(intInfT, intInfT)));
            // compare : intinf -> intinf -> order (order is an ADT: LESS | EQUAL | GREATER)
            var orderT = new Types.TAdt("order");
            BaseTypeEnv.Add("IntInf.compare", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intInfT, orderT)));
            BaseTypeEnv.Add("IntInf.min", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intInfT, intInfT)));
            BaseTypeEnv.Add("IntInf.max", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intInfT, intInfT)));
            BaseTypeEnv.Add("IntInf.pow", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(intInfT, intT, intInfT)));
            BaseTypeEnv.Add("IntInf.sign", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(intInfT, intT)));

            // Tuple module
            var t1 = new Types.TVar();
            var t2 = new Types.TVar();
            var t3 = new Types.TVar();
            var t4 = new Types.TVar();
            var t5 = new Types.TVar();
            var t6 = new Types.TVar();
            // fst : ('a * 'b) -> 'a
            BaseTypeEnv.Add("Tuple.fst", new Types.Scheme(new[] { t1, t2 }, new Types.TFun(new Types.TTuple(new[] { t1, t2 }), t1)));
            // snd : ('a * 'b) -> 'b
            BaseTypeEnv.Add("Tuple.snd", new Types.Scheme(new[] { t1, t2 }, new Types.TFun(new Types.TTuple(new[] { t1, t2 }), t2)));
            // swap : ('a * 'b) -> ('b * 'a)
            BaseTypeEnv.Add("Tuple.swap", new Types.Scheme(new[] { t1, t2 }, new Types.TFun(new Types.TTuple(new[] { t1, t2 }), new Types.TTuple(new[] { t2, t1 }))));
            // curry : (('a * 'b) -> 'c) -> 'a -> 'b -> 'c
            BaseTypeEnv.Add("Tuple.curry", new Types.Scheme(new[] { t1, t2, t3 }, new Types.TFun(new Types.TFun(new Types.TTuple(new[] { t1, t2 }), t3), new Types.TFun(t1, new Types.TFun(t2, t3)))));
            // uncurry : ('a -> 'b -> 'c) -> ('a * 'b) -> 'c
            BaseTypeEnv.Add("Tuple.uncurry", new Types.Scheme(new[] { t1, t2, t3 }, new Types.TFun(new Types.TFun(t1, new Types.TFun(t2, t3)), new Types.TFun(new Types.TTuple(new[] { t1, t2 }), t3))));
            // fst3 : ('a * 'b * 'c) -> 'a
            BaseTypeEnv.Add("Tuple.fst3", new Types.Scheme(new[] { t4, t5, t6 }, new Types.TFun(new Types.TTuple(new[] { t4, t5, t6 }), t4)));
            // snd3 : ('a * 'b * 'c) -> 'b
            BaseTypeEnv.Add("Tuple.snd3", new Types.Scheme(new[] { t4, t5, t6 }, new Types.TFun(new Types.TTuple(new[] { t4, t5, t6 }), t5)));
            // thd3 : ('a * 'b * 'c) -> 'c
            BaseTypeEnv.Add("Tuple.thd3", new Types.Scheme(new[] { t4, t5, t6 }, new Types.TFun(new Types.TTuple(new[] { t4, t5, t6 }), t6)));

            // Set module (operates on generic 'a sets)
            var tSetElem = new Types.TVar();
            var setT = new Types.TSet(tSetElem);
            BaseTypeEnv.Add("Set.empty", new Types.Scheme(new[] { tSetElem }, setT));
            BaseTypeEnv.Add("Set.singleton", new Types.Scheme(new[] { tSetElem }, new Types.TFun(tSetElem, setT)));
            BaseTypeEnv.Add("Set.add", new Types.Scheme(new[] { tSetElem }, Fun2(tSetElem, setT, setT)));
            BaseTypeEnv.Add("Set.remove", new Types.Scheme(new[] { tSetElem }, Fun2(tSetElem, setT, setT)));
            BaseTypeEnv.Add("Set.contains", new Types.Scheme(new[] { tSetElem }, Fun2(tSetElem, setT, boolT)));
            BaseTypeEnv.Add("Set.size", new Types.Scheme(new[] { tSetElem }, new Types.TFun(setT, intT)));
            BaseTypeEnv.Add("Set.isEmpty", new Types.Scheme(new[] { tSetElem }, new Types.TFun(setT, boolT)));
            BaseTypeEnv.Add("Set.union", new Types.Scheme(new[] { tSetElem }, Fun2(setT, setT, setT)));
            BaseTypeEnv.Add("Set.intersect", new Types.Scheme(new[] { tSetElem }, Fun2(setT, setT, setT)));
            BaseTypeEnv.Add("Set.diff", new Types.Scheme(new[] { tSetElem }, Fun2(setT, setT, setT)));
            BaseTypeEnv.Add("Set.isSubset", new Types.Scheme(new[] { tSetElem }, Fun2(setT, setT, boolT)));
            BaseTypeEnv.Add("Set.toList", new Types.Scheme(new[] { tSetElem }, new Types.TFun(setT, new Types.TList(tSetElem))));
            BaseTypeEnv.Add("Set.fromList", new Types.Scheme(new[] { tSetElem }, new Types.TFun(new Types.TList(tSetElem), setT)));

            // MapImplementation module (low-level, internal)
            var tMapKey = new Types.TVar();
            var tMapVal = new Types.TVar();
            var mapT = new Types.TMap(tMapKey, tMapVal);
            BaseTypeEnv.Add("MapImplementation.empty", new Types.Scheme(new[] { tMapKey, tMapVal }, mapT));
            BaseTypeEnv.Add("MapImplementation.singleton", new Types.Scheme(new[] { tMapKey, tMapVal }, Fun2(tMapKey, tMapVal, mapT)));
            BaseTypeEnv.Add("MapImplementation.add", new Types.Scheme(new[] { tMapKey, tMapVal }, Fun3(tMapKey, tMapVal, mapT, mapT)));
            BaseTypeEnv.Add("MapImplementation.remove", new Types.Scheme(new[] { tMapKey, tMapVal }, Fun2(tMapKey, mapT, mapT)));
            // MapImplementation.get returns 'v list: [] = not found, [value] = found
            BaseTypeEnv.Add("MapImplementation.get", new Types.Scheme(new[] { tMapKey, tMapVal }, Fun2(tMapKey, mapT, new Types.TList(tMapVal))));
            BaseTypeEnv.Add("MapImplementation.contains", new Types.Scheme(new[] { tMapKey, tMapVal }, Fun2(tMapKey, mapT, boolT)));
            BaseTypeEnv.Add("MapImplementation.size", new Types.Scheme(new[] { tMapKey, tMapVal }, new Types.TFun(mapT, intT)));
            BaseTypeEnv.Add("MapImplementation.isEmpty", new Types.Scheme(new[] { tMapKey, tMapVal }, new Types.TFun(mapT, boolT)));
            BaseTypeEnv.Add("MapImplementation.keys", new Types.Scheme(new[] { tMapKey, tMapVal }, new Types.TFun(mapT, new Types.TList(tMapKey))));
            BaseTypeEnv.Add("MapImplementation.values", new Types.Scheme(new[] { tMapKey, tMapVal }, new Types.TFun(mapT, new Types.TList(tMapVal))));
            var kvPairT = new Types.TTuple(new[] { tMapKey, tMapVal });
            BaseTypeEnv.Add("MapImplementation.toList", new Types.Scheme(new[] { tMapKey, tMapVal }, new Types.TFun(mapT, new Types.TList(kvPairT))));
            BaseTypeEnv.Add("MapImplementation.fromList", new Types.Scheme(new[] { tMapKey, tMapVal }, new Types.TFun(new Types.TList(kvPairT), mapT)));
            var tMapVal2 = new Types.TVar();
            var mapT2 = new Types.TMap(tMapKey, tMapVal2);
            BaseTypeEnv.Add("MapImplementation.mapValues", new Types.Scheme(new[] { tMapKey, tMapVal, tMapVal2 }, Fun2(new Types.TFun(tMapVal, tMapVal2), mapT, mapT2)));
            var t7 = new Types.TVar();
            BaseTypeEnv.Add("MapImplementation.fold", new Types.Scheme(new[] { tMapKey, tMapVal, t7 }, Fun3(new Types.TFun(tMapKey, new Types.TFun(tMapVal, new Types.TFun(t7, t7))), t7, mapT, t7)));

            // Map module (high-level wrapper, defined in Prelude/Map.atto)
            // This will be populated when the Prelude loads, but we add signature for
            // Map.get which returns proper option type
            var tGetOption = new Types.TAdt("option", new[] { tMapVal });
            BaseTypeEnv.Add("Map.get", new Types.Scheme(new[] { tMapKey, tMapVal }, Fun2(tMapKey, mapT, tGetOption)));

            // TextIOImplementation module (low-level, internal)
            // Stream types are opaque (represented as special values at runtime)
            var instreamT = new Types.TConst("instream");
            var outstreamT = new Types.TConst("outstream");
            BaseTypeEnv.Add("TextIOImplementation.stdIn", new Types.Scheme(Array.Empty<Types.TVar>(), instreamT));
            BaseTypeEnv.Add("TextIOImplementation.stdOut", new Types.Scheme(Array.Empty<Types.TVar>(), outstreamT));
            BaseTypeEnv.Add("TextIOImplementation.stdErr", new Types.Scheme(Array.Empty<Types.TVar>(), outstreamT));
            BaseTypeEnv.Add("TextIOImplementation.print", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, unitT)));
            BaseTypeEnv.Add("TextIOImplementation.openIn", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, instreamT)));
            BaseTypeEnv.Add("TextIOImplementation.openOut", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, outstreamT)));
            BaseTypeEnv.Add("TextIOImplementation.openAppend", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, outstreamT)));
            BaseTypeEnv.Add("TextIOImplementation.closeIn", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(instreamT, unitT)));
            BaseTypeEnv.Add("TextIOImplementation.closeOut", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(outstreamT, unitT)));
            // inputLine returns string list: [] = EOF, [line] = data
            BaseTypeEnv.Add("TextIOImplementation.inputLine", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(instreamT, new Types.TList(strT))));
            BaseTypeEnv.Add("TextIOImplementation.input", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(instreamT, strT)));
            BaseTypeEnv.Add("TextIOImplementation.output", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(outstreamT, strT, unitT)));
            BaseTypeEnv.Add("TextIOImplementation.flushOut", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(outstreamT, unitT)));

            // TextIO module (high-level wrapper, defined in Prelude/TextIO.atto)
            // This will be populated when the Prelude loads, but we add signatures for the
            // functions that differ from the implementation (e.g., inputLine returns option)
            var strOptionT = new Types.TAdt("option", new[] { strT });
            BaseTypeEnv.Add("TextIO.inputLine", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(instreamT, strOptionT)));

            // HTTP module
            BaseTypeEnv.Add("Http.get", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, strT)));
            BaseTypeEnv.Add("Http.post", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(strT, strT, strT)));
            BaseTypeEnv.Add("Http.postJson", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(strT, strT, strT)));
            // getWithHeaders takes (string * string) list as headers
            var strPairT = new Types.TTuple(new[] { strT, strT });
            BaseTypeEnv.Add("Http.getWithHeaders", new Types.Scheme(Array.Empty<Types.TVar>(), Fun2(strT, new Types.TList(strPairT), strT)));

            // Do not pre-register module-specific functions like SymCalc.* in the base env.
            // Their types are inferred from the prelude modules during compilation.

            // Built-in exception constructors
            // Fail : string -> exn
            BaseTypeEnv.Add("Fail", new Types.Scheme(Array.Empty<Types.TVar>(), new Types.TFun(strT, exnT)));
            // Div : exn; Domain : exn
            BaseTypeEnv.Add("Div", new Types.Scheme(Array.Empty<Types.TVar>(), exnT));
            BaseTypeEnv.Add("Domain", new Types.Scheme(Array.Empty<Types.TVar>(), exnT));

            // Unqualified list functions available as well
            var a2 = new Types.TVar();
            var b2 = new Types.TVar();
            BaseTypeEnv.Add("append", new Types.Scheme(new[] { a2 }, Fun2(new Types.TList(a2), new Types.TList(a2), new Types.TList(a2))));
            BaseTypeEnv.Add("map", new Types.Scheme(new[] { a2, b2 }, Fun2(new Types.TFun(a2, b2), new Types.TList(a2), new Types.TList(b2))));
            BaseTypeEnv.Add("null", new Types.Scheme(new[] { a2 }, new Types.TFun(new Types.TList(a2), boolT)));
            BaseTypeEnv.Add("exists", new Types.Scheme(new[] { a2 }, Fun2(new Types.TFun(a2, boolT), new Types.TList(a2), boolT)));
            BaseTypeEnv.Add("all", new Types.Scheme(new[] { a2 }, Fun2(new Types.TFun(a2, boolT), new Types.TList(a2), boolT)));
            BaseTypeEnv.Add("foldl", new Types.Scheme(new[] { a2, b2 }, Fun3(new Types.TFun(b2, new Types.TFun(a2, b2)), b2, new Types.TList(a2), b2)));
            BaseTypeEnv.Add("foldr", new Types.Scheme(new[] { a2, b2 }, Fun3(new Types.TFun(a2, new Types.TFun(b2, b2)), b2, new Types.TList(a2), b2)));
            BaseTypeEnv.Add("length", new Types.Scheme(new[] { a2 }, new Types.TFun(new Types.TList(a2), intT)));
            BaseTypeEnv.Add("filter", new Types.Scheme(new[] { a2 }, Fun2(new Types.TFun(a2, boolT), new Types.TList(a2), new Types.TList(a2))));
            BaseTypeEnv.Add("head", new Types.Scheme(new[] { a2 }, new Types.TFun(new Types.TList(a2), a2)));
            BaseTypeEnv.Add("tail", new Types.Scheme(new[] { a2 }, new Types.TFun(new Types.TList(a2), new Types.TList(a2))));
            BaseTypeEnv.Add("hd", new Types.Scheme(new[] { a2 }, new Types.TFun(new Types.TList(a2), a2)));
            BaseTypeEnv.Add("tl", new Types.Scheme(new[] { a2 }, new Types.TFun(new Types.TList(a2), new Types.TList(a2))));
        }

        public (System.Collections.Generic.List<Parsing.ModuleDecl> decls, ModuleSystem modules, Expr? expr, Types.Type? exprType) Compile(string source)
        {
            var lex = new Lexer.Lexer(source);
            var tokens = lex.Lex().ToList();
            var parser = new Parser(tokens);
            var (decls, expr) = parser.ParseCompilationUnit();
            var ti = new TypeInference();
            var modules = new ModuleSystem();
            modules.LoadDecls(decls);
            // Persist ADT constructors into the base type environment for subsequent compilations (REPL inputs)
            foreach (var adt in modules.Adts.Values)
            {
                foreach (var (ctor, payload) in adt.Ctors)
                {
                    Types.Type ctorType = payload == null ? new Types.TAdt(adt.TypeName) : new Types.TFun(payload, new Types.TAdt(adt.TypeName));
                    // Unqualified constructor
                    BaseTypeEnv.Add(ctor, new Types.Scheme(Array.Empty<Types.TVar>(), ctorType));
                    // Also add qualified by type name to allow disambiguation (e.g., Expr.Div)
                    BaseTypeEnv.Add($"{adt.TypeName}.{ctor}", new Types.Scheme(Array.Empty<Types.TVar>(), ctorType));
                }
            }
            // Persist exception constructors (exn) into base env
            foreach (var kv in modules.Exceptions)
            {
                var name = kv.Key;
                var payload = kv.Value;
                var exnT2 = Types.TConst.Exn;
                Types.Type ctorType = payload == null ? exnT2 : new Types.TFun(payload, exnT2);
                BaseTypeEnv.Add(name, new Types.Scheme(Array.Empty<Types.TVar>(), ctorType));
            }
            var tenv = modules.InjectStructuresInto(BaseTypeEnv, ti, decls);

            Types.Type? et = null;
            if (expr != null)
            {
                var (subst, t) = ti.Infer(tenv, expr);
                et = subst.Apply(t);
            }
            return (decls, modules, expr, et);
        }

        // Infer types for top-level val declarations and persist them in the frontend's base type environment
        public System.Collections.Generic.List<(string Name, Types.Type Type)> InferTopVals(ModuleSystem modules, System.Collections.Generic.List<Parsing.ModuleDecl> decls)
        {
            var ti = new TypeInference();
            var tenv = modules.InjectStructuresInto(BaseTypeEnv, ti, decls);
            var results = new System.Collections.Generic.List<(string, Types.Type)>();
            foreach (var d in decls)
            {
                if (d is Parsing.ValDecl vd)
                {
                    var (subst, t) = ti.Infer(tenv, vd.Expr);
                    var ty = subst.Apply(t);
                    if (vd.TypeAnn != null)
                    {
                        var annTy = Modules.ModuleSystem.TypeFromTypeExpr(vd.TypeAnn);
                        // Assert annotated type matches inferred
                        ti.AssertUnify(ty, annTy);
                        ty = annTy;
                    }
                    // Add to both current env and persistent base env for future lines
                    tenv.Add(vd.Name, new Types.Scheme(System.Array.Empty<Types.TVar>(), ty));
                    BaseTypeEnv.Add(vd.Name, new Types.Scheme(System.Array.Empty<Types.TVar>(), ty));
                    results.Add((vd.Name, ty));
                }
            }
            if (results.Count > 0)
            {
                var lastTy = results[^1].Item2;
                BaseTypeEnv.Add("it", new Types.Scheme(System.Array.Empty<Types.TVar>(), lastTy));
            }
            return results;
        }
    }
}
