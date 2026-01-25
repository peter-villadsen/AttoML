using System.Text;

namespace AttoML.Core.Parsing
{
    public static class AstPrinter
    {
        public static string Print(Expr expr)
        {
            var sb = new StringBuilder();
            PrintExpr(sb, expr);
            return sb.ToString();
        }

        public static string Print(ModuleDecl decl)
        {
            var sb = new StringBuilder();
            PrintDecl(sb, decl);
            return sb.ToString();
        }

        private static void PrintExpr(StringBuilder sb, Expr expr)
        {
            switch (expr)
            {
                case IntLit i: sb.Append(i.Value); break;
                case FloatLit f: sb.Append(f.Value); break;
                case StringLit s: sb.Append('"').Append(s.Value).Append('"'); break;
                case BoolLit b: sb.Append(b.Value ? "true" : "false"); break;
                case UnitLit: sb.Append("()"); break;
                case Var v: sb.Append(v.Name); break;
                case Fun fun:
                    sb.Append("fun ").Append(fun.Param).Append(" -> ");
                    PrintExpr(sb, fun.Body);
                    break;
                case App app:
                    sb.Append('(');
                    PrintExpr(sb, app.Func);
                    sb.Append(' ').
                        Append(' ');
                    PrintExpr(sb, app.Arg);
                    sb.Append(')');
                    break;
                case Let let:
                    sb.Append("let ").Append(let.Name).Append(" = ");
                    PrintExpr(sb, let.Expr);
                    sb.Append(" in ");
                    PrintExpr(sb, let.Body);
                    break;
                case LetRec lr:
                    sb.Append("let rec ").Append(lr.Name).Append(' ').Append(lr.Param).Append(" = ");
                    PrintExpr(sb, lr.FuncBody);
                    sb.Append(" in ");
                    PrintExpr(sb, lr.InBody);
                    break;
                case IfThenElse ite:
                    sb.Append("if "); PrintExpr(sb, ite.Cond);
                    sb.Append(" then "); PrintExpr(sb, ite.Then);
                    sb.Append(" else "); PrintExpr(sb, ite.Else);
                    break;
                case Tuple tup:
                    sb.Append('(');
                    for (int i = 0; i < tup.Items.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        PrintExpr(sb, tup.Items[i]);
                    }
                    sb.Append(')');
                    break;
                case ListLit ll:
                    sb.Append('[');
                    for (int i = 0; i < ll.Items.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        PrintExpr(sb, ll.Items[i]);
                    }
                    sb.Append(']');
                    break;
                case RecordLit rl:
                    sb.Append('{');
                    for (int i = 0; i < rl.Fields.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        var (name, e) = rl.Fields[i];
                        sb.Append(name).Append(" = ");
                        PrintExpr(sb, e);
                    }
                    sb.Append('}');
                    break;
                case Qualify q:
                    sb.Append(q.Module).Append('.').Append(q.Name);
                    break;
                case Match m:
                    sb.Append("match ");
                    PrintExpr(sb, m.Scrutinee);
                    sb.Append(" with ");
                    for (int i = 0; i < m.Cases.Count; i++)
                    {
                        if (i == 0) sb.Append(' ');
                        else sb.Append(" | ");
                        var (pat, ex) = m.Cases[i];
                        PrintPattern(sb, pat);
                        sb.Append(" -> ");
                        PrintExpr(sb, ex);
                    }
                    break;
                case Raise r:
                    sb.Append("raise ");
                    PrintExpr(sb, r.Expr);
                    break;
                case Handle h:
                    PrintExpr(sb, h.Expr);
                    sb.Append(" handle ");
                    for (int i = 0; i < h.Cases.Count; i++)
                    {
                        if (i > 0) sb.Append(" | ");
                        var (pat, ex) = h.Cases[i];
                        PrintPattern(sb, pat);
                        sb.Append(" -> ");
                        PrintExpr(sb, ex);
                    }
                    break;
            }
        }

