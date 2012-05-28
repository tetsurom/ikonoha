using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha
{
	[Flags]
	public enum SynFlag
	{
		ExprTerm = 1,
		ExprOp = 1 << 1,
		ExprLeftJoinOp2 = 1 << 2,
		ExprPostfixOp2 = 1 << 3,
		StmtBreakExec = 1 << 8,
		StmtJumpAhead = 1 << 9,
		StmtJumpSkip = 1 << 10,
	}

	public delegate void StmtTyChecker(KStatement stmt, Syntax syn, KGamma gma);


	/*
    typedef const struct _ksyntax ksyntax_t;
    struct _ksyntax {
	    keyword_t kw;  kflag_t flag;
	    kArray   *syntaxRulenull;
	    kMethod  *ParseStmtnull;
	    kMethod  *ParseExpr;
	    kMethod  *TopStmtTyCheck;
	    kMethod  *StmtTyCheck;
	    kMethod  *ExprTyCheck;
	    // binary
	    ktype_t    ty;   kshort_t priority;
	    kmethodn_t op2;  kmethodn_t op1;      // & a
	    //kshort_t dummy;
    };
    */
	public class Syntax
	{
		public IList<Token> SyntaxRule { get; set; }
		public KeywordType KeyWord { get; set; }
		public SynFlag Flag { get; set; }
		/// <summary>
		/// 文法の優先度？ 
		/// </summary>
		public int priority { get; set; }
		public KonohaType Type { get; set; }
		public StmtTyChecker ParseStmt { get; set; }
		public StmtTyChecker ParseExpr { get; set; }
		public StmtTyChecker TopStmtTyCheck { get; set; }
		public StmtTyChecker StmtTyCheck { get; set; }
		public StmtTyChecker ExprTyCheck { get; set; }
		public KMethod Op1 { get; set; }
		public KMethod Op2 { get; set; }
		public Syntax Parent { get; set; }
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
		private Context ctx;
		public Dictionary<KeywordType, Syntax> syntaxMap { get; set; }
		public KonohaSpace parent { get; set; }

		public KonohaSpace(Context ctx)
		{
			this.ctx = ctx;
			defineDefaultSyntax();
		}

		// sugar.c
		// static void defineDefaultSyntax(CTX, kKonohaSpace *ks)
		private void defineDefaultSyntax(){
			KDEFINE_SYNTAX[] syntaxs =
			{
				new KDEFINE_SYNTAX(){
					name = "==",
					op2 = "opEQ",
					priority_op2 = 512,
					kw = KeywordType.EQ,
					flag = SynFlag.ExprOp,
				},
				new KDEFINE_SYNTAX(){
					name = "$INT",
					ExprTyCheck = ExprTyCheck_Int,
					kw = KeywordType.TKInt,
					flag = SynFlag.ExprTerm,
				},
				new KDEFINE_SYNTAX(){
					name = "$expr",
					rule = "$expr",
					ParseStmt = ParseStmt_Expr,
					TopStmtTyCheck = TopStmtTyCheck_Expr,
					ExprTyCheck = ExprTyCheck_Expr,
					kw = KeywordType.Expr,
				},
			};
			defineSyntax(syntaxs);
		}

		// static kstatus_t KonohaSpace_eval(CTX, kKonohaSpace *ks, const char *script, kline_t uline)
		public void Eval(string script)
		{
			var tokenizer = new Tokenizer(ctx, this);
			var parser = new Parser(ctx, this);
			var tokens = tokenizer.Tokenize(script);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
		}

		// static ksyntax_t* KonohaSpace_getSyntaxRule(CTX, kKonohaSpace *ks, kArray *tls, int s, int e)
		internal Syntax GetSyntaxRule(IList<Token> tls, int s, int e)
		{
			Token tk = tls[s];
			if (tk.IsType)
			{
				tk = (s + 1 < e) ? tls[s + 1] : null;
				if (tk.Type == TokenType.SYMBOL || tk.Type == TokenType.USYMBOL)
				{
					tk = (s + 2 < e) ? tls[s + 2] : null;
					if (tk.Type == TokenType.AST_PARENTHESIS || tk.Keyword == KeywordType.DOT)
					{
						return GetSyntax(KeywordType.StmtMethodDecl); //
					}
					return GetSyntax(KeywordType.StmtTypeDecl);  //
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

		public void SYN_setExprTyCheck(KeywordType ks, StmtTyChecker checker)
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
			KonohaSpace ks = this;
			Syntax parent = null;
			while (ks != null)
			{
				if (ks.syntaxMap != null && ks.syntaxMap.ContainsKey(keyword))
				{
					parent = ks.syntaxMap[keyword];
					return parent;
				}
				ks = ks.parent;
			}
			if (isnew == true)
			{
				Console.WriteLine("creating new syntax {0} old={1}", keyword.ToString(), parent);
				if (this.syntaxMap == null)
				{
					this.syntaxMap = new Dictionary<KeywordType, Syntax>();
				}

				this.syntaxMap[keyword] = new Syntax();

				if (parent != null)
				{  // TODO: RCGC
					this.syntaxMap[keyword] = parent;
				}
				else
				{
					var syn = new Syntax()
					{
						KeyWord = keyword,
						Type = KonohaType.Unknown,
						Op1 = null,
						Op2 = null,
						ParseExpr = ctx.kmodsugar.UndefinedParseExpr,
						TopStmtTyCheck = ctx.kmodsugar.UndefinedStmtTyCheck,
						StmtTyCheck = ctx.kmodsugar.UndefinedStmtTyCheck,
						ExprTyCheck = ctx.kmodsugar.UndefinedExprTyCheck,
					};
					this.syntaxMap[keyword] = syn;
				}
				this.syntaxMap[keyword].Parent = parent;
				return this.syntaxMap[keyword];
			}
			return null;
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
			if(t[0] == opench && t[1] == 0) {
				int ne = findTopCh(tls, i+1, e, tk.Type, closech);
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

		private bool makeSyntaxRule(IList<Token> tls, int s, int e, out List<Token> adst)
		{
			int i;
			adst = new List<Token>();
			int nameid = 0;
			for(i = s; i < e; i++) {
				Token tk = tls[i];
				if(tk.Type == TokenType.INDENT) continue;
				if(tk.Type == TokenType.TEXT /*|| tk.Type == TK_STEXT*/) {
					if(checkNestedSyntax(tls, ref i, e, TokenType.AST_PARENTHESIS, '(', ')') ||
						checkNestedSyntax(tls, ref i, e, TokenType.AST_BRANCET, '[', ']') ||
						checkNestedSyntax(tls, ref i, e, TokenType.AST_BRACE, '{', '}')) {
					}
					else {
						tk.Type = TokenType.CODE;
						tk.Keyword = ctx.kmodsugar.keyword_(tk.Text, Symbol.NewID).Type;
					}
					adst.Add(tk);
					continue;
				}
				if(tk.Type == TokenType.SYMBOL) {
					if(i > 0 && tls[i-1].TopChar == '$') {
						var name = string.Format("${0}", tk.Text);
						tk.Keyword = ctx.kmodsugar.keyword_(name, Symbol.NewID).Type;
						tk.Type = TokenType.METANAME;
						if(nameid == 0) nameid = (int)tk.Keyword;
						tk.nameid = new Symbol(); //TODO nameid;
						nameid = 0;
						adst.Add(tk);
						continue;
					}
					if(i + 1 < e && tls[i+1].TopChar == ':') {
						Token tk2 = tls[i];
						nameid = (int)ctx.kmodsugar.keyword_(tk2.Text, Symbol.NewID).Type;
						i++;
						continue;
					}
				}
				if(tk.Type == TokenType.OPERATOR) {
					if(checkNestedSyntax(tls, ref i, e, TokenType.AST_OPTIONAL, '[', ']')) {
						adst.Add(tk);
						continue;
					}
					if(tls[i].TopChar == '$') continue;
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
			foreach(var syndef in syndefs) {
				KeywordType kw = ctx.kmodsugar.keyword_(syndef.name, Symbol.NewID).Type;
				Syntax syn = GetSyntax(kw, true);
				//syn.token = syndef.name;
				syn.Flag  |= syndef.flag;
				//if(syndef.type != null) {
				//    syn.Type = syndef.type;
				//}
				//if(syndef.op1 != null) {
				//    syn.Op1 = null;// syndef.op1;//Symbol.Get(ctx, syndef.op1, Symbol.NewID, SymPol.MsETHOD);
				//}
				//if(syndef.op2 != null) {
				//    syn.Op2 = null;// syndef.op2;//Symbol.Get(ctx, syndef.op2, Symbol.NewID, SymPol.MsETHOD);
				//}
				//if(syndef.priority_op2 > 0) {
				//    syn.priority = syndef.priority_op2;
				//}
				if(syndef.rule != null) {
					List<Token> adst;
					parseSyntaxRule(syndef.rule, new LineInfo(0, ""), out adst);
					syn.SyntaxRule = adst;
				}
				syn.ParseStmt = syndef.ParseStmt;
				syn.ParseExpr = syndef.ParseExpr;
				syn.TopStmtTyCheck = syndef.TopStmtTyCheck;
				syn.StmtTyCheck = syndef.StmtTyCheck;
				syn.ExprTyCheck = syndef.ExprTyCheck;
				if(syn.ParseExpr == ctx.kmodsugar.UndefinedParseExpr) {
					if(syn.Flag == SynFlag.ExprOp) {
						syn.ParseExpr = ctx.kmodsugar.ParseExrp_Op;
					}
					else if (syn.Flag == SynFlag.ExprTerm)
					{
						syn.ParseExpr = ctx.kmodsugar.ParseExpr_Term;
					}
				}
			}
			//Console.WriteLine("syntax size={0}, hmax={1}", syntaxMap.Count, syntaxMap.);
		}



		public void ExprTyCheck_Int(KStatement stmt, Syntax syn, KGamma gma)
		{
			Console.WriteLine("tesetsetset");
		}

		public void ExprTyCheck_Expr(KStatement stmt, Syntax syn, KGamma gma)
		{
			Console.WriteLine("tesetsetset");
		}
		public void ParseStmt_Expr(KStatement stmt, Syntax syn, KGamma gma)
		{
			Console.WriteLine("tesetsetset");
		}
		public void TopStmtTyCheck_Expr(KStatement stmt, Syntax syn, KGamma gma)
		{
			Console.WriteLine("tesetsetset");
		}
	}
}
