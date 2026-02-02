using System;
using System.Collections.Generic;
using System.Globalization;
using AttoML.Core.Lexer;

namespace AttoML.Core.Parsing
{
    public sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;
        // Track nested match/handle parsing depth; when depth==1, '|' terminates a branch expression
        private int _insideMatchDepth;

        public Parser(IEnumerable<Token> tokens)
        {
            _tokens = new List<Token>(tokens);
            _pos = 0;
        }

        private Token Peek(int offset = 0) => _pos + offset < _tokens.Count ? _tokens[_pos + offset] : _tokens[^1];
        private TokenKind Kind => Peek().Kind;
        private Token Next() => _tokens[_pos++];
        private bool Match(TokenKind kind) { if (Kind == kind) { _pos++; return true; } return false; }
        private Token Expect(TokenKind kind) { var t = Next(); if (t.Kind != kind) throw new Exception($"Expected {kind} but got {t.Kind}"); return t; }

        // Helper to accept either -> or => for backward compatibility
        private void ExpectArrow()
        {
            if (Kind == TokenKind.Arrow || Kind == TokenKind.FatArrow)
            {
                Next();
            }
            else
            {
                throw new Exception($"Expected '->' or '=>' but got {Kind}");
            }
        }

        public (List<ModuleDecl> modules, Expr? expr) ParseCompilationUnit()
        {
            var modules = new List<ModuleDecl>();
            Expr? lastExpr = null;
            while (Kind != TokenKind.EOF)
            {
                if (Kind == TokenKind.Structure)
                {
                    modules.Add(ParseStructure());
                }
                else if (Kind == TokenKind.Signature)
                {
                    modules.Add(ParseSignature());
                }
                else if (Kind == TokenKind.Type)
                {
                    modules.Add(ParseTypeDecl());
                }
                else if (Kind == TokenKind.Exception)
                {
                    modules.Add(ParseExceptionDecl());
                }
                else if (Kind == TokenKind.Open)
                {
                    modules.Add(ParseOpen());
                }
                else if (Kind == TokenKind.Val)
                {
                    modules.Add(ParseTopValDecl());
                }
                else if (IsTopLevelFunDecl())
                {
                    modules.Add(ParseTopLevelFunDecl());
                }
                else
                {
                    lastExpr = ParseExpr();
                }
            }
            return (modules, lastExpr);
        }

        private bool IsTopLevelFunDecl()
        {
            if (Kind != TokenKind.Fun) return false;
            // Look for: fun <name> <param> ... = expr
            // Distinguish from anonymous function: fun <param> -> expr
            var t1 = Peek(1);
            if (t1.Kind != TokenKind.Identifier) return false; // function name
            var t2 = Peek(2);
            if (t2.Kind == TokenKind.Arrow) return false; // anonymous function
            // Require that a parameter-like token follows function name
            // Accept identifier or '(' starting a tuple pattern
            if (t2.Kind != TokenKind.Identifier && t2.Kind != TokenKind.LParen)
                return false;
            return true;
        }

        private ValDecl ParseTopLevelFunDecl()
        {
            // fun name p1 p2 ... = body  ==>  val name = fun p1 -> fun p2 -> ... -> body
            // where each pi can be an identifier or a tuple pattern in parentheses.
            Expect(TokenKind.Fun);
            var name = Expect(TokenKind.Identifier).Text;
            var idParams = new List<string>();
            var patParams = new List<Pattern?>(); // parallel list; null when identifier

            bool sawAnyParam = false;
            while (Kind == TokenKind.Identifier || Kind == TokenKind.LParen)
            {
                sawAnyParam = true;
                if (Kind == TokenKind.Identifier)
                {
                    var p = Expect(TokenKind.Identifier).Text;
                    idParams.Add(p);
                    patParams.Add(null);
                }
                else
                {
                    // tuple or parenthesized pattern
                    var pat = ParseParenOrTuplePattern();
                    idParams.Add("__arg");
                    patParams.Add(pat);
                }
            }
            if (!sawAnyParam)
            {
                throw new Exception("Expected at least one parameter after function name");
            }
            Expect(TokenKind.Equals);
            var body = ParseExpr();
            Expr desugared = body;
            for (int i = idParams.Count - 1; i >= 0; i--)
            {
                var pname = idParams[i];
                var ppat = patParams[i];
                if (ppat == null)
                {
                    desugared = new Fun(pname, desugared);
                }
                else
                {
                    // fun __arg -> match __arg with ppat -> desugared
                    var match = new Match(new Var(pname), new List<(Pattern, Expr)> { (ppat, desugared) });
                    desugared = new Fun(pname, match);
                }
            }
            return new ValDecl(name, null, desugared);
        }

