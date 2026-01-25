using System.Collections.Generic;

namespace AttoML.Interpreter.Runtime
{
    public sealed class Env
    {
        private readonly Dictionary<string, Value> _map = new();
        public void Set(string name, Value val) => _map[name] = val;
        public bool TryGet(string name, out Value val) => _map.TryGetValue(name, out val!);
        public Env Clone()
        {
            var e = new Env();
            foreach (var kv in _map)
            {
                e._map[kv.Key] = kv.Value;
            }
            return e;
        }
    }
}
