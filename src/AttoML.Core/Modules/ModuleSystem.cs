using AttoML.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using AttoML.Core.Parsing;
using AttoML.Core.Types;
using TypeT = AttoML.Core.Types.Type;

namespace AttoML.Core.Modules
{
    public sealed class SignatureInfo
    {
        public string Name { get; }
        public IReadOnlyDictionary<string, TypeT?> Members { get; }
        public SignatureInfo(string name, IReadOnlyDictionary<string, TypeT?> members){ Name=name; Members=members; }
    }

    public sealed class StructureInfo
    {
        public string Name { get; }
        // Preserve declared order of bindings for intra-structure references
        public IReadOnlyList<(string Name, Expr Expr, Parsing.TypeExpr? TypeAnn)> OrderedBindings { get; }
        // Fast lookup by name for signature checks
        public IReadOnlyDictionary<string, (Expr Expr, Parsing.TypeExpr? TypeAnn)> BindingsByName { get; }
        public string? SigName { get; }
        public StructureInfo(string name, IReadOnlyList<(string, Expr, Parsing.TypeExpr?)> orderedBindings, string? sigName)
        {
            Name = name;
            OrderedBindings = orderedBindings;
            BindingsByName = orderedBindings.ToDictionary(x => x.Item1, x => (x.Item2, x.Item3));
            SigName = sigName;
        }
    }

    public sealed class ModuleSystem
    {
        public Dictionary<string, SignatureInfo> Signatures { get; } = new();
        public Dictionary<string, StructureInfo> Structures { get; } = new();
        public Dictionary<string, (string TypeName, List<TVar> TypeParams, List<(string Ctor, TypeT? Payload)> Ctors)> Adts { get; } = new();
        public Dictionary<string, TypeT?> Exceptions { get; } = new(); // name -> payload type (null for none)

        public void LoadDecls(IEnumerable<ModuleDecl> decls)
        {
            foreach (var d in decls)
            {
                switch (d)
                {
                    case SignatureDecl sd:
                        var mem = new Dictionary<string, TypeT?>();
                        foreach (var v in sd.Vals)
                        {
                            mem[v.Name] = v.Type == null ? null : TypeFromTypeExpr(v.Type);
                        }
                        Signatures[sd.Name] = new SignatureInfo(sd.Name, mem);
                        break;
                    case StructureDecl st:
                        var ord = st.Bindings.Select(x => (x.Name, x.Expr, x.TypeAnn)).ToList();
                        Structures[st.Name] = new StructureInfo(st.Name, ord, st.SigName);
                        break;
                    case AttoML.Core.Parsing.TypeDecl td:
                        // Create type parameter mapping for this ADT
                        var typeParams = new List<TVar>();
                        var typeParamMap = new Dictionary<string, TVar>();
                        foreach (var tpName in td.TypeParams)
                        {
                            var tvar = new TVar();
                            typeParams.Add(tvar);
                            typeParamMap[tpName] = tvar;
                        }

                        var ctors = new List<(string, TypeT?)>();
                        foreach (var c in td.Ctors)
                        {
                            if (c.PayloadType != null)
                            {
                                var payloadT = TypeFromTypeExpr(c.PayloadType, typeParamMap);
                                ctors.Add((c.Name, payloadT));
                            }
                            else
                            {
                                ctors.Add((c.Name, null));
                            }
                        }
                        Adts[td.Name] = (td.Name, typeParams, ctors);
                        break;
                    case ExceptionDecl ed:
                        Exceptions[ed.Name] = ed.PayloadType == null ? null : TypeFromTypeExpr(ed.PayloadType);
                        break;
                    case OpenDecl:
                        // open is processed in the frontend by injecting qualified names
                        break;
                }
            }
        }

