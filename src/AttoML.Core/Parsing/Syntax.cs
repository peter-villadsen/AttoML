using System;
using System.Collections.Generic;

namespace AttoML.Core.Parsing
{
    // AST nodes for the language
    public abstract class Expr { }

    public sealed class IntLit : Expr { public int Value; public IntLit(int v){Value=v;} }
    public sealed class FloatLit : Expr { public double Value; public FloatLit(double v){Value=v;} }
    public sealed class StringLit : Expr { public string Value; public StringLit(string v){Value=v;} }
    public sealed class BoolLit : Expr { public bool Value; public BoolLit(bool v){Value=v;} }
    public sealed class UnitLit : Expr { public static readonly UnitLit Instance = new UnitLit(); private UnitLit(){} }

    public sealed class Var : Expr { public string Name; public Var(string n){Name=n;} }

    public sealed class Fun : Expr { public string Param; public Expr Body; public Fun(string p, Expr b){Param=p; Body=b;} }
    public sealed class App : Expr { public Expr Func; public Expr Arg; public App(Expr f, Expr a){Func=f; Arg=a;} }

    public sealed class Let : Expr { public string Name; public TypeExpr? TypeAnn; public Expr Expr; public Expr Body; public Let(string n, TypeExpr? ta, Expr e, Expr b){Name=n; TypeAnn=ta; Expr=e; Body=b;} }
    public sealed class LetRec : Expr { public string Name; public string Param; public TypeExpr? TypeAnn; public Expr FuncBody; public Expr InBody; public LetRec(string n, string p, TypeExpr? ta, Expr fb, Expr ib){Name=n; Param=p; TypeAnn=ta; FuncBody=fb; InBody=ib;} }

    public sealed class IfThenElse : Expr { public Expr Cond, Then, Else; public IfThenElse(Expr c, Expr t, Expr e){Cond=c; Then=t; Else=e;} }

    public sealed class Tuple : Expr { public List<Expr> Items; public Tuple(List<Expr> items){Items=items;} }
    public sealed class ListLit : Expr { public List<Expr> Items; public ListLit(List<Expr> items){Items=items;} }
    public sealed class RecordLit : Expr { public List<(string Name, Expr Expr)> Fields; public RecordLit(List<(string, Expr)> fields){Fields=fields;} }
    public sealed class RecordAccess : Expr { public Expr Record; public string Field; public RecordAccess(Expr r, string f){Record=r; Field=f;} }

    public sealed class Qualify : Expr { public string Module; public string Name; public Qualify(string m,string n){Module=m;Name=n;} }

    // Exceptions
    public sealed class Raise : Expr { public Expr Expr; public Raise(Expr e){Expr=e;} }
    public sealed class Handle : Expr { public Expr Expr; public List<(Pattern Pat, Expr Expr)> Cases; public Handle(Expr e, List<(Pattern, Expr)> cases){Expr=e; Cases=cases;} }

    // Pattern matching
    public abstract class Pattern { }
    public sealed class PWildcard : Pattern { public static readonly PWildcard Instance = new PWildcard(); private PWildcard(){} }
    public sealed class PVar : Pattern { public string Name; public PVar(string n){Name=n;} }
    public sealed class PInt : Pattern { public int Value; public PInt(int v){Value=v;} }
    public sealed class PFloat : Pattern { public double Value; public PFloat(double v){Value=v;} }
    public sealed class PString : Pattern { public string Value; public PString(string v){Value=v;} }
    public sealed class PBool : Pattern { public bool Value; public PBool(bool v){Value=v;} }
    public sealed class PUnit : Pattern { public static readonly PUnit Instance = new PUnit(); private PUnit(){} }
    public sealed class PTuple : Pattern { public List<Pattern> Items; public PTuple(List<Pattern> items){Items=items;} }
    public sealed class PList : Pattern { public List<Pattern> Items; public PList(List<Pattern> items){Items=items;} }
    public sealed class PListCons : Pattern { public Pattern Head; public Pattern Tail; public PListCons(Pattern h, Pattern t){Head=h; Tail=t;} }
    public sealed class PRecord : Pattern { public List<(string Name, Pattern Pat)> Fields; public PRecord(List<(string, Pattern)> fields){Fields=fields;} }
    public sealed class PCtor : Pattern { public string? Module; public string Name; public Pattern? Payload; public PCtor(string? m, string n, Pattern? payload){Module=m; Name=n; Payload=payload;} }

    public sealed class Match : Expr { public Expr Scrutinee; public List<(Pattern Pat, Expr Expr)> Cases; public Match(Expr s, List<(Pattern, Expr)> cases){Scrutinee=s; Cases=cases;} }

    // Module system AST
    public abstract class ModuleDecl { }
    public sealed class StructureDecl : ModuleDecl { public string Name; public string? SigName; public List<Binding> Bindings; public StructureDecl(string n, List<Binding> bs, string? sigName=null){Name=n; Bindings=bs; SigName=sigName;} }
    public sealed class SignatureDecl : ModuleDecl { public string Name; public List<SignatureVal> Vals; public SignatureDecl(string n, List<SignatureVal> vs){Name=n; Vals=vs;} }
    public sealed class OpenDecl : ModuleDecl { public string Name; public OpenDecl(string n){Name=n;} }
    public sealed class TypeDecl : ModuleDecl { public string Name; public List<string> TypeParams; public List<TypeCtorDecl> Ctors; public TypeDecl(string n, List<string> typeParams, List<TypeCtorDecl> ctors){Name=n; TypeParams=typeParams; Ctors=ctors;} }
    public sealed class ValDecl : ModuleDecl { public string Name; public Parsing.TypeExpr? TypeAnn; public Expr Expr; public ValDecl(string n, Parsing.TypeExpr? t, Expr e){Name=n; TypeAnn=t; Expr=e;} }
    public sealed class ExceptionDecl : ModuleDecl { public string Name; public TypeExpr? PayloadType; public ExceptionDecl(string n, TypeExpr? t){Name=n; PayloadType=t;} }
    public sealed class TypeCtorDecl { public string Name; public TypeExpr? PayloadType; public TypeCtorDecl(string n, TypeExpr? t){Name=n; PayloadType=t;} }

    public sealed class Binding { public string Name; public TypeExpr? TypeAnn; public Expr Expr; public Binding(string n, TypeExpr? t, Expr e){Name=n; TypeAnn=t; Expr=e;} }
    public sealed class SignatureVal { public string Name; public TypeExpr? Type; public SignatureVal(string n, TypeExpr? t){Name=n; Type=t;} }

    // Type expression AST (for signatures and annotations)
    public abstract class TypeExpr { }
    public sealed class TypeName : TypeExpr { public string Name; public TypeName(string n){Name=n;} }
    public sealed class TypeVar : TypeExpr { public string Name; public TypeVar(string n){Name=n;} }
    public sealed class TypeArrow : TypeExpr { public TypeExpr From, To; public TypeArrow(TypeExpr f, TypeExpr t){From=f; To=t;} }
    public sealed class TypeTuple : TypeExpr { public List<TypeExpr> Items; public TypeTuple(List<TypeExpr> items){Items=items;} }
    public sealed class TypeApp : TypeExpr { public TypeExpr Base; public string Constructor; public TypeApp(TypeExpr b, string c){Base=b; Constructor=c;} }
}