        private ValDecl ParseTopValDecl()
        {
            Expect(TokenKind.Val);
            var name = Expect(TokenKind.Identifier).Text;
            TypeExpr? texpr = null;
            if (Match(TokenKind.Colon))
            {
                texpr = ParseTypeExpr();
            }
            Expect(TokenKind.Equals);
            var e = ParseExpr();
            return new ValDecl(name, texpr, e);
        }

        public Expr ParseExpr()
        {
            if (Kind == TokenKind.Let)
                return ParseLetOrLetRec();
            if (Kind == TokenKind.If)
                return ParseIfThenElse();
            if (Kind == TokenKind.Fun)
                return ParseFun();
            if (Kind == TokenKind.Fn)
                return ParseFn();
            if (Kind == TokenKind.Match)
                return ParseMatch();
            if (Kind == TokenKind.Case)
                return ParseCase();
            if (Kind == TokenKind.Raise)
                return ParseRaise();
            return ParseApp();
        }

        private Expr ParseLetOrLetRec()
        {
            Expect(TokenKind.Let);
            bool isRec = Match(TokenKind.Rec);
            // Support pattern destructuring in non-recursive let via desugaring
            string? name = null;
            Pattern? pat = null;
            if (Kind == TokenKind.LParen)
            {
                pat = ParseParenOrTuplePattern();
            }
            else
            {
                name = Expect(TokenKind.Identifier).Text;
            }
            if (isRec)
            {
                var param = Expect(TokenKind.Identifier).Text;
                TypeExpr? texpr = null;
                if (Match(TokenKind.Colon))
                {
                    texpr = ParseTypeExpr();
                }
                Expect(TokenKind.Equals);
                var body = ParseExpr();
                Expect(TokenKind.In);
                var inBody = ParseExpr();
                return new LetRec(name, param, texpr, body, inBody);
            }
            else
            {
                TypeExpr? texpr = null;
                if (Match(TokenKind.Colon))
                {
                    texpr = ParseTypeExpr();
                }
                Expect(TokenKind.Equals);
                var e1 = ParseExpr();
                Expect(TokenKind.In);
                var e2 = ParseExpr();
                if (pat != null)
                {
                    // let (pat) = e1 in e2  ==>  let __tmp = e1 in match __tmp with pat -> e2
                    var tmp = "__tmp";
                    var match = new Match(new Var(tmp), new List<(Pattern, Expr)> { (pat, e2) });
                    return new Let(tmp, null, e1, match);
                }
                else
                {
                    return new Let(name!, texpr, e1, e2);
                }
            }
        }

        private Expr ParseIfThenElse()
        {
            Expect(TokenKind.If);
            var cond = ParseExpr();
            Expect(TokenKind.Then);
            var thenE = ParseExpr();
            Expect(TokenKind.Else);
            var elseE = ParseExpr();
            return new IfThenElse(cond, thenE, elseE);
        }

        private Expr ParseFun()
        {
            Expect(TokenKind.Fun);
            if (Kind == TokenKind.Identifier)
            {
                var param = Expect(TokenKind.Identifier).Text;
                ExpectArrow();
                var body = ParseExpr();
                return new Fun(param, body);
            }
            else if (Kind == TokenKind.LParen)
            {
                // Desugar fun (pat) -> body into fun __arg -> match __arg with pat -> body
                var pat = ParseParenOrTuplePattern();
                ExpectArrow();
                var body = ParseExpr();
                var argName = "__arg";
                var match = new Match(new Var(argName), new List<(Pattern, Expr)> { (pat, body) });
                return new Fun(argName, match);
            }
            else
            {
                throw new Exception("Expected parameter after 'fun'");
            }
        }

