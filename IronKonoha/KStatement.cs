using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha
{

	public enum StmtType
	{
		UNDEFINED,
		ERR,
		EXPR,
		BLOCK,
		RETURN,
		IF,
		LOOP,
		JUMP,
	}

	[System.Diagnostics.DebuggerDisplay("{map}")]
	public class KStatement : ExprOrStmt
	{
		public Syntax syn { get; set; }
		public LineInfo ULine { get; set; }
		public KonohaSpace ks { get; set; }
		public BlockExpr parent { get; set; }
		public StmtType build { get; set; }
		public Dictionary<KeywordType, bool> annotation { get; private set; }
		public Dictionary<object, KonohaExpr> map { get; private set; }
		public bool isERR { get { return build == StmtType.ERR; } }
		public KFunc MethodFunc { get; set; }
		public bool TyCheckDone { get; set; }

		public KStatement(LineInfo line, KonohaSpace ks)
		{
			this.ULine = line;
			this.ks = ks;
			annotation = new Dictionary<KeywordType, bool>();
			map = new Dictionary<object, KonohaExpr>();
		}

		// static kbool_t Stmt_parseSyntaxRule(CTX, kStmt *stmt, kArray *tls, int s, int e)
		public bool parseSyntaxRule(Context ctx, IList<Token> tls, int s, int e)
		{
			bool ret = false;
			Syntax syn = this.ks.GetSyntaxRule(tls, s, e);
			//Debug.Assert(syn != null);
			if (syn != null && syn.SyntaxRule != null)
			{
				this.syn = syn;
				ret = (matchSyntaxRule(ctx, syn.SyntaxRule, this.ULine, tls, s, e, false) != -1);
			}
			else
			{
				ctx.SUGAR_P(ReportLevel.ERR, this.ULine, 0, "undefined syntax rule for '{0}'", syn.KeyWord.ToString());
			}
			return ret;
		}

		// static int matchSyntaxRule(CTX, kStmt *stmt, kArray *rules, kline_t /*parent*/uline, kArray *tls, int s, int e, int optional)
		public int matchSyntaxRule(Context ctx, IList<Token> rules, LineInfo /*parent*/uline, IList<Token> tls, int s, int e, bool optional)
		{
			int ri, ti, rule_size = rules.Count;
			ti = s;
			for (ri = 0; ri < rule_size && ti < e; ri++)
			{
				Token rule = rules[ri];
				Token tk = tls[ti];
				uline = tk.ULine;
				Console.WriteLine("matching rule={0},{1},{2} token={3},{4},{5}", ri, rule.Type, rule.Keyword, ti - s, tk.Type, tk.Text);
				if (rule.Type == TokenType.CODE)
				{
					if (rule.Keyword != tk.Keyword)
					{
						if (optional)
						{
							return s;
						}
						tk.Print(ctx, ReportLevel.ERR, "{0} needs '{1}'", this.syn.KeyWord, rule.Keyword);
						return -1;
					}
					ti++;
					continue;
				}
				else if (rule.Type == TokenType.METANAME)
				{
					Syntax syn = this.ks.GetSyntax(rule.Keyword);
					if (syn == null || syn.PatternMatch == null)
					{
						tk.Print(ctx, ReportLevel.ERR, "unknown syntax pattern: {0}", rule.Keyword);
						return -1;
					}
					int c = e;
					if (ri + 1 < rule_size && rules[ri + 1].Type == TokenType.CODE)
					{
						c = lookAheadKeyword(tls, ti + 1, e, rules[ri + 1]);
						if (c == -1)
						{
							if (optional)
							{
								return s;
							}
							tk.Print(ctx, ReportLevel.ERR, "{0} needs '{1}'", this.syn.KeyWord, rule.Keyword);
							return -1;
						}
						ri++;
					}
					int err_count = ctx.ctxsugar.err_count;
					int next = ParseStmt(ctx, syn, rule.nameid, tls, ti, c);
					Console.WriteLine("matched '{0}' nameid='{1}', next={2}=>{3}", rule.Keyword, rule.nameid.Name, ti, next);
					if (next == -1)
					{
						if (optional)
						{
							return s;
						}
						if (err_count == ctx.sugarerr_count)
						{
							tk.Print(ctx, ReportLevel.ERR, "unknown syntax pattern: {0}", this.syn.KeyWord, rule.Keyword, tk.Text);
						}
						return -1;
					}
					////XXX Why???
					//optional = 0;
					ti = (c == e) ? next : c + 1;
					continue;
				}
				else if (rule.Type == TokenType.AST_OPTIONAL)
				{
					int next = matchSyntaxRule(ctx, rule.Sub, uline, tls, ti, e, true);
					if (next == -1)
					{
						return -1;
					}
					ti = next;
					continue;
				}
				else if (rule.Type == TokenType.AST_PARENTHESIS || rule.Type == TokenType.AST_BRACE || rule.Type == TokenType.AST_BRACKET)
				{
					if (tk.Type == rule.Type && rule.TopChar == tk.TopChar)
					{
						int next = matchSyntaxRule(ctx, rule.Sub, uline, tk.Sub, 0, tk.Sub.Count, false);
						if (next == -1)
						{
							return -1;
						}
						ti++;
					}
					else
					{
						if (optional)
						{
							return s;
						}
						tk.Print(ctx, ReportLevel.ERR, "{0} needs '{1}'", this.syn.KeyWord, rule.TopChar);
						return -1;
					}
				}
			}
			if (!optional)
			{
				for (; ri < rules.Count; ri++)
				{
					Token rule = rules[ri];
					if (rule.Type != TokenType.AST_OPTIONAL)
					{
						ctx.SUGAR_P(ReportLevel.ERR, uline, -1, "{0} needs syntax pattern: {1}", this.syn.KeyWord, rule.Keyword);
						return -1;
					}
				}
				//WARN_Ignored(_ctx, tls, ti, e);
			}
			return ti;
		}

		// static int ParseStmt(CTX, ksyntax_t *syn, kStmt *stmt, ksymbol_t name, kArray *tls, int s, int e)
		public int ParseStmt(Context ctx, Syntax syn, Symbol name, IList<Token> tls, int s, int e)
		{
			//Console.WriteLine("ParseStmt {0}, {0}", name.Name, tls[s].Text);
			return syn.PatternMatch(ctx, this, syn, name, tls, s, e);
		}

		// static kExpr *ParseExpr(CTX, ksyntax_t *syn, kStmt *stmt, kArray *tls, int s, int c, int e)
		public KonohaExpr ParseExpr(Context ctx, Syntax syn, IList<Token> tls, int s, int c, int e)
		{
			Debug.Assert(syn != null);
			if (syn.ParseExpr != null)
			{
				return syn.ParseExpr(ctx, syn, this, tls, s, c, e);
			}
			return KModSugar.UndefinedParseExpr(ctx, syn, this, tls, s, c, e);
		}

		// static kExpr* Stmt_newExpr2(CTX, kStmt *stmt, kArray *tls, int s, int e)
		public KonohaExpr newExpr2(Context ctx, IList<Token> tls, int s, int e)
		{
			if(s < e) {
				Syntax syn = null;
				int idx = findBinaryOp(ctx, tls, s, e, ref syn);
				if(idx != -1) {
					Console.WriteLine("** Found BinaryOp: s={0}, idx={1}, e={2}, '{3}' **", s, idx, e, tls[idx].Text);
					return ParseExpr(ctx, syn, tls, s, idx, e);
				}
				int c = s;
				syn = ks.GetSyntax(tls[c].Keyword);
				Debug.Assert(syn != null);
				return ParseExpr(ctx, syn, tls, c, c, e);
			}
			else {
				if (0 < s - 1) {
					ctx.SUGAR_P(ReportLevel.ERR, ULine, -1, "expected expression after {0}", tls[s-1].Text);
				}
				else if(e < tls.Count) {
					ctx.SUGAR_P(ReportLevel.ERR, ULine, -1, "expected expression before {0}", tls[e].Text);
				}
				else {
					ctx.SUGAR_P(ReportLevel.ERR, ULine, 0, "expected expression");
				}
				return null;
			}
		}

		//static int Stmt_findBinaryOp(CTX, kStmt *stmt, kArray *tls, int s, int e, ksyntax_t **synRef)
		int findBinaryOp(Context ctx, IList<Token> tls, int s, int e, ref Syntax synRef)
		{
			int idx = -1;
			int prif = 0;
			for(int i = skipUnaryOp(ctx, tls, s, e) + 1; i < e; i++) {
				Token tk = tls[i];
				Syntax syn = ks.GetSyntax(tk.Keyword);
		//		if(syn != NULL && syn->op2 != 0) {
				if(syn.priority > 0) {
					if (prif < syn.priority || (prif == syn.priority && syn.Flag != SynFlag.ExprLeftJoinOp2))
					{
						prif = syn.priority;
						idx = i;
						synRef = syn;
					}
					if(syn.Flag != SynFlag.ExprPostfixOp2) {  /* check if real binary operator to parse f() + 1 */
						i = skipUnaryOp(ctx, tls, i+1, e) - 1;
					}
				}
			}
			return idx;
		}

		// static int Stmt_skipUnaryOp(CTX, kStmt *stmt, kArray *tls, int s, int e)
		int skipUnaryOp(Context ctx, IList<Token> tls, int s, int e)
		{
			int i;
			for(i = s; i < e; i++) {
				Token tk = tls[i];
				if(!isUnaryOp(ctx, tk)) {
					break;
				}
			}
			return i;
		}

		bool isUnaryOp(Context ctx, Token tk)
		{
			Syntax syn = ks.GetSyntax(tk.Keyword);
			return syn != null && syn.Op1 != null;
		}

		// static int lookAheadKeyword(kArray *tls, int s, int e, kToken *rule)
		public int lookAheadKeyword(IList<Token> tls, int s, int e, Token rule)
		{
			int i;
			for (i = s; i < e; i++)
			{
				Token tk = tls[i];
				if (rule.Keyword == tk.Keyword)
					return i;
			}
			return -1;
		}

		// static int Stmt_addAnnotation(CTX, kStmt *stmt, kArray *tls, int s, int e)
		public int addAnnotation(Context ctx, IList<Token> tls, int s, int e)
		{
			int i;
			for (i = s; i < e; i++)
			{
				Token tk = tls[i];
				if (tk.Type != TokenType.METANAME)
					break;
				if (i + 1 < e)
				{
					var kw = ctx.kmodsugar.keyword_("@" + tk.Text, Symbol.NewID).Type;
					Token tk1 = tls[i + 1];
					KonohaExpr value = null;
					if (tk1.Type == TokenType.AST_PARENTHESIS)
					{
						value = this.newExpr2(ctx, tk1.Sub, 0, tk1.Sub.Count);
						i++;
					}
					if (value != null)
					{
						annotation[kw] = true;
					}
				}
			}
			return i;
		}

		// static kExpr *Stmt_addExprParams(CTX, kStmt *stmt, kExpr *expr, kArray *tls, int s, int e, int allowEmpty)
		public void addExprParams(Context ctx,  KonohaExpr expr, IList<Token> tls, int s, int e, bool allowEmpty)
		{
			int i, start = s;
			for(i = s; i < e; i++) {
				Token tk = tls[i];
				if(tk.Keyword == KeywordType.COMMA) {
					((ConsExpr)expr).Add(ctx, newExpr2(ctx, tls, start, i));
					start = i + 1;
				}
			}
			if(!allowEmpty || start < i) {
				((ConsExpr)expr).Add(ctx, newExpr2(ctx, tls, start, i));
			}
			//kArray_clear(tls, s);
			//return expr;
		}

		// Stmt_toERR
		public void toERR(Context ctx, uint estart)
		{
			this.syn = ks.GetSyntax(KeywordType.Err);
			this.build = StmtType.ERR;
			//kObject_setObject(stmt, KW_Err, kstrerror(eno));
		}

		internal bool TyCheck(Context ctx, KGamma gma)
		{
			var fo = gma.isTopLevel ? syn.TopStmtTyCheck : syn.StmtTyCheck;
			bool result;
			Debug.Assert(fo != null);
			StmtTyChecker[] a = fo.GetInvocationList() as StmtTyChecker[];
			if (a != null && a.Length > 1)
			{ // @Future
				for (int i = a.Length - 1; i > 0; --i)
				{
					result = a[i](this, this.syn, gma);
					if (syn == null) return true;
					if (build != StmtType.UNDEFINED) return result;
				}
				fo = a[0];
			}
			result = fo(this, this.syn, gma);
			if (/*this.syn == null*/ this.TyCheckDone) return true; // this means done;
			if (result == false && this.build == StmtType.UNDEFINED)
			{
				ctx.SUGAR_P(ReportLevel.ERR, this.ULine, 0, "statement typecheck error: {0}", syn.KeyWord);
			}
			return result;
		}

		public KType getcid(KeywordType kw, KType defcid)
		{
			if (!this.map.ContainsKey(kw))
			{
				return defcid;
			}
			else
			{
				var tk = this.map[kw];
				Debug.Assert(tk.tk.IsType);
				return tk.tk.KType;
			}
		}

		public void done(){
			//this.syn = null;
			this.TyCheckDone = true;
		}


		internal bool tyCheckExpr(Context ctx, KeywordType nameid, KGamma gma, KType reqty, TPOL pol)
		{
			var k = Symbol.Get(ctx, nameid);
			if (this.map.ContainsKey(k))
			{
				var expr = this.map[k];
				var texpr = expr.tyCheck(ctx, this, gma, reqty, pol);
				Console.WriteLine("reqty={0}, texpr.ty={1} isnull={2}", reqty, texpr.ty, texpr == null);
				if (texpr != null)
				{
					if (texpr != expr)
					{
						this.map[k] = texpr;
					}
					return true;
				}
			}
			return false;
		}

		internal void typed(KStatement stmt, StmtType build)
		{
			if (stmt.build != StmtType.ERR)
			{
				stmt.build = build;
			}
		}
	}

}
