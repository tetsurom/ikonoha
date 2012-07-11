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
		Import,
	}

	[System.Diagnostics.DebuggerDisplay("{map}")]
	public class KStatement : ExprOrStmt
	{
		public Syntax syn { get; set; }
		public LineInfo ULine { get; set; }
		public KNameSpace ks { get; set; }
		public BlockExpr parent { get; set; }
		public StmtType build { get; set; }
		public Dictionary<KeywordType, bool> annotation { get; private set; }
		public Dictionary<object, KonohaExpr> map { get; private set; }
		public bool isERR { get { return build == StmtType.ERR; } }
		public KFunc MethodFunc { get; set; }
		public bool TyCheckDone { get; set; }

		public KStatement(LineInfo line, KNameSpace ks)
		{
			this.ULine = line;
			this.ks = ks;
			annotation = new Dictionary<KeywordType, bool>();
			map = new Dictionary<object, KonohaExpr>();
		}

		public override string GetDebugView(int indent)
		{
			var builder = new StringBuilder();
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append(this.syn == null ? "!null" : this.syn.KeyWord.Name);
			builder.Append('{');
			foreach (var pair in map)
			{
				builder.Append(System.Environment.NewLine);
				for (int i = 1; i < indent + 4; ++i)
				{
					builder.Append(' ');
				}
				builder.Append('[');
				builder.Append(pair.Key.ToString());
				builder.Append(']');
				builder.Append(System.Environment.NewLine);
				builder.Append(pair.Value.GetDebugView(indent + 4));
			}
			builder.Append(System.Environment.NewLine);
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append('}');
			return builder.ToString();
		}


		// static kbool_t Stmt_parseSyntaxRule(CTX, kStmt *stmt, kArray *tls, int s, int e)
		public bool parseSyntaxRule(Context ctx, IList<Token> tls, int s, int e)
		{
			bool ret = false;
			Syntax syn = this.ks.GetSyntaxRule(tls, s, e);
			Debug.Assert(syn != null);
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
				Debug.WriteLine("matching rule={0},{1},{2} token={3},{4},{5}", ri, rule.TokenType, rule.Keyword, ti - s, tk.TokenType, tk.Text);
				if (rule.TokenType == TokenType.CODE)
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
				else if (rule.TokenType == TokenType.METANAME)
				{
					Syntax syn = this.ks.GetSyntax(rule.Keyword);
					if (syn == null || syn.PatternMatch == null)
					{
						tk.Print(ctx, ReportLevel.ERR, "unknown syntax pattern: {0}", rule.Keyword);
						return -1;
					}
					int c = e;
					if (ri + 1 < rule_size && rules[ri + 1].TokenType == TokenType.CODE)
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
					Debug.WriteLine("matched '{0}' nameid='{1}', next={2}=>{3}", rule.Keyword, rule.nameid.Name, ti, next);
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
				else if (rule.TokenType == TokenType.AST_OPTIONAL)
				{
					int next = matchSyntaxRule(ctx, rule.Sub, uline, tls, ti, e, true);
					if (next == -1)
					{
						return -1;
					}
					ti = next;
					continue;
				}
				else if (rule.TokenType == TokenType.AST_PARENTHESIS || rule.TokenType == TokenType.AST_BRACE || rule.TokenType == TokenType.AST_BRACKET)
				{
					if (tk.TokenType == rule.TokenType && rule.TopChar == tk.TopChar)
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
					if (rule.TokenType != TokenType.AST_OPTIONAL)
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
					Debug.WriteLine("** Found BinaryOp: s={0}, idx={1}, e={2}, '{3}' **", s, idx, e, tls[idx].Text);
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
				if (tk.TokenType != TokenType.METANAME)
					break;
				if (i + 1 < e)
				{
					var kw = ctx.kmodsugar.keyword_("@" + tk.Text).Type;
					Token tk1 = tls[i + 1];
					KonohaExpr value = null;
					if (tk1.TokenType == TokenType.AST_PARENTHESIS)
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
				if (tk.Keyword == KeyWordTable.COMMA)
				{
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
			this.syn = ks.GetSyntax(KeyWordTable.Err);
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

		public KonohaType getcid(Symbol kw, KonohaType defcid)
		{
			if (!this.map.ContainsKey(kw))
			{
				return defcid;
			}
			else
			{
				var tk = this.map[kw];
				Debug.Assert(tk.tk.IsType);
				return tk.tk.Type;
			}
		}

		public void done(){
			//this.syn = null;
			this.TyCheckDone = true;
		}


		internal bool tyCheckExpr(Context ctx, KeywordType nameid, KGamma gma, KonohaType reqty, TPOL pol)
		{
			var k = Symbol.Get(ctx, nameid);
			if (this.map.ContainsKey(k))
			{
				var expr = this.map[k];
				var texpr = expr.tyCheck(ctx, this, gma, reqty, pol);
				Debug.WriteLine("reqty={0}, texpr.ty={1} isnull={2}", reqty.Name, texpr == null ? null : texpr.ty.Name, texpr == null);
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

		internal void typed(StmtType build)
		{
			if (this.build != StmtType.ERR)
			{
				this.build = build;
			}
		}

		/// <summary>
		/// return stmt.map[kw] as BlockExpr
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="kw"></param>
		/// <returns></returns>
		public BlockExpr Block(Context ctx, KeywordType kw)
		{
			return Expr(ctx, kw, null) as BlockExpr;
		}
		/// <summary>
		/// return stmt.map[kw]
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="kw"></param>
		/// <returns></returns>
		public KonohaExpr Expr(Context ctx, KeywordType kw){
			return Expr(ctx, kw, null);
		}
		public KonohaExpr Expr(Context ctx, KeywordType kw, KonohaExpr def)
		{
			var sym = Symbol.Get(ctx, kw);
			if(this.map.ContainsKey(sym)){
				return this.map[sym];
			}
			return def;
		}
		/// <summary>
		/// return stmt.map[kw].tk
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="kw"></param>
		/// <returns></returns>
		public Token Token(Context ctx, KeywordType kw){
			return Token(ctx, kw, null);
		}
		public Token Token(Context ctx, KeywordType kw, Token def)
		{
			var e = Expr(ctx, kw, null);
			if(e != null){
				return e.tk;
			}
			return def;
		}

		//static kStmt* Stmt_lookupIfStmtWithoutElse(CTX, kStmt *stmt)
		public KStatement LookupIfStmtWithoutElse(Context ctx)
		{
			var bkElse = this.Block(ctx, KeywordType.Else);
			if(bkElse != null) {
				if(bkElse.blocks.Count() == 1) {
					var stmtIf = bkElse.blocks[0];
					if(stmtIf.syn.KeyWord.Type == KeywordType.If) {
						return stmtIf.LookupIfStmtWithoutElse(ctx);
					}
				}
				return null;
			}
			return this;
		}

		//static kStmt* Stmt_lookupIfStmtNULL(CTX, kStmt *stmt)
		public KStatement LookupIfStmt(Context ctx)
		{
			var bka = this.parent.blocks;
			KStatement prevIfStmt = null;
			foreach(var s in bka) {
				if(s == this) {
					if(prevIfStmt != null) {
						return prevIfStmt.LookupIfStmtWithoutElse(ctx);
					}
					return null;
				}
				if(s.TyCheckDone) continue;  // this is done
				prevIfStmt = (s.syn.KeyWord.Type == KeywordType.If) ? s : null;
			}
			return null;
		}
		 
		//static kBlock* Stmt_block(CTX, kStmt *stmt, keyword_t kw, kBlock *def)
		public BlockExpr toBlock(Context ctx, KeywordType kw, BlockExpr def)
		{
			BlockExpr bk = Expr(ctx, kw) as BlockExpr;
			if(bk != null) {
				var tk = bk.tk;
				if (tk.TokenType == TokenType.CODE) {
					tk.toBrace(ctx, ks);
				}
				if (tk.TokenType == TokenType.AST_BRACE) {
					var parser = new Parser(ctx, ks);
					bk = parser.CreateBlock(null, tk.Sub, 0, tk.Sub.Count(), ';');
					this.map[Symbol.Get(ctx, kw)] = bk;
				}
				return bk;
			}
			return def;
		}

	}
}