        private Expr ParseFn()
        {
            Expect(TokenKind.Fn);
            if (Kind == TokenKind.Identifier)
            {
                var param = Expect(TokenKind.Identifier).Text;
                ExpectArrow();
                var body = ParseExpr();
                return new Fun(param, body);
            }
            else if (Kind == TokenKind.LParen)
            {
                // Desugar fn (pat) => body into fn __arg => match __arg with pat -> body
                var pat = ParseParenOrTuplePattern();
                ExpectArrow();
                var body = ParseExpr();
                var argName = "__arg";
                var match = new Match(new Var(argName), new List<(Pattern, Expr)> { (pat, body) });
                return new Fun(argName, match);
            }
            else
            {
                throw new Exception("Expected parameter after 'fn'");
            }
        }

        private Expr ParseApp()
        {
            // Check for termination conditions before trying to parse an atom
            // This handles cases where we're at a Bar token inside a match expression
            if (_insideMatchDepth > 0 && Kind == TokenKind.Bar)
            {
                throw new Exception($"Unexpected token {Kind} at start of expression");
            }

            var expr = ParseAtom();
            while (true)
            {
                if (Kind == TokenKind.RParen || Kind == TokenKind.RBracket || Kind == TokenKind.EOF || Kind == TokenKind.In || Kind == TokenKind.Then || Kind == TokenKind.Else || Kind == TokenKind.RBrace || Kind == TokenKind.With || Kind == TokenKind.Of || (_insideMatchDepth > 0 && Kind == TokenKind.Bar))
                {
                    break;
                }
                if (Kind == TokenKind.Comma) break; // tuple handled in atom
                // Application is juxtaposition: f x
                // But avoid starting a new form like let/fun/if/fn/case/val
                if (Kind == TokenKind.Let || Kind == TokenKind.Fun || Kind == TokenKind.Fn || Kind == TokenKind.If || Kind == TokenKind.Case || Kind == TokenKind.Match || Kind == TokenKind.Structure || Kind == TokenKind.Signature || Kind == TokenKind.Open || Kind == TokenKind.Type || Kind == TokenKind.Val)
                {
                    break;
                }
                // Handle infix '@' for list append: left @ right -> List.append left right
                if (Kind == TokenKind.At)
                {
                    Next();
                    var right = ParseNoRelational();
                    var append = new Qualify("List", "append");
                    expr = new App(new App(append, expr), right);
                    continue;
                }
                // Arithmetic infix operators
                if (Kind == TokenKind.Plus || Kind == TokenKind.Minus || Kind == TokenKind.Star || Kind == TokenKind.Slash || Kind == TokenKind.Caret
                    || (Kind == TokenKind.Identifier && (Peek().Text == "div" || Peek().Text == "mod")))
                {
                    var isWordOp = Kind == TokenKind.Identifier;
                    TokenKind op = Kind;
                    string? word = null;
                    if (isWordOp)
                    {
                        word = Expect(TokenKind.Identifier).Text;
                    }
                    else
                    {
                        Next();
                    }
                    var right = ParseNoRelational();
                    if (op == TokenKind.Caret)
                    {
                        // '^' maps to String.concat
                        var qstr = new Qualify("String", "concat");
                        expr = new App(new App(qstr, expr), right);
                    }
                    else
                    {
                        string name = op switch
                        {
                            TokenKind.Plus => "add",
                            TokenKind.Minus => "sub",
                            TokenKind.Star => "mul",
                            TokenKind.Slash => "div",
                            _ => word switch
                            {
                                "div" => "idiv",
                                "mod" => "mod",
                                _ => throw new Exception("unknown op")
                            }
                        };
                        var q = new Qualify("Base", name);
                        expr = new App(new App(q, expr), right);
                    }
                    continue;
                }
                // Relational and equality operators
                if (Kind == TokenKind.Equals || Kind == TokenKind.NotEqual || Kind == TokenKind.EqEq || Kind == TokenKind.BangEq || Kind == TokenKind.LessThan || Kind == TokenKind.GreaterThan || Kind == TokenKind.LessEqual || Kind == TokenKind.GreaterEqual)
                {
                    var op = Kind; Next();
                    var right = ParseApp();
                    if (op == TokenKind.Equals || op == TokenKind.EqEq)
                    {
                        var eq = new Qualify("Base", "eq");
                        expr = new App(new App(eq, expr), right);
                    }
                    else if (op == TokenKind.NotEqual || op == TokenKind.BangEq)
                    {
                        var eq = new Qualify("Base", "eq");
                        var not = new Qualify("Base", "not");
                        var eqApp = new App(new App(eq, expr), right);
                        expr = new App(not, eqApp);
                    }
                    else if (op == TokenKind.LessThan)
                    {
                        var lt = new Qualify("Base", "lt");
                        expr = new App(new App(lt, expr), right);
                    }
                    else if (op == TokenKind.GreaterThan)
                    {
                        var lt = new Qualify("Base", "lt");
                        expr = new App(new App(lt, right), expr);
                    }
                    else if (op == TokenKind.LessEqual)
                    {
                        var lt = new Qualify("Base", "lt");
                        var eq = new Qualify("Base", "eq");
                        var or = new Qualify("Base", "or");
                        var ltApp = new App(new App(lt, expr), right);
                        var eqApp = new App(new App(eq, expr), right);
                        expr = new App(new App(or, ltApp), eqApp);
                    }
                    else if (op == TokenKind.GreaterEqual)
                    {
                        var lt = new Qualify("Base", "lt");
                        var eq = new Qualify("Base", "eq");
                        var or = new Qualify("Base", "or");
                        var ltApp = new App(new App(lt, right), expr);
                        var eqApp = new App(new App(eq, expr), right);
                        expr = new App(new App(or, ltApp), eqApp);
                    }
                    continue;
                }
                // Short-circuit boolean operators
                if (Kind == TokenKind.AndThen)
                {
                    Next();
                    var right = ParseApp();
                    expr = new IfThenElse(expr, right, new BoolLit(false));
                    continue;
                }
                if (Kind == TokenKind.OrElse)
                {
                    Next();
                    var right = ParseApp();
                    expr = new IfThenElse(expr, new BoolLit(true), right);
                    continue;
                }
                // Non-short-circuit boolean word operators: and/or -> Base.and/Base.or
                if (Kind == TokenKind.Identifier && (Peek().Text == "and" || Peek().Text == "or"))
                {
                    var opWord = Expect(TokenKind.Identifier).Text;
                    var right = ParseApp();
                    var q = new Qualify("Base", opWord);
                    expr = new App(new App(q, expr), right);
                    continue;
                }
                if (Kind == TokenKind.Handle)
                {
                    Next();
                    var cases = ParseHandleCases();
                    expr = new Handle(expr, cases);
                    continue;
                }
                var arg = ParseAtom();
                expr = new App(expr, arg);
            }
            return expr;
        }

