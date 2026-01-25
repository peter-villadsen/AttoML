using System.Collections.Generic;
using System.Linq;

namespace AttoML.Core.Types
{
    public sealed class TypeEnv
    {
        private readonly Dictionary<string, Scheme> _env = new();
        public void Add(string name, Scheme scheme) => _env[name] = scheme;
        public bool TryGet(string name, out Scheme scheme) => _env.TryGetValue(name, out scheme!);
        public IEnumerable<string> Names => _env.Keys;
        public TypeEnv Clone()
        {
            var e = new TypeEnv();
            foreach (var kv in _env)
            {
                e._env[kv.Key] = kv.Value;
            }
            return e;
        }
        public override string ToString() => string.Join(", ", _env.Select(kv => $"{kv.Key}: {kv.Value}"));
    }
}