        private static void PrintPattern(StringBuilder sb, Pattern pat)
        {
            switch (pat)
            {
                case PWildcard: sb.Append('_'); break;
                case PVar pv: sb.Append(pv.Name); break;
                case PInt pi: sb.Append(pi.Value); break;
                case PFloat pf: sb.Append(pf.Value); break;
                case PString ps: sb.Append('"').Append(ps.Value).Append('"'); break;
                case PBool pb: sb.Append(pb.Value ? "true" : "false"); break;
                case PUnit: sb.Append("()"); break;
                case PTuple pt:
                    sb.Append('(');
                    for (int i = 0; i < pt.Items.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        PrintPattern(sb, pt.Items[i]);
                    }
                    sb.Append(')');
                    break;
                case PCtor pc:
                    if (pc.Module != null)
                        sb.Append(pc.Module).Append('.');
                    sb.Append(pc.Name);
                    if (pc.Payload != null)
                    {
                        sb.Append(' ');
                        PrintPattern(sb, pc.Payload);
                    }
                    break;
            }
        }

        private static void PrintDecl(StringBuilder sb, ModuleDecl decl)
        {
            switch (decl)
            {
                case TypeDecl td:
                    sb.Append("type ").Append(td.Name).Append(" = ");
                    for (int i = 0; i < td.Ctors.Count; i++)
                    {
                        if (i > 0) sb.Append(" | ");
                        var c = td.Ctors[i];
                        sb.Append(c.Name);
                        if (c.PayloadType != null)
                        {
                            sb.Append(" of ");
                            PrintTypeExpr(sb, c.PayloadType);
                        }
                    }
                    break;
                case SignatureDecl sd:
                    sb.Append("signature ").Append(sd.Name).Append(" = {");
                    for (int i = 0; i < sd.Vals.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        var v = sd.Vals[i];
                        sb.Append("val ").Append(v.Name);
                        if (v.Type != null)
                        {
                            sb.Append(" : ");
                            PrintTypeExpr(sb, v.Type);
                        }
                    }
                    sb.Append('}');
                    break;
                case StructureDecl st:
                    sb.Append("structure ").Append(st.Name);
                    if (st.SigName != null)
                    {
                        sb.Append(" : ").Append(st.SigName);
                    }
                    sb.Append(" = {");
                    for (int i = 0; i < st.Bindings.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        var b = st.Bindings[i];
                        sb.Append("let ").Append(b.Name);
                        if (b.TypeAnn != null)
                        {
                            sb.Append(" : ");
                            PrintTypeExpr(sb, b.TypeAnn);
                        }
                        sb.Append(" = ");
                        PrintExpr(sb, b.Expr);
                    }
                    sb.Append('}');
                    break;
                case ValDecl vd:
                    sb.Append("val ").Append(vd.Name);
                    if (vd.TypeAnn != null)
                    {
                        sb.Append(" : ");
                        PrintTypeExpr(sb, vd.TypeAnn);
                    }
                    sb.Append(" = ");
                    PrintExpr(sb, vd.Expr);
                    break;
                case ExceptionDecl ed:
                    sb.Append("exception ").Append(ed.Name);
                    if (ed.PayloadType != null)
                    {
                        sb.Append(" of ");
                        PrintTypeExpr(sb, ed.PayloadType);
                    }
                    break;
                case OpenDecl od:
                    sb.Append("open ").Append(od.Name);
                    break;
                default:
                    sb.Append("<unknown decl>");
                    break;
            }
        }

        private static void PrintTypeExpr(StringBuilder sb, TypeExpr te)
        {
            switch (te)
            {
                case TypeName tn:
                    sb.Append(tn.Name);
                    break;
                case TypeArrow ta:
                    // Right-associative printing
                    PrintTypeAtom(sb, ta.From);
                    sb.Append(" -> ");
                    PrintTypeExpr(sb, ta.To);
                    break;
                case TypeTuple tt:
                    // Print product as A * B * C for readability
                    for (int i = 0; i < tt.Items.Count; i++)
                    {
                        if (i > 0) sb.Append(" * ");
                        PrintTypeAtom(sb, tt.Items[i]);
                    }
                    break;
            }
        }

        private static void PrintTypeAtom(StringBuilder sb, TypeExpr te)
        {
            switch (te)
            {
                case TypeName tn:
                    sb.Append(tn.Name);
                    break;
                case TypeTuple tt:
                    sb.Append('(');
                    for (int i = 0; i < tt.Items.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        PrintTypeExpr(sb, tt.Items[i]);
                    }
                    sb.Append(')');
                    break;
                default:
                    PrintTypeExpr(sb, te);
                    break;
            }
        }
    }
}