        public TypeEnv InjectStructuresInto(TypeEnv env, TypeInference ti, System.Collections.Generic.IEnumerable<Parsing.ModuleDecl>? declsForOpen = null)
        {
            var e = env.Clone();
            // Collect structures requested to be opened
            var opened = new HashSet<string>();
            if (declsForOpen != null)
            {
                foreach (var d in declsForOpen)
                {
                    if (d is Parsing.OpenDecl od)
                    {
                        opened.Add(od.Name);
                    }
                }
            }

            // CRITICAL FIX: Inject ADT constructors BEFORE processing structures
            // so that structure bindings can reference constructors like Some/None
            foreach (var adt in Adts.Values)
            {
                // Build parametric ADT type: option<'a>, either<'a,'b>, etc.
                var adtType = new TAdt(adt.TypeName, adt.TypeParams);

                foreach (var (ctor, payload) in adt.Ctors)
                {
                    // Constructor type: payload -> ADT or just ADT
                    TypeT ctorType = payload == null ? adtType : new TFun(payload, adtType);

                    // Quantify over type parameters to make polymorphic
                    // e.g., SOME : forall 'a. 'a -> 'a option
                    //       None : forall 'a. 'a option
                    var scheme = new Scheme(adt.TypeParams, ctorType);

                    e.Add(ctor, scheme);
                }
            }

            // Now process structures with ADT constructors available
            foreach (var s in Structures.Values)
            {
                // Check signature if present
                if (s.SigName != null && Signatures.TryGetValue(s.SigName, out var sig))
                {
                    foreach (var kv in sig.Members)
                    {
                        if (!s.BindingsByName.ContainsKey(kv.Key))
                            throw new ModuleException($"Structure {s.Name} missing member {kv.Key} required by signature {sig.Name}");
                    }
                }
                // Intra-structure environment allowing previously bound names to be referenced unqualified
                var eLocal = e.Clone();
                // Predeclare stubs for all names to permit forward references and mutual recursion
                // If a type annotation exists, use it for the stub to guide inference across bindings.
                foreach (var (bn, _, bann) in s.OrderedBindings)
                {
                    TypeT stubTy;
                    if (bann != null)
                    {
                        stubTy = TypeFromTypeExpr(bann);
                    }
                    else
                    {
                        stubTy = new TVar();
                    }
                    var stubScheme = new Scheme(Array.Empty<TVar>(), stubTy);
                    eLocal.Add(bn, stubScheme);
                }
                // Second pass: infer each binding with stubs available
                foreach (var (bn, bexpr, bann) in s.OrderedBindings)
                {
                    // Don't convert to LetRec - the stubbing mechanism already enables mutual recursion!
                    // Converting to LetRec shadows the stub with a fresh var, breaking mutual recursion.

                    // If there's an annotation and it's a Fun, we need special handling:
                    // Manually infer with parameter type from annotation to avoid Fun case creating fresh vars
                    if (bann != null && bexpr is AttoML.Core.Parsing.Fun f)
                    {
                        var annTy = TypeFromTypeExpr(bann);
                        // Update stub with annotation so recursive calls work correctly
                        eLocal.Add(bn, new Scheme(Array.Empty<TVar>(), annTy));

                        // Extract parameter type from annotation
                        if (annTy is TFun tf)
                        {
                            // DEBUG: Print annotation structure
                            if (tf.From is TTuple tt)
                            {
                                for (int i = 0; i < tt.Items.Count; i++)
                                {
                                }
                            }

                            // Infer the function body directly, bypassing the Fun case in TypeInference
                            // This ensures the parameter gets the annotated type, not a fresh var
                            var envWithParam = eLocal.Clone();
                            // Add parameter with the type from annotation - this is key!
                            envWithParam.Add(f.Param, new Scheme(Array.Empty<TVar>(), tf.From));

                            Types.Subst substFun;
                            TypeT bodyTy;
                            try
                            {
                                foreach (var name in envWithParam.Names)
                                {
                                    if (envWithParam.TryGet(name, out var schemeDebug))
                                    {
                                    }
                                }

                                // Infer the function body with parameter type constrained
                                (substFun, bodyTy) = ti.Infer(envWithParam, f.Body);

                            }
                            catch (Exception ex)
                            {
                                throw new ModuleException($"Error inferring binding {s.Name}.{bn}: {ex.Message}");
                            }

                            // Verify body type matches annotation's return type
                            try
                            {
                                ti.AssertUnify(substFun.Apply(bodyTy), substFun.Apply(tf.To));
                            }
                            catch (Exception ex)
                            {
                                throw new ModuleException($"Function body type {substFun.Apply(bodyTy)} doesn't match annotated return type {tf.To}: {ex.Message}");
                            }

                            // Use the annotation as the final type
                            var tyFun = annTy;
                            var schemeFun = new Scheme(Array.Empty<TVar>(), tyFun);
                            e.Add($"{s.Name}.{bn}", schemeFun);
                            eLocal.Add(bn, schemeFun);
                            if (opened.Contains(s.Name))
                            {
                                e.Add(bn, schemeFun);
                            }
                            continue;  // Skip normal inference path
                        }
                    }

                    AttoML.Core.Parsing.Expr inferExpr = bexpr;
                    Types.Subst subst;
                    TypeT t;
                    try
                    {
                        (subst, t) = ti.Infer(eLocal, inferExpr);
                    }
                    catch (Exception ex)
                    {
                        throw new ModuleException($"Error inferring binding {s.Name}.{bn}: {ex.Message}");
                    }
                    var ty = subst.Apply(t);
                    if (bann != null)
                    {
                        var annTy = TypeFromTypeExpr(bann);
                        // enforce annotation
                        var sBind = tiUnifyHelper(ti, ty, annTy);
                        ty = annTy;
                    }
                    var scheme = new Scheme(Array.Empty<TVar>(), ty);
                    // Qualified injection for external use
                    e.Add($"{s.Name}.{bn}", scheme);
                    // Unqualified for subsequent bindings inside the same structure
                    eLocal.Add(bn, scheme);
                    // If structure is opened, also inject unqualified into global env
                    if (opened.Contains(s.Name))
                    {
                        e.Add(bn, scheme);
                    }
                }
            }
            // Inject Exception constructors (result type exn)
            foreach (var kv in Exceptions)
            {
                var name = kv.Key;
                var payload = kv.Value;
                TypeT ctorType = payload == null ? TConst.Exn : new TFun(payload, TConst.Exn);
                var scheme = new Scheme(Array.Empty<TVar>(), ctorType);
                e.Add(name, scheme);
            }
            // Handle 'open' for modules that are not defined as structures by injecting unqualified aliases
            foreach (var modName in opened)
            {
                if (!Structures.ContainsKey(modName))
                {
                    var prefix = modName + ".";
                    foreach (var qn in e.Names.ToList())
                    {
                        if (qn.StartsWith(prefix, StringComparison.Ordinal))
                        {
                            var member = qn.Substring(prefix.Length);
                            if (e.TryGet(qn, out var sch))
                            {
                                e.Add(member, sch);
                            }
                        }
                    }
                }
            }
            return e;
        }