        // Parse an expression like ParseApp, but stop before consuming relational/equality/short-circuit operators
        private Expr ParseNoRelational()
        {
            // Check for termination conditions before trying to parse an atom
            if (_insideMatchDepth > 0 && Kind == TokenKind.Bar)
            {
                throw new Exception($"Unexpected token {Kind} at start of expression");
            }

            var expr = ParseAtom();
            while (true)
            {
                if (Kind == TokenKind.RParen || Kind == TokenKind.RBracket || Kind == TokenKind.EOF || Kind == TokenKind.In || Kind == TokenKind.Then || Kind == TokenKind.Else || Kind == TokenKind.RBrace || Kind == TokenKind.With || (_insideMatchDepth > 0 && Kind == TokenKind.Bar))
                {
                    break;
                }
                if (Kind == TokenKind.Comma) break;
                if (Kind == TokenKind.Let || Kind == TokenKind.Fun || Kind == TokenKind.If || Kind == TokenKind.Structure || Kind == TokenKind.Signature || Kind == TokenKind.Open || Kind == TokenKind.Type)
                {
                    break;
                }
                // Stop on relational/equality and short-circuit tokens to let outer caller handle precedence
                if (Kind == TokenKind.Equals || Kind == TokenKind.NotEqual || Kind == TokenKind.EqEq || Kind == TokenKind.BangEq || Kind == TokenKind.LessThan || Kind == TokenKind.GreaterThan || Kind == TokenKind.LessEqual || Kind == TokenKind.GreaterEqual || Kind == TokenKind.AndThen || Kind == TokenKind.OrElse)
                {
                    break;
                }
                if (Kind == TokenKind.At)
                {
                    Next();
                    var right = ParseNoRelational();
                    var append = new Qualify("List", "append");
                    expr = new App(new App(append, expr), right);
                    continue;
                }
                if (Kind == TokenKind.Plus || Kind == TokenKind.Minus || Kind == TokenKind.Star || Kind == TokenKind.Slash || Kind == TokenKind.Caret
                    || (Kind == TokenKind.Identifier && (Peek().Text == "div" || Peek().Text == "mod")))
                {
                    var isWordOp = Kind == TokenKind.Identifier;
                    TokenKind op = Kind;
                    string? word = null;
                    if (isWordOp)
                    {
                        word = Expect(TokenKind.Identifier).Text;
                    }
                    else
                    {
                        Next();
                    }
                    var right = ParseNoRelational();
                    if (op == TokenKind.Caret)
                    {
                        var qstr = new Qualify("String", "concat");
                        expr = new App(new App(qstr, expr), right);
                    }
                    else
                    {
                        string name = op switch
                        {
                            TokenKind.Plus => "add",
                            TokenKind.Minus => "sub",
                            TokenKind.Star => "mul",
                            TokenKind.Slash => "div",
                            _ => word switch
                            {
                                "div" => "idiv",
                                "mod" => "mod",
                                _ => throw new Exception("unknown op")
                            }
                        };
                        var q = new Qualify("Base", name);
                        expr = new App(new App(q, expr), right);
                    }
                    continue;
                }
                var arg = ParseAtom();
                expr = new App(expr, arg);
            }
            return expr;
        }

