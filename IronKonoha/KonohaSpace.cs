using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha
{
	/*
    typedef const struct _ksyntax ksyntax_t;
    struct _ksyntax {
	    keyword_t kw;  kflag_t flag;
	    kArray   *syntaxRuleNULL;
	    kMethod  *ParseStmtNULL;
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
		/// <summary>
		/// 文法の優先度？ 
		/// </summary>
		public int priority { get; set; }

		public KonohaType Type { get; set; }

		public KMethod ParseStmtnull { get; set; }

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
	    const struct _kKonohaSpace   *parentNULL;
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

		public KonohaSpace (Context ctx)
		{
			this.ctx = ctx;
		}

		// static kstatus_t KonohaSpace_eval(CTX, kKonohaSpace *ks, const char *script, kline_t uline)
		public void Eval (string script)
		{
			var tokenizer = new Tokenizer (ctx, this);
			var parser = new Parser (ctx, this);
			var tokens = tokenizer.Tokenize (script);
			var block = parser.CreateBlock (null, tokens, 0, tokens.Count (), ';');
		}
        
		// static ksyntax_t* KonohaSpace_getSyntaxRule(CTX, kKonohaSpace *ks, kArray *tls, int s, int e)
		internal Syntax GetSyntaxRule (IList<Token> tls, int s, int e)
		{
			Token tk = tls [s];
			if (tk.IsType) {
				tk = (s + 1 < e) ? tls [s + 1] : null;
				if (tk.Type == TokenType.SYMBOL || tk.Type == TokenType.USYMBOL) {
					tk = (s + 2 < e) ? tls [s + 2] : null;
					if (tk.Type == TokenType.AST_PARENTHESIS || tk.KeyWord == KeywordType.DOT) {
						return GetSyntax (KeywordType.StmtMethodDecl); //
					}
					return GetSyntax (KeywordType.StmtTypeDecl);  //
				}
				return GetSyntax (KeywordType.Expr);  // expression
			}
			Syntax syn = GetSyntax (tk.KeyWord);

			if (syn == null || syn.SyntaxRule == null) {
				//wDBG_P("kw='%s', %d, %d", T_kw(syn.KeyWord), syn.ParseExpr == kmodsugar.UndefinedParseExpr, kmodsugar.UndefinedExprTyCheck == syn.ExprTyCheck);
				int i;
				for (i = s + 1; i < e; i++) {
					tk = tls [i];
					syn = GetSyntax (tk.KeyWord);
					if (syn.SyntaxRule != null && syn.priority > 0) {
						ctx.SUGAR_P (ReportLevel.DEBUG, tk.ULine, tk.Lpos, "binary operator syntax kw='%s'", syn.KeyWord.ToString ());   // sugar $expr "=" $expr;
						return syn;
					}
				}
				return GetSyntax (KeywordType.Expr);
			}
			return syn;
		}

		public delegate void StmtTyChecker (KonohaStatement stmt,Syntax syn,KGamma gma);

		public void SYN_setTopStmtTyCheck (KeywordType ks, StmtTyChecker checker)
		{
			var syn = GetSyntax (ks, true);
			syn.TopStmtTyCheck = new KMethod ();//checker;
		}

		public void SYN_setStmtTyCheck (KeywordType ks, StmtTyChecker checker)
		{
			var syn = GetSyntax (ks, true);
			syn.StmtTyCheck = new KMethod ();//checker;
		}

		public void SYN_setExprTyCheck (KeywordType ks, StmtTyChecker checker)
		{
			var syn = GetSyntax (ks, true);
			syn.ExprTyCheck = new KMethod ();//checker;
		}

		internal Syntax GetSyntax (KeywordType keyword)
		{
			return GetSyntax (keyword, true);
			//return GetSyntax(keyword, false);
		}
        
		//KonohaSpace_syntax
		internal Syntax GetSyntax (KeywordType keyword, bool isnew)
		{
			KonohaSpace ks = this;
			Syntax parent = null;
			while (ks != null) {
				if (ks.syntaxMap != null) {
					if (ks.syntaxMap.ContainsKey (keyword)) {
						parent = ks.syntaxMap [keyword];
					}
				}
				ks = ks.parentNULL;
			}
			if (isnew == true) {
				//DBG_P("creating new syntax %s old=%p", T_kw(kw), parent);
				if (this.syntaxMap == null) {
					this.syntaxMap = new Dictionary<KeywordType, Syntax> ();
				}

				this.syntaxMap [keyword] = new Syntax ();

				if (parent != null) {  // TODO: RCGC
					this.syntaxMap [keyword] = parent;
				} else {
					var syn = new Syntax ()
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
					this.syntaxMap [keyword] = syn;
				}
				this.syntaxMap [keyword].Parent = parent;
				return this.syntaxMap [keyword];
			}
			return null;
		}

		public Dictionary<KeywordType, Syntax> syntaxMap { get; set; }

		public KonohaSpace parentNULL { get; set; }
	}
}
