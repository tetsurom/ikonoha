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
		SYNFLAG_ExprTerm = 1,
		SYNFLAG_ExprOp = 1 << 1,
		SYNFLAG_ExprLeftJoinOp2 = 1 << 2,
		SYNFLAG_ExprPostfixOp2 = 1 << 3,
		SYNFLAG_StmtBreakExec = 1 << 8,
		SYNFLAG_StmtJumpAhead = 1 << 9,
		SYNFLAG_StmtJumpSkip = 1 << 10,
	}

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
		public KMethod ParseStmt { get; set; }
		public KMethod ParseExpr { get; set; }
		public KMethod TopStmtTyCheck { get; set; }
		public KMethod StmtTyCheck { get; set; }
		public KMethod ExprTyCheck { get; set; }
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

		public delegate void StmtTyChecker(KStatement stmt, Syntax syn, KGamma gma);

		public void SYN_setTopStmtTyCheck(KeywordType ks, StmtTyChecker checker)
		{
			var syn = GetSyntax(ks, true);
			syn.TopStmtTyCheck = new KMethod();//checker;
		}

		public void SYN_setStmtTyCheck(KeywordType ks, StmtTyChecker checker)
		{
			var syn = GetSyntax(ks, true);
			syn.StmtTyCheck = new KMethod();//checker;
		}

		public void SYN_setExprTyCheck(KeywordType ks, StmtTyChecker checker)
		{
			var syn = GetSyntax(ks, true);
			syn.ExprTyCheck = new KMethod();//checker;
		}

		internal Syntax GetSyntax(KeywordType keyword)
		{
			return GetSyntax(keyword, true);
			//return GetSyntax(keyword, false);
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

		// struct.h
		// static void KonohaSpace_defineSyntax(CTX, kKonohaSpace *ks, KDEFINE_SYNTAX *syndef)
		public void defineSyntax(KDEFINE_SYNTAX syndef)
		{
			KMethod pParseStmt = null, pParseExpr = null, pStmtTyCheck = null, pExprTyCheck = null;
			KMethod mParseStmt = null, mParseExpr = null, mStmtTyCheck = null, mExprTyCheck = null;
			while(syndef.name != null) {
				KeywordType kw = ctx.kmodsugar.keyword_(syndef.name, Symbol.NewID).Type;
				Syntax syn = GetSyntax(kw, true);
				//syn.token = syndef.name;
				syn.Flag  |= syndef.flag;
				/*
				if(syndef.type != 0) {
					syn.Type = syndef.type;
				}
				if(syndef.op1 != null) {
					syn.Op1 = ksymbol(syndef.op1, 127, FN_NEWID, SYMPOL_METHOD);
				}
				if(syndef.op2 != null) {
					syn.Op2 = ksymbol(syndef.op2, 127, FN_NEWID, SYMPOL_METHOD);
				}
				if(syndef.priority_op2 > 0) {
					syn.priority = syndef.priority_op2;
				}
				if(syndef.rule != null) {
					parseSyntaxRule(_ctx, syndef.rule, 0, syn.syntaxRulenull);
				}
				setSyntaxMethod(_ctx, syndef.ParseStmt, &(syn.ParseStmtnull), &pParseStmt, &mParseStmt);
				setSyntaxMethod(_ctx, syndef.ParseExpr, &(syn.ParseExpr), &pParseExpr, &mParseExpr);
				setSyntaxMethod(_ctx, syndef.TopStmtTyCheck, &(syn.TopStmtTyCheck), &pStmtTyCheck, &mStmtTyCheck);
				setSyntaxMethod(_ctx, syndef.StmtTyCheck, &(syn.StmtTyCheck), &pStmtTyCheck, &mStmtTyCheck);
				setSyntaxMethod(_ctx, syndef.ExprTyCheck, &(syn.ExprTyCheck), &pExprTyCheck, &mExprTyCheck);
				if(syn.ParseExpr == kmodsugar.UndefinedParseExpr) {
					if(FLAG_is(syn.flag, SYNFLAG_ExprOp)) {
						KSETv(syn.ParseExpr, kmodsugar.ParseExpr_Op);
					}
					else if(FLAG_is(syn.flag, SYNFLAG_ExprTerm)) {
						KSETv(syn.ParseExpr, kmodsugar.ParseExpr_Term);
					}
				}
				DBG_ASSERT(syn == SYN_(ks, kw));
				syndef++;
				*/
			}
			Console.WriteLine("syntax size={0}, hmax={1}", ks.syntaxMapNN.size, ks.syntaxMapNN.hmax);
		}


	}
}