        private Expr ParseAtom()
        {
            switch (Kind)
            {
                case TokenKind.IntLiteral:
                    return new IntLit(int.Parse(Next().Text, CultureInfo.InvariantCulture));
                case TokenKind.FloatLiteral:
                    return new FloatLit(double.Parse(Next().Text, CultureInfo.InvariantCulture));
                case TokenKind.StringLiteral:
                    return new StringLit(Next().Text);
                case TokenKind.BoolLiteral:
                    return new BoolLit(Next().Text == "true");
                case TokenKind.Unit:
                    Next(); return UnitLit.Instance;
                case TokenKind.Identifier:
                    // Might be qualified: Module.Name
                    var id = Expect(TokenKind.Identifier).Text;
                    if (Match(TokenKind.ModuleQualifiedSep))
                    {
                        var member = Expect(TokenKind.Identifier).Text;
                        return new Qualify(id, member);
                    }
                    return new Var(id);
                case TokenKind.LParen:
                    return ParseParenOrTuple();
                case TokenKind.LBracket:
                    return ParseList();
                case TokenKind.LBrace:
                    return ParseRecord();
                default:
                    throw new Exception($"Unexpected token {Kind}");
            }
        }

        private Expr ParseMatch()
        {
            Expect(TokenKind.Match);
            var scrutinee = ParseExpr();
            Expect(TokenKind.With);
            var cases = new List<(Pattern, Expr)>();
            // Allow optional leading '|'
            if (Kind == TokenKind.Bar)
            {
                Next();
            }
            while (true)
            {
                var pat = ParsePattern();
                ExpectArrow();
                _insideMatchDepth++;
                var expr = ParseExpr();
                _insideMatchDepth--;
                cases.Add((pat, expr));
                if (Kind == TokenKind.Bar)
                {
                    Next();
                    continue;
                }
                break;
            }
            return new Match(scrutinee, cases);
        }

        private Expr ParseCase()
        {
            Expect(TokenKind.Case);
            var scrutinee = ParseExpr();
            Expect(TokenKind.Of);
            var cases = new List<(Pattern, Expr)>();
            // Allow optional leading '|'
            if (Kind == TokenKind.Bar)
            {
                Next();
            }
            while (true)
            {
                var pat = ParsePattern();
                ExpectArrow();
                _insideMatchDepth++;
                var expr = ParseExpr();
                _insideMatchDepth--;
                cases.Add((pat, expr));
                if (Kind == TokenKind.Bar)
                {
                    Next();
                    continue;
                }
                break;
            }
            return new Match(scrutinee, cases);
        }

        private Expr ParseRaise()
        {
            Expect(TokenKind.Raise);
            var e = ParseExpr();
            return new Raise(e);
        }

