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

        // STANDARD ML COMPATIBILITY: Keywords can be used as identifiers in binding positions
        // This matches the behavior of SML/NJ, OCaml, and other ML implementations
        // Keywords that can be used as identifiers (parameter names, pattern variables, etc.)
        private bool IsBindableKeyword(TokenKind kind)
        {
            return kind == TokenKind.Open || kind == TokenKind.Type ||
                   kind == TokenKind.In || kind == TokenKind.Case ||
                   kind == TokenKind.Of || kind == TokenKind.End ||
                   kind == TokenKind.Match || kind == TokenKind.With ||
                   kind == TokenKind.Then || kind == TokenKind.Else ||
                   kind == TokenKind.Handle;
            // Note: We keep these reserved for parsing:
            // Fun, Let, Rec, If, Val, Structure, Signature, Exception, Raise
        }

        private bool IsIdentifierOrBindable()
        {
            return Kind == TokenKind.Identifier || IsBindableKeyword(Kind);
        }

        private string ExpectIdentifierOrBindable(string context = "")
        {
            if (IsIdentifierOrBindable())
            {
                var text = Peek().Text;
                Next();
                return text;
            }
            var contextMsg = string.IsNullOrEmpty(context) ? "" : $" in {context}";
            throw new Exception($"Expected identifier{contextMsg}, got {Kind}");
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
            // Accept identifier, bindable keyword, or '(' starting a tuple pattern
            if (t2.Kind != TokenKind.Identifier && t2.Kind != TokenKind.LParen && !IsBindableKeyword(t2.Kind))
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
            while (IsIdentifierOrBindable() || Kind == TokenKind.LParen)
            {
                sawAnyParam = true;
                if (IsIdentifierOrBindable())
                {
                    var p = ExpectIdentifierOrBindable("function parameter");
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
            var name = ExpectIdentifierOrBindable("val declaration name");
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
                name = ExpectIdentifierOrBindable("let binding name");
            }
            if (isRec)
            {
                var param = ExpectIdentifierOrBindable("let rec parameter");
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
            if (IsIdentifierOrBindable())
            {
                var param = ExpectIdentifierOrBindable("fun parameter");
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
            var expr = ParseAtom();
            while (true)
            {
                if (ShouldStopParsing()) break;
                if (Kind == TokenKind.Comma) break;

                if (TryParseListOperator(ref expr)) continue;
                if (TryParseArithmeticOperator(ref expr)) continue;
                if (TryParseRelationalOperator(ref expr)) continue;
                if (TryParseBooleanOperator(ref expr)) continue;
                if (TryParsePipeOperator(ref expr)) continue;
                if (TryParseHandleOperator(ref expr)) continue;

                var arg = ParseAtom();
                expr = new App(expr, arg);
            }
            return expr;
        }

        private bool ShouldStopParsing()
        {
            if (Kind == TokenKind.RParen || Kind == TokenKind.RBracket || Kind == TokenKind.EOF ||
                Kind == TokenKind.In || Kind == TokenKind.Then || Kind == TokenKind.Else ||
                Kind == TokenKind.RBrace || Kind == TokenKind.With || Kind == TokenKind.End ||
                Kind == TokenKind.Bar)
            {
                return true;
            }
            // Avoid starting a new form like let/fun/if/fn/match/val
            if (Kind == TokenKind.Let || Kind == TokenKind.Fun || Kind == TokenKind.Fn ||
                Kind == TokenKind.If || Kind == TokenKind.Match || Kind == TokenKind.Structure ||
                Kind == TokenKind.Signature || Kind == TokenKind.Open || Kind == TokenKind.Type ||
                Kind == TokenKind.Val)
            {
                return true;
            }
            return false;
        }

        private bool TryParseListOperator(ref Expr expr)
        {
            // Handle infix '@' for list append: left @ right -> List.append left right
            if (Kind == TokenKind.At)
            {
                Next();
                var right = ParseNoRelational();
                var append = new Qualify("List", "append");
                expr = new App(new App(append, expr), right);
                return true;
            }
            // Handle infix '::' for list cons: x :: xs -> List.cons x xs
            if (Kind == TokenKind.ColonColon)
            {
                Next();
                var right = ParseNoRelational();
                var cons = new Qualify("List", "cons");
                expr = new App(new App(cons, expr), right);
                return true;
            }
            return false;
        }

        private bool TryParseArithmeticOperator(ref Expr expr)
        {
            if (Kind == TokenKind.Plus || Kind == TokenKind.Minus || Kind == TokenKind.Star ||
                Kind == TokenKind.Slash || Kind == TokenKind.Caret ||
                (Kind == TokenKind.Identifier && (Peek().Text == "div" || Peek().Text == "mod")))
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
                return true;
            }
            return false;
        }

        private bool TryParseRelationalOperator(ref Expr expr)
        {
            if (Kind == TokenKind.Equals || Kind == TokenKind.NotEqual || Kind == TokenKind.EqEq ||
                Kind == TokenKind.BangEq || Kind == TokenKind.LessThan || Kind == TokenKind.GreaterThan ||
                Kind == TokenKind.LessEqual || Kind == TokenKind.GreaterEqual)
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
                return true;
            }
            return false;
        }

        private bool TryParseBooleanOperator(ref Expr expr)
        {
            // Short-circuit boolean operators
            if (Kind == TokenKind.AndThen)
            {
                Next();
                var right = ParseApp();
                expr = new IfThenElse(expr, right, new BoolLit(false));
                return true;
            }
            if (Kind == TokenKind.OrElse)
            {
                Next();
                var right = ParseApp();
                expr = new IfThenElse(expr, new BoolLit(true), right);
                return true;
            }
            // Non-short-circuit boolean word operators: and/or -> Base.and/Base.or
            if (Kind == TokenKind.Identifier && (Peek().Text == "and" || Peek().Text == "or"))
            {
                var opWord = Expect(TokenKind.Identifier).Text;
                var right = ParseApp();
                var q = new Qualify("Base", opWord);
                expr = new App(new App(q, expr), right);
                return true;
            }
            return false;
        }

        private bool TryParsePipeOperator(ref Expr expr)
        {
            // Pipe operator: x |> f becomes f x
            if (Kind == TokenKind.Pipe)
            {
                Next();
                var right = ParseNoRelational();
                expr = new App(right, expr);
                return true;
            }
            return false;
        }

        private bool TryParseHandleOperator(ref Expr expr)
        {
            if (Kind == TokenKind.Handle)
            {
                Next();
                var cases = ParseHandleCases();
                expr = new Handle(expr, cases);
                return true;
            }
            return false;
        }

        // Parse an expression like ParseApp, but stop before consuming relational/equality/short-circuit operators
        private Expr ParseNoRelational()
        {
            var expr = ParseAtom();
            while (true)
            {
                if (Kind == TokenKind.RParen || Kind == TokenKind.RBracket || Kind == TokenKind.EOF || Kind == TokenKind.In || Kind == TokenKind.Then || Kind == TokenKind.Else || Kind == TokenKind.RBrace || Kind == TokenKind.With || Kind == TokenKind.End || Kind == TokenKind.Bar)
                {
                    break;
                }
                if (Kind == TokenKind.Comma) break;
                if (Kind == TokenKind.Let || Kind == TokenKind.Fun || Kind == TokenKind.If || Kind == TokenKind.Structure || Kind == TokenKind.Signature || Kind == TokenKind.Open || Kind == TokenKind.Type)
                {
                    break;
                }
                // Stop on pipe, relational/equality and short-circuit tokens to let outer caller handle precedence
                if (Kind == TokenKind.Pipe || Kind == TokenKind.Equals || Kind == TokenKind.NotEqual || Kind == TokenKind.EqEq || Kind == TokenKind.BangEq || Kind == TokenKind.LessThan || Kind == TokenKind.GreaterThan || Kind == TokenKind.LessEqual || Kind == TokenKind.GreaterEqual || Kind == TokenKind.AndThen || Kind == TokenKind.OrElse)
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
                if (Kind == TokenKind.ColonColon)
                {
                    Next();
                    var right = ParseNoRelational();
                    var cons = new Qualify("List", "cons");
                    expr = new App(new App(cons, expr), right);
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
                        var member = ExpectIdentifierOrBindable("qualified member name");
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
                    // Check if it's a bindable keyword used as a variable
                    if (IsBindableKeyword(Kind))
                    {
                        var keywordText = ExpectIdentifierOrBindable("variable name");
                        // Check for qualified names: keyword.Member
                        if (Match(TokenKind.ModuleQualifiedSep))
                        {
                            var member = ExpectIdentifierOrBindable("qualified member name");
                            return new Qualify(keywordText, member);
                        }
                        return new Var(keywordText);
                    }
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
            // Parse pattern-expression pairs until 'end'
            while (Kind != TokenKind.End)
            {
                var pat = ParsePattern();
                ExpectArrow();
                var expr = ParseExpr();
                cases.Add((pat, expr));
                // Continue on Bar
                if (Kind == TokenKind.Bar)
                {
                    Next();
                    continue;
                }
                // Must be followed by 'end'
                if (Kind != TokenKind.End)
                {
                    throw new Exception($"Expected 'end' or '|' after match case, got {Kind}");
                }
            }
            Expect(TokenKind.End);
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
            // Parse pattern-expression pairs
            // Handle clauses end at natural expression boundaries (no explicit 'end')
            while (true)
            {
                var pat = ParsePattern();
                ExpectArrow();
                var expr = ParseExpr();
                cases.Add((pat, expr));
                // Continue on Bar
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
                        name = ExpectIdentifierOrBindable("qualified constructor name");
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
                    // Check if it's a bindable keyword used as a pattern variable
                    if (IsBindableKeyword(Kind))
                    {
                        var keywordText = ExpectIdentifierOrBindable("pattern variable");
                        // Keywords are always lowercase, so they're pattern variables, not constructors
                        return new PVar(keywordText);
                    }
                    throw new Exception($"Unexpected token {Kind} in pattern");
            }
        }

        private bool IsPatternStart(TokenKind k)
        {
            return k == TokenKind.Identifier || k == TokenKind.IntLiteral || k == TokenKind.FloatLiteral || k == TokenKind.StringLiteral || k == TokenKind.BoolLiteral || k == TokenKind.Unit || k == TokenKind.LParen || k == TokenKind.LBracket || k == TokenKind.LBrace || IsBindableKeyword(k);
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
                    var bn = ExpectIdentifierOrBindable("structure val binding name");
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
                    var bn = ExpectIdentifierOrBindable("structure function name");
                    var idParams = new List<string>();
                    var patParams = new List<Pattern?>();

                    bool sawAnyParam = false;
                    while (IsIdentifierOrBindable() || Kind == TokenKind.LParen)
                    {
                        sawAnyParam = true;
                        if (IsIdentifierOrBindable())
                        {
                            var p = ExpectIdentifierOrBindable("structure function parameter");
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
                        throw new Exception($"Expected at least one parameter after function name '{bn}' in structure. Next token: {Kind}");
                    }

                    // Parse optional type annotation for function
                    TypeExpr? funTypeAnn = null;
                    if (Match(TokenKind.Colon))
                    {
                        funTypeAnn = ParseTypeExpr();
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

                    binds.Add(new Binding(bn, funTypeAnn, desugared));
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

            // Parse optional type parameters: 'a or ('a, 'b)
            var typeParams = new List<string>();
            if (Kind == TokenKind.Quote)  // Single param: 'a option
            {
                Next();  // consume '
                typeParams.Add(Expect(TokenKind.Identifier).Text);
            }
            else if (Kind == TokenKind.LParen && Peek(1).Kind == TokenKind.Quote)  // Multiple params: ('a, 'b) either
            {
                Next();  // consume (
                while (true)
                {
                    Expect(TokenKind.Quote);  // '
                    typeParams.Add(Expect(TokenKind.Identifier).Text);
                    if (!Match(TokenKind.Comma)) break;
                }
                Expect(TokenKind.RParen);
            }

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
            return new TypeDecl(name, typeParams, ctors);
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
            TypeExpr baseType;
            if (Match(TokenKind.LParen))
            {
                var items = new List<TypeExpr>();
                items.Add(ParseTypeExpr());
                while (Match(TokenKind.Comma)) items.Add(ParseTypeExpr());
                Expect(TokenKind.RParen);
                if (items.Count == 1) baseType = items[0];
                else baseType = new TypeTuple(items);
            }
            else if (Match(TokenKind.Quote))
            {
                // Type variable: 'a, 'b, etc.
                var id = Expect(TokenKind.Identifier).Text;
                baseType = new TypeVar(id);
            }
            else
            {
                var id = Expect(TokenKind.Identifier).Text;
                baseType = new TypeName(id);
            }

            // Handle postfix type constructor application (e.g., "t list", "t option", "t map")
            // In ML, type constructors are postfix: "'a list", "('a * 'b) option", etc.
            // Type constructors start with lowercase (by convention) or are predefined types
            // We need to distinguish between type constructors and continuing expressions
            // Type constructors appear when:
            // 1. We just parsed a complete type atom (baseType)
            // 2. Next token is an identifier (potential type constructor)
            // 3. We're not in a context expecting something else (checked by caller)
            while (Kind == TokenKind.Identifier)
            {
                var ctorName = Peek().Text;
                // Check if this looks like a type constructor
                // Type constructors: list, option, result, map, set, etc.
                // We can't easily distinguish from type variables or other uses here,
                // so we'll be permissive and let type checking catch errors
                // However, we need to stop at certain keywords that might follow types
                if (ctorName == "and" || ctorName == "andalso" || ctorName == "orelse" ||
                    ctorName == "of" || ctorName == "in" || ctorName == "then" ||
                    ctorName == "else" || ctorName == "end")
                {
                    // These keywords indicate we've reached the end of the type expression
                    break;
                }
                // CRITICAL FIX: Stop at uppercase identifiers (data constructors, not type constructors)
                // Type constructors are lowercase (list, option, map), data constructors are uppercase (Some, None, MYSOME)
                // This prevents consuming data constructors from subsequent expressions
                if (ctorName.Length > 0 && char.IsUpper(ctorName[0]))
                {
                    break;
                }
                Next(); // consume the type constructor
                baseType = new TypeApp(baseType, ctorName);
            }

            return baseType;
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
