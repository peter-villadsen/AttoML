using System;
using System.Collections.Generic;
using System.Linq;

namespace AttoML.Core.Types
{
    /// <summary>
    /// Pretty-prints types with conventional type variable names ('a, 'b, 'c, ...)
    /// instead of internal IDs ('a6769, 'a6770, ...).
    /// </summary>
    public static class TypePrinter
    {
        /// <summary>
        /// Print a type with pretty type variable names.
        /// Type variables are named in order of first appearance: 'a, 'b, 'c, ...
        /// </summary>
        public static string Print(Type type)
        {
            if (type == null) return "<unknown>";

            // Collect all TVars in order of appearance
            var vars = CollectTypeVars(type);

            // Create mapping: TVar.Id -> pretty name ('a, 'b, 'c, ...)
            var nameMap = new Dictionary<int, string>();
            for (int i = 0; i < vars.Count; i++)
            {
                nameMap[vars[i].Id] = GenerateName(i);
            }

            // Print type using the name map
            return PrintType(type, nameMap);
        }

        /// <summary>
        /// Print a type scheme with pretty quantified variable names.
        /// </summary>
        public static string Print(Scheme scheme)
        {
            if (scheme.Quantified.Count == 0)
                return Print(scheme.Type);

            // Create name map for quantified variables first (they should get 'a, 'b, 'c, ...)
            var nameMap = new Dictionary<int, string>();
            for (int i = 0; i < scheme.Quantified.Count; i++)
            {
                nameMap[scheme.Quantified[i].Id] = GenerateName(i);
            }

            // Collect any additional free variables in the type body
            var additionalVars = CollectTypeVars(scheme.Type)
                .Where(v => !nameMap.ContainsKey(v.Id))
                .ToList();

            for (int i = 0; i < additionalVars.Count; i++)
            {
                nameMap[additionalVars[i].Id] = GenerateName(scheme.Quantified.Count + i);
            }

            var quantNames = string.Join(" ", scheme.Quantified.Select(v => nameMap[v.Id]));
            return $"forall {quantNames}. {PrintType(scheme.Type, nameMap)}";
        }

        /// <summary>
        /// Generate a pretty type variable name from an index.
        /// 0 -> 'a, 1 -> 'b, ..., 25 -> 'z, 26 -> 'aa, 27 -> 'ab, ...
        /// </summary>
        public static string GenerateName(int index)
        {
            if (index < 0) return "'?";

            if (index < 26)
                return $"'{(char)('a' + index)}";

            // For index >= 26: 'aa, 'ab, 'ac, ..., 'az, 'ba, 'bb, ...
            int first = (index / 26) - 1;
            int second = index % 26;
            return $"'{(char)('a' + first)}{(char)('a' + second)}";
        }

        /// <summary>
        /// Collect all type variables in a type in order of first appearance.
        /// </summary>
        private static List<TVar> CollectTypeVars(Type t)
        {
            var seen = new HashSet<int>();
            var result = new List<TVar>();
            CollectRec(t, seen, result);
            return result;
        }

        private static void CollectRec(Type t, HashSet<int> seen, List<TVar> result)
        {
            switch (t)
            {
                case TVar v:
                    if (!seen.Contains(v.Id))
                    {
                        seen.Add(v.Id);
                        result.Add(v);
                    }
                    break;

                case TFun f:
                    CollectRec(f.From, seen, result);
                    CollectRec(f.To, seen, result);
                    break;

                case TTuple tt:
                    foreach (var item in tt.Items)
                        CollectRec(item, seen, result);
                    break;

                case TList tl:
                    CollectRec(tl.Elem, seen, result);
                    break;

                case TSet ts:
                    CollectRec(ts.Elem, seen, result);
                    break;

                case TMap tm:
                    CollectRec(tm.Key, seen, result);
                    CollectRec(tm.Value, seen, result);
                    break;

                case TRecord tr:
                    foreach (var kv in tr.Fields.Values)
                        CollectRec(kv, seen, result);
                    break;

                case TAdt ta:
                    foreach (var arg in ta.TypeArgs)
                        CollectRec(arg, seen, result);
                    break;
            }
        }

        /// <summary>
        /// Print a type using the given name map for type variables.
        /// </summary>
        public static string PrintType(Type t, Dictionary<int, string> nameMap)
        {
            return t switch
            {
                TVar v => nameMap.TryGetValue(v.Id, out var name) ? name : $"'a{v.Id}",
                TConst c => c.Name,
                TFun f => FormatFun(f, nameMap),
                TTuple tt => $"({string.Join(", ", tt.Items.Select(i => PrintType(i, nameMap)))})",
                TList tl => $"[{PrintType(tl.Elem, nameMap)}]",
                TSet ts => $"{PrintType(ts.Elem, nameMap)} set",
                TMap tm => $"({PrintType(tm.Key, nameMap)}, {PrintType(tm.Value, nameMap)}) map",
                TRecord tr => "{" + string.Join(", ", tr.Fields.Select(kv => $"{kv.Key}: {PrintType(kv.Value, nameMap)}")) + "}",
                TAdt ta => FormatAdt(ta, nameMap),
                _ => t.ToString()
            };
        }

        private static string FormatFun(TFun f, Dictionary<int, string> nameMap)
        {
            // Add parentheses around function arguments if they are also functions
            var fromStr = f.From is TFun ? $"({PrintType(f.From, nameMap)})" : PrintType(f.From, nameMap);
            return $"{fromStr} -> {PrintType(f.To, nameMap)}";
        }

        private static string FormatAdt(TAdt ta, Dictionary<int, string> nameMap)
        {
            if (ta.TypeArgs.Count == 0)
                return ta.Name;

            // For single type arg, no parens needed: 'a option
            // For multiple type args, use parens: ('a, 'b) either
            if (ta.TypeArgs.Count == 1)
                return $"{PrintType(ta.TypeArgs[0], nameMap)} {ta.Name}";

            var args = string.Join(", ", ta.TypeArgs.Select(a => PrintType(a, nameMap)));
            return $"({args}) {ta.Name}";
        }
    }
}