        private List<(Pattern, Expr)> ParseHandleCases()
        {
            var cases = new List<(Pattern, Expr)>();
            // Optional leading '|'
            if (Kind == TokenKind.Bar) Next();
            while (true)
            {
                var pat = ParsePattern();
                ExpectArrow();
                _insideMatchDepth++;
                var expr = ParseExpr();
                _insideMatchDepth--;
                cases.Add((pat, expr));
                if (Kind == TokenKind.Bar)
                {
                    Next();
                    continue;
                }
                break;
            }
            return cases;
        }

        private Pattern ParsePattern()
        {
            var pat = ParseAtomicPattern();

            // Check for cons pattern: pat :: rest
            if (Kind == TokenKind.ColonColon)
            {
                Next();
                var tail = ParsePattern();
                return new PListCons(pat, tail);
            }

            return pat;
        }

        private Pattern ParseAtomicPattern()
        {
            switch (Kind)
            {
                case TokenKind.IntLiteral:
                    return new PInt(int.Parse(Next().Text, CultureInfo.InvariantCulture));
                case TokenKind.FloatLiteral:
                    return new PFloat(double.Parse(Next().Text, CultureInfo.InvariantCulture));
                case TokenKind.StringLiteral:
                    return new PString(Next().Text);
                case TokenKind.BoolLiteral:
                    return new PBool(Next().Text == "true");
                case TokenKind.Unit:
                    Next(); return PUnit.Instance;
                case TokenKind.LParen:
                    return ParseParenOrTuplePattern();
                case TokenKind.LBracket:
                    return ParseListPattern();
                case TokenKind.LBrace:
                    return ParseRecordPattern();
                case TokenKind.Identifier:
                    // Wildcard pattern
                    var idTok = Expect(TokenKind.Identifier);
                    var id = idTok.Text;
                    if (id == "_")
                    {
                        return PWildcard.Instance;
                    }
                    // Qualified constructor pattern Module.Name
                    string? module = null;
                    string name = id;
                    if (Match(TokenKind.ModuleQualifiedSep))
                    {
                        module = id;
                        name = Expect(TokenKind.Identifier).Text;
                    }
                    // Constructor pattern if capitalized or qualified; optional payload pattern
                    if (module != null || (name.Length > 0 && char.IsUpper(name[0])))
                    {
                        Pattern? payload = null;
                        if (IsPatternStart(Kind))
                        {
                            payload = ParsePattern();
                        }
                        return new PCtor(module, name, payload);
                    }
                    // Otherwise variable pattern
                    return new PVar(name);
                default:
                    throw new Exception($"Unexpected token {Kind} in pattern");
            }
        }

        private bool IsPatternStart(TokenKind k)
        {
            return k == TokenKind.Identifier || k == TokenKind.IntLiteral || k == TokenKind.FloatLiteral || k == TokenKind.StringLiteral || k == TokenKind.BoolLiteral || k == TokenKind.Unit || k == TokenKind.LParen || k == TokenKind.LBracket || k == TokenKind.LBrace;
        }

        private Pattern ParseParenOrTuplePattern()
        {
            Expect(TokenKind.LParen);
            var items = new List<Pattern>();
            items.Add(ParsePattern());
            while (Match(TokenKind.Comma)) items.Add(ParsePattern());
            Expect(TokenKind.RParen);
            if (items.Count == 1)
            {
                return items[0];
            }
            return new PTuple(items);
        }

        private Pattern ParseListPattern()
        {
            Expect(TokenKind.LBracket);
            var items = new List<Pattern>();
            if (Kind != TokenKind.RBracket)
            {
                items.Add(ParsePattern());
                while (Match(TokenKind.Comma))
                {
                    items.Add(ParsePattern());
                }
            }
            Expect(TokenKind.RBracket);
            return new PList(items);
        }

        private Pattern ParseRecordPattern()
        {
            Expect(TokenKind.LBrace);
            var fields = new List<(string, Pattern)>();
            if (Kind != TokenKind.RBrace)
            {
                while (true)
                {
                    var fieldName = Expect(TokenKind.Identifier).Text;
                    Expect(TokenKind.Equals);
                    var pat = ParsePattern();
                    fields.Add((fieldName, pat));
                    if (!Match(TokenKind.Comma))
                    {
                        break;
                    }
                }
            }
            Expect(TokenKind.RBrace);
            return new PRecord(fields);
        }

