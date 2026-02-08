using System;
using System.Collections.Generic;
using System.Linq;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class ListModule
    {
        public static ModuleVal Build()
        {
            var m = new Dictionary<string, Value>();
            m["append"] = Curry2((a,b) =>
            {
                var la = (ListVal)a; var lb = (ListVal)b;
                return new ListVal(la.Items.Concat(lb.Items).ToList());
            });
            m["map"] = Curry2((f, lst) =>
            {
                var fn = (ClosureVal)f; var l = (ListVal)lst;
                return new ListVal(l.Items.Select(x => fn.Invoke(x)).ToList());
            });
            m["null"] = new ClosureVal(lst => new BoolVal(((ListVal)lst).Items.Count == 0));
            m["exists"] = Curry2((f, lst) =>
            {
                var fn = (ClosureVal)f; var l = (ListVal)lst;
                return new BoolVal(l.Items.Any(x => ((BoolVal)fn.Invoke(x)).Value));
            });
            m["all"] = Curry2((f, lst) =>
            {
                var fn = (ClosureVal)f; var l = (ListVal)lst;
                return new BoolVal(l.Items.All(x => ((BoolVal)fn.Invoke(x)).Value));
            });
            m["foldl"] = Curry3((f, seed, lst) =>
            {
                var fn = (ClosureVal)f; var acc = seed; var l = (ListVal)lst;
                foreach (var x in l.Items)
                {
                    var step = (ClosureVal)fn.Invoke(acc);
                    acc = step.Invoke(x);
                }
                return acc;
            });
            m["foldr"] = Curry3((f, seed, lst) =>
            {
                var fn = (ClosureVal)f; var acc = seed; var l = (ListVal)lst;
                for (int i = l.Items.Count - 1; i >= 0; i--)
                {
                    var x = l.Items[i];
                    var step = (ClosureVal)fn.Invoke(x);
                    acc = step.Invoke(acc);
                }
                return acc;
            });
            m["length"] = new ClosureVal(lst => new IntVal(((ListVal)lst).Items.Count));
            m["filter"] = Curry2((f, lst) =>
            {
                var fn = (ClosureVal)f; var l = (ListVal)lst;
                return new ListVal(l.Items.Where(x => ((BoolVal)fn.Invoke(x)).Value).ToList());
            });
            m["head"] = new ClosureVal(lst =>
            {
                var l = (ListVal)lst; if (l.Items.Count == 0) throw new Exception("head of empty list");
                return l.Items[0];
            });
            m["tail"] = new ClosureVal(lst =>
            {
                var l = (ListVal)lst; if (l.Items.Count == 0) throw new Exception("tail of empty list");
                return new ListVal(l.Items.Skip(1).ToList());
            });
            // hd : 'a list -> 'a (raises Fail "empty list" on empty)
            m["hd"] = new ClosureVal(lst =>
            {
                var l = (ListVal)lst;
                if (l.Items.Count == 0) throw new AttoException(new AdtVal("Fail", new StringVal("empty list")));
                return l.Items[0];
            });
            // tl : 'a list -> 'a list (raises Fail "empty list" on empty)
            m["tl"] = new ClosureVal(lst =>
            {
                var l = (ListVal)lst;
                if (l.Items.Count == 0) throw new AttoException(new AdtVal("Fail", new StringVal("empty list")));
                return new ListVal(l.Items.Skip(1).ToList());
            });
            // cons : 'a -> 'a list -> 'a list (prepends element to list)
            m["cons"] = Curry2((x, lst) =>
            {
                var l = (ListVal)lst;
                var newItems = new List<Value> { x };
                newItems.AddRange(l.Items);
                return new ListVal(newItems);
            });
            return new ModuleVal(m);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a,b)));
        }
        private static Value Curry3(Func<Value, Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => new ClosureVal(c => f(a,b,c))));
        }
    }
}