        public static TypeT TypeFromTypeExpr(TypeExpr te, Dictionary<string, TVar>? typeParamMap = null)
        {
            return te switch
            {
                TypeName tn => tn.Name switch
                {
                    "int" => TConst.Int,
                    "bool" => TConst.Bool,
                    "float" => TConst.Float,
                    "string" => TConst.String,
                    "intinf" => TConst.IntInf,
                    "unit" => TConst.Unit,
                    "exn" => TConst.Exn,
                    // Treat any other name as an ADT reference (supports recursive payloads like Expr)
                    _ => new TAdt(tn.Name)
                },
                TypeVar tv => typeParamMap != null && typeParamMap.TryGetValue(tv.Name, out var tvar)
                    ? tvar
                    : throw new ModuleException($"Unbound type variable '{tv.Name}"),
                TypeArrow ta => new TFun(TypeFromTypeExpr(ta.From, typeParamMap), TypeFromTypeExpr(ta.To, typeParamMap)),
                TypeTuple tt => new TTuple(tt.Items.Select(t => TypeFromTypeExpr(t, typeParamMap)).ToList()),
                TypeApp tapp => tapp.Constructor switch
                {
                    // Special handling for built-in list type (backward compatibility)
                    "list" => new TList(TypeFromTypeExpr(tapp.Base, typeParamMap)),
                    // All other type constructors are treated as parametric ADTs
                    // This includes: option, result, map, set, and user-defined types
                    _ => new TAdt(tapp.Constructor, new[] { TypeFromTypeExpr(tapp.Base, typeParamMap) })
                },
                _ => throw new ModuleException("Unknown type expr")
            };
        }

        private static Types.Subst tiUnifyHelper(TypeInference ti, TypeT a, TypeT b)
        {
            // Use TypeInference's private Unify via a small indirection: expose AssertUnify which calls Unify.
            ti.AssertUnify(a, b);
            return new Types.Subst();
        }
    }
}