        private Expr ParseParenOrTuple()
        {
            Expect(TokenKind.LParen);
            var items = new List<Expr>();
            if (Kind == TokenKind.RParen)
            {
                Expect(TokenKind.RParen);
                return UnitLit.Instance;
            }
            items.Add(ParseExpr());
            while (Match(TokenKind.Comma))
            {
                items.Add(ParseExpr());
            }
            Expect(TokenKind.RParen);
            if (items.Count == 1) return items[0];
            return new Tuple(items);
        }

        private StructureDecl ParseStructure()
        {
            Expect(TokenKind.Structure);
            var name = Expect(TokenKind.Identifier).Text;
            string? sigName = null;
            if (Match(TokenKind.Colon))
            {
                sigName = Expect(TokenKind.Identifier).Text;
            }
            Expect(TokenKind.Equals);
            Expect(TokenKind.LBrace);
            var binds = new List<Binding>();
            while (Kind != TokenKind.RBrace)
            {
                // Support val, fun, or let syntax
                if (Kind == TokenKind.Val)
                {
                    // val name : type? = expr
                    Expect(TokenKind.Val);
                    var bn = Expect(TokenKind.Identifier).Text;
                    TypeExpr? texpr = null;
                    if (Match(TokenKind.Colon))
                    {
                        texpr = ParseTypeExpr();
                    }
                    Expect(TokenKind.Equals);
                    var be = ParseExpr();
                    binds.Add(new Binding(bn, texpr, be));
                }
                else if (Kind == TokenKind.Fun)
                {
                    // fun name p1 p2 ... = body  ==>  desugared to lambda
                    Expect(TokenKind.Fun);
                    var bn = Expect(TokenKind.Identifier).Text;
                    var idParams = new List<string>();
                    var patParams = new List<Pattern?>();
                    
                    bool sawAnyParam = false;
                    while (Kind == TokenKind.Identifier || Kind == TokenKind.LParen)
                    {
                        sawAnyParam = true;
                        if (Kind == TokenKind.Identifier)
                        {
                            var p = Expect(TokenKind.Identifier).Text;
                            idParams.Add(p);
                            patParams.Add(null);
                        }
                        else
                        {
                            var pat = ParseParenOrTuplePattern();
                            idParams.Add("__arg");
                            patParams.Add(pat);
                        }
                    }
                    
                    if (!sawAnyParam)
                    {
                        throw new Exception("Expected at least one parameter after function name in structure");
                    }
                    
                    Expect(TokenKind.Equals);
                    var body = ParseExpr();
                    
                    // Desugar to nested lambdas
                    Expr desugared = body;
                    for (int i = idParams.Count - 1; i >= 0; i--)
                    {
                        var pname = idParams[i];
                        var ppat = patParams[i];
                        if (ppat == null)
                        {
                            desugared = new Fun(pname, desugared);
                        }
                        else
                        {
                            var match = new Match(new Var(pname), new List<(Pattern, Expr)> { (ppat, desugared) });
                            desugared = new Fun(pname, match);
                        }
                    }
                    
                    binds.Add(new Binding(bn, null, desugared));
                }
                else if (Kind == TokenKind.Let)
                {
                    // let name : type? = expr (backward compatibility)
                    Expect(TokenKind.Let);
                    var bn = Expect(TokenKind.Identifier).Text;
                    TypeExpr? texpr = null;
                    if (Match(TokenKind.Colon))
                    {
                        texpr = ParseTypeExpr();
                    }
                    Expect(TokenKind.Equals);
                    var be = ParseExpr();
                    binds.Add(new Binding(bn, texpr, be));
                }
                else
                {
                    throw new Exception($"Expected val, fun, or let in structure body, but got {Kind}");
                }
                
                if (Kind == TokenKind.Comma) Next();
            }
            Expect(TokenKind.RBrace);
            return new StructureDecl(name, binds, sigName);
        }

