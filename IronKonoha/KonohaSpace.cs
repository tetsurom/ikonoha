using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Text;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace IronKonoha
{

	public delegate bool StmtTyChecker(KStatement stmt, Syntax syn, KGamma gma);
	public delegate KonohaExpr ExprTyChecker(KStatement stmt, KonohaExpr expr, KGamma gma, KType reqty);
	public delegate int StmtParser(Context ctx, KStatement stmt, Syntax syn, Symbol name, IList<Token> tokens, int start, int end);
	public delegate KonohaExpr ExprParser(Context ctx, Syntax syn, KStatement stmt, IList<Token> tokens, int start, int current, int end);

	[Flags]
	public enum TPOL{
		NOCHECK       =       1,
		ALLOWVOID     = (1 << 1),
		COERCION      = (1 << 2),
		CONST         = (1 << 4),
	}

	/*
    typedef const struct _kKonohaSpace kKonohaSpace;
    struct _kKonohaSpace {
	    kObjectHeader h;
	    kpack_t packid;  kpack_t packdom;
	    const struct _kKonohaSpace   *parentnull;
	    const Ftokenizer *fmat;
	    struct kmap_t   *syntaxMapNN;
	    //
	    void         *gluehdr;
	    kObject      *scrNUL;
	    kcid_t static_cid;   kcid_t function_cid;
	    kArray*       methods;  // default K_EMPTYARRAY
	    karray_t      cl;
    };
    */
	public class KonohaSpace : KObject
	{
		public Context ctx { get; private set; }

		public Dictionary<KeywordType, Syntax> syntaxMap { get; set; }
		public KonohaSpace parent { get; set; }
		public ExpandoObject scope {get; set;}
		public readonly SymbolConst Symbols;
		
		public KonohaSpace(Context ctx)
		{
			this.ctx = ctx;
			this.scope = new ExpandoObject();
			Symbols = new SymbolConst(ctx);
			defineDefaultSyntax();
		}
		
		public KonohaSpace(Context ctx,int child)
		{
			Symbols = new SymbolConst(ctx);
			this.ctx = ctx;
		}

		// sugar.c
		// static void defineDefaultSyntax(CTX, kKonohaSpace *ks)
		private void defineDefaultSyntax()
		{
			KDEFINE_SYNTAX[] syntaxes =
			{
				new KDEFINE_SYNTAX(){
					name = "$ERR",
					flag = SynFlag.StmtBreakExec,
				},
				new KDEFINE_SYNTAX(){
					name = "$expr",
					rule = "$expr",
					PatternMatch = PatternMatch.Expr,
					TopStmtTyCheck = TyCheck.TopStmtTyCheck.Expr,
					ExprTyCheck = TyCheck.ExprTyCheck.Expr,
					kw = KeywordType.Expr,
				},
				new KDEFINE_SYNTAX(){
					name = "$SYMBOL",
					PatternMatch = PatternMatch.Symbol,
					ExprTyCheck = TyCheck.ExprTyCheck.Symbol,
					kw = KeywordType.Symbol,
					flag = SynFlag.ExprTerm,
				},
				new KDEFINE_SYNTAX(){
					name = "$USYMBOL",
					PatternMatch = PatternMatch.Usymbol,
					TopStmtTyCheck = TyCheck.TopStmtTyCheck.ConstDecl,
					ExprTyCheck = TyCheck.ExprTyCheck.USymbol,
					kw = KeywordType.Usymbol,
					flag = SynFlag.ExprTerm,
				},
				new KDEFINE_SYNTAX(){
					name = "$TEXT",
					ExprTyCheck = TyCheck.ExprTyCheck.Text,
					kw = KeywordType.Text,
					flag = SynFlag.ExprTerm,
				},
				new KDEFINE_SYNTAX(){
					name = "$INT",
					ExprTyCheck = TyCheck.ExprTyCheck.Int,
					kw = KeywordType.TKInt,
					flag = SynFlag.ExprTerm,
				},
				new KDEFINE_SYNTAX(){
					name = "$FLOAT",
					ExprTyCheck = TyCheck.ExprTyCheck.Float,
					kw = KeywordType.TKFloat,
					flag = SynFlag.ExprTerm,
				},
				new KDEFINE_SYNTAX(){
					name = "$type",
					rule = "$type $expr",
					PatternMatch = PatternMatch.Type,
					ExprTyCheck = TyCheck.ExprTyCheck.Type,
					kw = KeywordType.Type,
					flag = SynFlag.ExprTerm,
				},
				new KDEFINE_SYNTAX(){
					name = "()",
					kw = KeywordType.Parenthesis,
					ParseExpr = ParseExpr_Parenthesis,
					ExprTyCheck = TyCheck.ExprTyCheck.FuncStyleCall,
					priority_op2 = 16,
					flag = SynFlag.ExprPostfixOp2,
				},
				new KDEFINE_SYNTAX(){
					name = "[]",
					kw = KeywordType.Bracket,
					priority_op2 = 16,
					flag = SynFlag.ExprPostfixOp2,
				},
				new KDEFINE_SYNTAX(){
					name = "{}",
					kw = KeywordType.Brace,
					priority_op2 = 16,
					flag = SynFlag.ExprPostfixOp2,
				},
				new KDEFINE_SYNTAX(){
					name = "$block",
					kw = KeywordType.Block,
					PatternMatch = PatternMatch.Block,
					ExprTyCheck = TyCheck.ExprTyCheck.Block,
				},
				new KDEFINE_SYNTAX(){
					name = "$params",
					kw = KeywordType.Params,
					PatternMatch = PatternMatch.Params,
					TopStmtTyCheck = TyCheck.TopStmtTyCheck.ParamsDecl,
					ExprTyCheck = TyCheck.ExprTyCheck.MethodCall,
				},
				new KDEFINE_SYNTAX(){
					name = "$toks",
					kw = KeywordType.Toks,
					PatternMatch = PatternMatch.Toks,
				},
				new KDEFINE_SYNTAX(){
					name = ".",
					priority_op2 = 16,
					kw = KeywordType.DOT,
					ParseExpr = ParseExpr_Dot,
				},
				new KDEFINE_SYNTAX(){
					name = "/",
					op2 = "opDIV",
					priority_op2 = 32,
					kw = KeywordType.DIV,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "%",
					op2 = "opMOD",
					priority_op2 = 32,
					kw = KeywordType.MOD,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "*",
					op2 = "opMUL",
					priority_op2 = 32,
					kw = KeywordType.MUL,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "+",
					op1 = "opPLUS",
					op2 = "opADD",
					priority_op2 = 64,
					kw = KeywordType.ADD,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "-",
					op1 = "opMINUS",
					op2 = "opSUB",
					priority_op2 = 64,
					kw = KeywordType.SUB,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "<",
					op2 = "opLT",
					priority_op2 = 256,
					kw = KeywordType.LT,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "<=",
					op2 = "opLTE",
					priority_op2 = 256,
					kw = KeywordType.LTE,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = ">",
					op2 = "opGT",
					priority_op2 = 256,
					kw = KeywordType.GT,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = ">=",
					op2 = "opGTE",
					priority_op2 = 256,
					kw = KeywordType.GTE,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "==",
					op2 = "opEQ",
					priority_op2 = 512,
					kw = KeywordType.EQ,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "!=",
					op2 = "opNEQ",
					priority_op2 = 512,
					kw = KeywordType.NEQ,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "&&",
					op2 = "opAND",
					priority_op2 = 1024,
					kw = KeywordType.AND,
					flag = SynFlag.ExprOp,
					ExprTyCheck = TyCheck.ExprTyCheck.And,
				},
				new KDEFINE_SYNTAX(){
					name = "||",
					op2 = "opOR",
					priority_op2 = 2048,
					kw = KeywordType.OR,
					flag = SynFlag.ExprOp,
					ExprTyCheck = TyCheck.ExprTyCheck.Or,
				},
				new KDEFINE_SYNTAX(){
					name = "=",
					priority_op2 = 4096,
					flag = SynFlag.ExprLeftJoinOp2,
				},
				new KDEFINE_SYNTAX(){
				    name = ",",
					op2 = "*",
				    priority_op2 = 8192,
				},
				new KDEFINE_SYNTAX(){
				    name = "$",
				},
				new KDEFINE_SYNTAX(){
					name = "void",
					rule = "$type [$USYMBOL \".\"] $SYMBOL $params [$block]",
					PatternMatch = PatternMatch.Type,
					TopStmtTyCheck = TyCheck.TopStmtTyCheck.MethodDecl,
					kw = KeywordType.StmtMethodDecl,
					type = KType.Void,
				},
				new KDEFINE_SYNTAX(){
					name = "int",
					PatternMatch = PatternMatch.Type,
					kw = KeywordType.Type,
					type = KType.Int,
				},
				new KDEFINE_SYNTAX(){
					name = "boolean",
					PatternMatch = PatternMatch.Type,
					kw = KeywordType.Type,
					type = KType.Boolean,
				},
				//new KDEFINE_SYNTAX(){
				//    name = "null",
				//    kw = KeywordType.Null,
				//    flag = SynFlag.ExprTerm,
				//},
				new KDEFINE_SYNTAX(){
					name = "true",
					kw = KeywordType.True,
					flag = SynFlag.ExprTerm,
					ExprTyCheck = TyCheck.ExprTyCheck.True,
				},
				new KDEFINE_SYNTAX(){
					name = "false",
					kw = KeywordType.False,
					flag = SynFlag.ExprTerm,
					ExprTyCheck = TyCheck.ExprTyCheck.False,
				},
				new KDEFINE_SYNTAX(){
					name = "else",
					kw = KeywordType.Else,
					rule = "\"else\" $block",
					TopStmtTyCheck = TyCheck.TopStmtTyCheck.Else,
					StmtTyCheck = TyCheck.StmtTyCheck.Else,
				},
				new KDEFINE_SYNTAX(){
					name = "if",
					kw = KeywordType.If,
					rule = "\"if\" \"(\" $expr \")\" $block [\"else\" else: $block]",
					TopStmtTyCheck = TyCheck.StmtTyCheck.If,
					StmtTyCheck = TyCheck.StmtTyCheck.If,
				},
				new KDEFINE_SYNTAX(){
					name = "return",
					kw = KeywordType.Return,
					rule = "\"return\" [$expr]",
					flag = SynFlag.StmtBreakExec,
					StmtTyCheck = TyCheck.StmtTyCheck.Return,
				},
			};
			defineSyntax(syntaxes);
			//this.GetSyntax(KeywordType.Void).Type = KType.Void;
			var usynbolRule = new List<Token>();
			parseSyntaxRule("$USYMBOL \"=\" $expr", new LineInfo(0, ""), out usynbolRule);
			this.GetSyntax(KeywordType.Usymbol).SyntaxRule = usynbolRule;
		}

		public IList<Token> tokenize(string script)
		{
			var tokenizer = new Tokenizer(ctx, this);
			return tokenizer.Tokenize(script);
		}

		// static kstatus_t KonohaSpace_eval(CTX, kKonohaSpace *ks, const char *script, kline_t uline)
		public dynamic Eval(string script)
		{
			var tokens = tokenize(script);
			var parser = new Parser(ctx, this);
			var converter = new Converter(ctx, this);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
			block.TyCheckAll(ctx, new KGamma() { ks = this, cid = KType.System, flag = KGammaFlag.TOPLEVEL });
			dynamic ast = converter.Convert(block);
			string dbv = typeof(Expression).InvokeMember("DebugView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty, null, ast, null);
			Console.WriteLine("### DLR AST Dump ###");
			Console.WriteLine(dbv);
			var f = ast.Compile();
			if (f.Method.ReturnType == typeof(void))
			{
				f();
				return null;
			}
			return f();
		}

		// static ksyntax_t* KonohaSpace_getSyntaxRule(CTX, kKonohaSpace *ks, kArray *tls, int s, int e)
		internal Syntax GetSyntaxRule(IList<Token> tls, int s, int e)
		{
			Token tk = tls[s];
			if (tk.IsType)
			{
				tk = (s + 1 < e) ? tls[s + 1] : null;
				if (tk != null && (tk.Type == TokenType.SYMBOL || tk.Type == TokenType.USYMBOL))
				{
					tk = (s + 2 < e) ? tls[s + 2] : null;
					if (tk != null && (tk.Type == TokenType.AST_PARENTHESIS || tk.Keyword == KeywordType.DOT))
					{
						return GetSyntax(KeywordType.StmtMethodDecl); //
					}
					return GetSyntax(KeywordType.Type);  //
				}
				return GetSyntax(KeywordType.Expr);  // expression
			}
			Syntax syn = GetSyntax(tk.Keyword);

			if (syn == null || syn.SyntaxRule == null)
			{
				//wDBG_P("kw='%s', %d, %d", T_kw(syn.KeyWord), syn.ParseExpr == kmodsugar.UndefinedParseExpr, kmodsugar.UndefinedExprTyCheck == syn.ExprTyCheck);
				int i;
				for (i = s + 1; i < e; i++)
				{
					tk = tls[i];
					syn = GetSyntax(tk.Keyword);
					if (syn.SyntaxRule != null && syn.priority > 0)
					{
						ctx.SUGAR_P(ReportLevel.DEBUG, tk.ULine, 0, "binary operator syntax kw='%s'", syn.KeyWord.ToString());   // sugar $expr "=" $expr;
						return syn;
					}
				}
				return GetSyntax(KeywordType.Expr);
			}
			return syn;
		}


		public void SYN_setTopStmtTyCheck(KeywordType ks, StmtTyChecker checker)
		{
			var syn = GetSyntax(ks, true);
			syn.TopStmtTyCheck = checker;
		}

		public void SYN_setStmtTyCheck(KeywordType ks, StmtTyChecker checker)
		{
			var syn = GetSyntax(ks, true);
			syn.StmtTyCheck = checker;
		}

		public void SYN_setExprTyCheck(KeywordType ks, ExprTyChecker checker)
		{
			var syn = GetSyntax(ks, true);
			syn.ExprTyCheck = checker;
		}

		internal Syntax GetSyntax(KeywordType keyword)
		{
			//return GetSyntax(keyword, true);
			return GetSyntax(keyword, false);
		}

		//KonohaSpace_syntax
		internal Syntax GetSyntax(KeywordType keyword, bool isnew)
		{
			
			Syntax syntaxParent = null;
			for (KonohaSpace ks = this; ks != null; ks = ks.parent)
			{
				if (ks.syntaxMap != null && ks.syntaxMap.ContainsKey(keyword))
				{
					syntaxParent = ks.syntaxMap[keyword];
					break;
				}
			}
			if (isnew == true)
			{
				Console.WriteLine("creating new syntax {0} old={1}", keyword.ToString(), syntaxParent);
				if (this.syntaxMap == null)
				{
					this.syntaxMap = new Dictionary<KeywordType, Syntax>();
				}

				this.syntaxMap[keyword] = new Syntax();

				if (syntaxParent != null)
				{  // TODO: RCGC
					this.syntaxMap[keyword] = syntaxParent;
				}
				else
				{
					var syn = new Syntax()
					{
						KeyWord = keyword,
						Type = KType.Unknown,
						Op1 = null,
						Op2 = null,
						ParseExpr = KModSugar.UndefinedParseExpr,
						TopStmtTyCheck = ctx.kmodsugar.UndefinedStmtTyCheck,
						StmtTyCheck = ctx.kmodsugar.UndefinedStmtTyCheck,
						ExprTyCheck = ctx.kmodsugar.UndefinedExprTyCheck,
					};
					this.syntaxMap[keyword] = syn;
				}
				this.syntaxMap[keyword].Parent = syntaxParent;
				return this.syntaxMap[keyword];
			}
			return syntaxParent;
		}

		// static int findTopCh(CTX, kArray *tls, int s, int e, ktoken_t tt, int closech)
		private int findTopCh(IList<Token> tls, int s, int e, TokenType tt, char closech)
		{
			int i;
			for (i = s; i < e; i++)
			{
				Token tk = tls[i];
				if (tk.Type == tt && tk.TopChar == closech) return i;
			}
			Debug.Assert(i != e);  // Must not happen
			return e;
		}

		// static kbool_t checkNestedSyntax(CTX, kArray *tls, int *s, int e, ktoken_t tt, int opench, int closech)
		private bool checkNestedSyntax(IList<Token> tls, ref int s, int e, TokenType tt, char opench, char closech)
		{
			int i = s;
			Token tk = tls[i];
			string t = tk.Text;
			if (t[0] == opench && t.Length == 1)
			{
				int ne = findTopCh(tls, i + 1, e, tk.Type, closech);
				tk.Type = tt;
				tk.Keyword = (KeywordType)tt;
				List<Token> sub;
				//tk->topch = opench; tk.losech = closech;
				makeSyntaxRule(tls, i + 1, ne, out sub);
				tk.Sub = sub;
				s = ne;
				return true;
			}
			return false;
		}

		// static kbool_t makeSyntaxRule(CTX, kArray *tls, int s, int e, kArray *adst)
		private bool makeSyntaxRule(IList<Token> tls, int s, int e, out List<Token> adst)
		{
			int i;
			adst = new List<Token>();
			Symbol nameid = null;
			for (i = s; i < e; i++)
			{
				Token tk = tls[i];
				if (tk.Type == TokenType.INDENT) continue;
				if (tk.Type == TokenType.TEXT /*|| tk.Type == TK_STEXT*/)
				{
					if (checkNestedSyntax(tls, ref i, e, TokenType.AST_PARENTHESIS, '(', ')') ||
						checkNestedSyntax(tls, ref i, e, TokenType.AST_BRACKET, '[', ']') ||
						checkNestedSyntax(tls, ref i, e, TokenType.AST_BRACE, '{', '}'))
					{
					}
					else
					{
						tk.Type = TokenType.CODE;
						tk.Keyword = ctx.kmodsugar.keyword_(tk.Text, Symbol.NewID).Type;
					}
					adst.Add(tk);
					continue;
				}
				if (tk.Type == TokenType.SYMBOL)
				{
					if (i > 0 && tls[i - 1].TopChar == '$')
					{
						var name = string.Format("${0}", tk.Text);
						tk.Keyword = ctx.kmodsugar.keyword_(name, Symbol.NewID).Type;
						tk.Type = TokenType.METANAME;
						if (nameid == null) nameid = Symbol.Get(ctx, tk.Text);
						tk.nameid = nameid;
						nameid = null;
						adst.Add(tk);
						continue;
					}
					if (i + 1 < e && tls[i + 1].TopChar == ':')
					{
						Token tk2 = tls[i];
						nameid = Symbol.Get(ctx, tk2.Text);
						i++;
						continue;
					}
				}
				if (tk.Type == TokenType.OPERATOR)
				{
					if (checkNestedSyntax(tls, ref i, e, TokenType.AST_OPTIONAL, '[', ']'))
					{
						adst.Add(tk);
						continue;
					}
					if (tls[i].TopChar == '$') continue;
				}
				ctx.SUGAR_P(ReportLevel.ERR, tk.ULine, 0, "illegal sugar syntax: {0}", tk.Text);
				return false;
			}
			return true;
		}

		// token.h
		// static void parseSyntaxRule(CTX, const char *rule, kline_t pline, kArray *a);
		public void parseSyntaxRule(string rule, LineInfo pline, out List<Token> adst)
		{
			var tokenizer = new Tokenizer(ctx, this);
			var tokens = tokenizer.Tokenize(rule);
			makeSyntaxRule(tokens, 0, tokens.Count, out adst);
		}

		// struct.h
		// static void KonohaSpace_defineSyntax(CTX, kKonohaSpace *ks, KDEFINE_SYNTAX *syndef)
		public void defineSyntax(KDEFINE_SYNTAX[] syndefs)
		{
			//KMethod pParseStmt = null, pParseExpr = null, pStmtTyCheck = null, pExprTyCheck = null;
			//KMethod mParseStmt = null, mParseExpr = null, mStmtTyCheck = null, mExprTyCheck = null;
			foreach (var syndef in syndefs)
			{
				ctx.kmodsugar.AddKeyword(syndef.name, syndef.kw);
				KeywordType kw = ctx.kmodsugar.keyword_(syndef.name, Symbol.NewID).Type;
				Syntax syn = GetSyntax(kw, true);
				//syn.token = syndef.name;
				syn.Flag |= syndef.flag;
				//if(syndef.type != null) {
				//    syn.Type = syndef.type;
				//}
				//if(syndef.op1 != null) {
				//    syn.Op1 = null;// syndef.op1;//Symbol.Get(ctx, syndef.op1, Symbol.NewID, SymPol.MsETHOD);
				//}
				//if(syndef.op2 != null) {
				//    syn.Op2 = null;// syndef.op2;//Symbol.Get(ctx, syndef.op2, Symbol.NewID, SymPol.MsETHOD);
				//}
				if (syndef.priority_op2 > 0)
				{
					syn.priority = syndef.priority_op2;
				}
				if (syndef.rule != null)
				{
					List<Token> adst;
					parseSyntaxRule(syndef.rule, new LineInfo(0, ""), out adst);
					syn.SyntaxRule = adst;
				}
				syn.PatternMatch = syndef.PatternMatch;
				syn.ParseExpr = syndef.ParseExpr ?? KModSugar.UndefinedParseExpr;
				syn.TopStmtTyCheck = syndef.TopStmtTyCheck;
				syn.StmtTyCheck = syndef.StmtTyCheck ?? ctx.kmodsugar.UndefinedStmtTyCheck;
				syn.ExprTyCheck = syndef.ExprTyCheck ?? ctx.kmodsugar.UndefinedExprTyCheck;
				if (syn.ParseExpr == KModSugar.UndefinedParseExpr)
				{
					if (syn.Flag == SynFlag.ExprOp)
					{
						syn.ParseExpr = KModSugar.ParseExpr_Op;
					}
					else if (syn.Flag == SynFlag.ExprTerm)
					{
						syn.ParseExpr = KModSugar.ParseExpr_Term;
					}
				}
			}
			//Console.WriteLine("syntax size={0}, hmax={1}", syntaxMap.Count, syntaxMap.);
		}

		// static kbool_t KonohaSpace_defineMethod(CTX, kKonohaSpace *ks, kMethod *mtd, kline_t pline)
		internal bool DefineMethod(KFunc mtd, LineInfo lineInfo)
		{
			if(mtd.packid == 0) {
				mtd.packid = this.packid;
			}
			KClass ct = ctx.CT_(mtd.cid);
			if(ct != null && ct.packdom == this.packdom && mtd.isPublic) {
				//ct.addMethod(ctx, mtd);
			}
			else {
				addMethod(mtd);
			}
			return true;
		}

		private void addMethod(KFunc mtd)
		{
			Type retType = GetCSTypeFromKType(mtd.cid);

			var args = mtd.paramNames.ToList();
			var argtypes = mtd.paramTypes.Select(t => GetCSTypeFromKType(t)).ToList();

			Type ftype = null;

			if (retType == typeof(void))
			{
				if (args.Count > 0)
				{
					ftype = Converter.ActionTypes[args.Count].MakeGenericType(argtypes.ToArray());
				}
				else
				{
					ftype = Converter.ActionTypes[0];
				}
			}
			else
			{
				argtypes.Add(typeof(object));
				ftype = Converter.FuncTypes[args.Count].MakeGenericType(argtypes.ToArray());
			}

			Type fctype = typeof(FuncCache<>).MakeGenericType(ftype);

			dynamic cache = Activator.CreateInstance(fctype, new Converter(ctx, this), mtd.Body, args);

			var sc= this.scope as IDictionary<string, object>;

			sc[mtd.Name] = cache.Invoke;

			cache.Scope = this.scope;
			cache.key = mtd.Name;
		}

		private Type GetCSTypeFromKType(KType kType)
		{
			if (kType == KType.Void)
			{
				return typeof(void);
			}
			else if (kType == KType.Int)
			{
				return typeof(long);
			}
			else if (kType == KType.Boolean)
			{
				return typeof(bool);
			}
			return typeof(object);
		}

		// static KMETHOD ParseExpr_Parenthesis(CTX, ksfp_t *sfp _RIX)
		private static KonohaExpr ParseExpr_Parenthesis(Context ctx, Syntax syn, KStatement stmt, IList<Token> tls, int s, int c, int e)
		{
			Token tk = tls[c];
			if(s == c) {
				KonohaExpr expr = stmt.newExpr2(ctx, tk.Sub, 0, tk.Sub.Count);
				return KModSugar.Expr_rightJoin(ctx, expr, stmt, tls, s + 1, c + 1, e);
			}
			else {
				KonohaExpr lexpr = stmt.newExpr2(ctx, tls, s, c);
				if(lexpr == null) {
					return null;
				}
				if (lexpr.syn == null)
				{
					lexpr.syn = stmt.ks.GetSyntax(lexpr.tk.Keyword);
				}
				if(lexpr.syn.KeyWord == KeywordType.DOT) {
					lexpr.syn = stmt.ks.GetSyntax(KeywordType.ExprMethodCall); // CALL
				}
				else if(lexpr.syn.KeyWord != KeywordType.ExprMethodCall) {
					Console.WriteLine("function calls  .. ");
					syn = stmt.ks.GetSyntax(KeywordType.Parenthesis);    // (f null ())
					lexpr  = new ConsExpr(ctx, syn, lexpr, null);
				}
				stmt.addExprParams(ctx, lexpr, tk.Sub, 0, tk.Sub.Count, true/*allowEmpty*/);
				return KModSugar.Expr_rightJoin(ctx, lexpr, stmt, tls, s + 1, c + 1, e);
			}
		}

		static bool isFieldName(IList<Token> tls, int c, int e)
		{
			if(c+1 < e) {
				Token tk = tls[c+1];
				return (tk.Type == TokenType.SYMBOL || tk.Type == TokenType.USYMBOL || tk.Type == TokenType.MSYMBOL);
			}
			return false;
		}

		private static KonohaExpr ParseExpr_Dot(Context ctx, Syntax syn, KStatement stmt, IList<Token> tls, int s, int c, int e)
		{
			Console.WriteLine("s={0}, c={1}", s, c);
			Debug.Assert(s < c);
			if (isFieldName(tls, c, e))
			{
				KonohaExpr expr = stmt.newExpr2(ctx, tls, s, c);
				expr = new ConsExpr(ctx, syn, tls[c + 1], expr);
				return KModSugar.Expr_rightJoin(ctx, expr, stmt, tls, c + 2, c + 2, e);
			}
			if (c + 1 < e) c++;
			return new ConsExpr(ctx, syn, tls[c], ReportLevel.ERR, "expected field name: not " + tls[c].Text);
		}

		public int packid { get; set; }

		public object packdom { get; set; }


		// static kMethod* KonohaSpace_getCastMethodNULL(CTX, kKonohaSpace *ks, kcid_t cid, kcid_t tcid)
		internal KFunc getCastMethod(KType cid, KType tcid)
		{
			throw new NotImplementedException();
			/*
			KFunc mtd = KonohaSpace_getMethod(cid, "to"+tcid);
			if (mtd == null)
			{
				mtd = KonohaSpace_getMethod(cid, "as"+tcid);
			}
			return mtd;
			*/
		}
	}
}