        private SignatureDecl ParseSignature()
        {
            Expect(TokenKind.Signature);
            var name = Expect(TokenKind.Identifier).Text;
            Expect(TokenKind.Equals);
            Expect(TokenKind.LBrace);
            var vals = new List<SignatureVal>();
            while (Kind != TokenKind.RBrace)
            {
                Expect(TokenKind.Val);
                var vn = Expect(TokenKind.Identifier).Text;
                TypeExpr? texpr = null;
                if (Match(TokenKind.Colon))
                {
                    texpr = ParseTypeExpr();
                }
                vals.Add(new SignatureVal(vn, texpr));
                if (Kind == TokenKind.Comma) Next();
            }
            Expect(TokenKind.RBrace);
            return new SignatureDecl(name, vals);
        }

        private OpenDecl ParseOpen()
        {
            Expect(TokenKind.Open);
            var name = Expect(TokenKind.Identifier).Text;
            return new OpenDecl(name);
        }

        private TypeDecl ParseTypeDecl()
        {
            Expect(TokenKind.Type);
            var name = Expect(TokenKind.Identifier).Text;
            Expect(TokenKind.Equals);
            var ctors = new List<TypeCtorDecl>();
            while (true)
            {
                if (Kind == TokenKind.Bar)
                {
                    Next();
                }
                var ctorName = Expect(TokenKind.Identifier).Text;
                TypeExpr? payload = null;
                if (Match(TokenKind.Of))
                {
                    payload = ParseTypeExpr();
                }
                ctors.Add(new TypeCtorDecl(ctorName, payload));
                if (Kind != TokenKind.Bar)
                {
                    break;
                }
            }
            return new TypeDecl(name, ctors);
        }

        private ExceptionDecl ParseExceptionDecl()
        {
            Expect(TokenKind.Exception);
            var name = Expect(TokenKind.Identifier).Text;
            TypeExpr? payload = null;
            if (Match(TokenKind.Of))
            {
                payload = ParseTypeExpr();
            }
            return new ExceptionDecl(name, payload);
        }

        private TypeExpr ParseTypeExpr()
        {
            // Parse arrow types with right associativity: a -> b -> c == a -> (b -> c)
            // '*' forms product types; we treat sequences like A * B * C as tuples (A, B, C)
            var left = ParseTypeProduct();
            while (Match(TokenKind.Arrow))
            {
                var right = ParseTypeProduct();
                left = new TypeArrow(left, right);
            }
            return left;
        }

        private TypeExpr ParseTypeProduct()
        {
            var items = new List<TypeExpr>();
            items.Add(ParseTypeAtom());
            while (Match(TokenKind.Star))
            {
                items.Add(ParseTypeAtom());
            }
            if (items.Count == 1) return items[0];
            return new TypeTuple(items);
        }

        private TypeExpr ParseTypeAtom()
        {
            if (Match(TokenKind.LParen))
            {
                var items = new List<TypeExpr>();
                items.Add(ParseTypeExpr());
                while (Match(TokenKind.Comma)) items.Add(ParseTypeExpr());
                Expect(TokenKind.RParen);
                if (items.Count == 1) return items[0];
                return new TypeTuple(items);
            }
            var id = Expect(TokenKind.Identifier).Text;
            return new TypeName(id);
        }

        private Expr ParseList()
        {
            Expect(TokenKind.LBracket);
            var items = new List<Expr>();
            if (Kind == TokenKind.RBracket)
            {
                Expect(TokenKind.RBracket);
                return new ListLit(items);
            }
            items.Add(ParseExpr());
            while (Match(TokenKind.Comma))
            {
                items.Add(ParseExpr());
            }
            Expect(TokenKind.RBracket);
            return new ListLit(items);
        }

        private Expr ParseRecord()
        {
            Expect(TokenKind.LBrace);
            var fields = new List<(string, Expr)>();
            if (Kind == TokenKind.RBrace)
            {
                Expect(TokenKind.RBrace);
                return new RecordLit(fields);
            }
            var name = Expect(TokenKind.Identifier).Text;
            Expect(TokenKind.Equals);
            var e = ParseExpr();
            fields.Add((name, e));
            while (Match(TokenKind.Comma))
            {
                name = Expect(TokenKind.Identifier).Text;
                Expect(TokenKind.Equals);
                e = ParseExpr();
                fields.Add((name, e));
            }
            Expect(TokenKind.RBrace);
            return new RecordLit(fields);
        }
    }
}
