﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha
{
	public class KStatement : KObject
	{
		public Syntax syn { get; set; }
		public LineInfo ULine { get; set; }
		public KonohaSpace ks { get; set; }
		public BlockExpr parent { get; set; }
		public ushort build { get; set; }

		public KStatement(LineInfo line, KonohaSpace ks)
		{
			this.ULine = line;
			this.ks = ks;
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
				//DBG_P("matching rule=%d,%s,%s token=%d,%s,%s", ri, T_tt(rule.Type), T_kw(rule.KeyWord), ti-s, T_tt(tk.Type), kToken_s(tk));
				if (rule.Type == TokenType.CODE)
				{
					if (rule.Keyword != tk.Keyword)
					{
						if (optional)
							return s;
						tk.Print(ctx, ReportLevel.ERR, "{0} needs '{1}'", this.syn.KeyWord, rule.Keyword);
						return -1;
					}
					ti++;
					continue;
				}
				else if (rule.Type == TokenType.METANAME)
				{
					Syntax syn = this.ks.GetSyntax(rule.Keyword);
					if (syn == null || syn.ParseStmt == null)
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
								return s;
							tk.Print(ctx, ReportLevel.ERR, "{0} needs '{1}'", this.syn.KeyWord, rule.Keyword);
							return -1;
						}
						ri++;
					}
					int err_count = ctx.ctxsugar.err_count;
					int next = ParseStmt(ctx, syn, rule.nameid, tls, ti, c);
					//			DBG_P("matched '%s' nameid='%s', next=%d=>%d", Pkeyword(rule.KeyWord), Pkeyword(rule->nameid), ti, next);
					if (next == -1)
					{
						if (optional)
							return s;
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
						return -1;
					ti = next;
					continue;
				}
				else if (rule.Type == TokenType.AST_PARENTHESIS || rule.Type == TokenType.AST_BRACE || rule.Type == TokenType.AST_BRANCET)
				{
					if (tk.Type == rule.Type && rule.TopChar == tk.TopChar)
					{
						int next = matchSyntaxRule(ctx, rule.Sub, uline, tk.Sub, 0, tk.Sub.Count, false);
						if (next == -1)
							return -1;
						ti++;
					}
					else
					{
						if (optional)
							return s;
						//kToken_p(tk, ERR_, "%s needs '%c'", T_statement(this.syn.KeyWord), rule.TopChar);
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
						//SUGAR_P(ERR_, uline, -1, "%s needs syntax pattern: %s", T_statement(this.syn.KeyWord), T_kw(rule.KeyWord));
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
			throw new NotImplementedException();
			/*
            INIT_GCSTACK();
	        BEGIN_LOCAL(lsfp, 8);
	        KSETv(lsfp[K_CALLDELTA+0].o, (kObject*)stmt);
	        lsfp[K_CALLDELTA+0].ndata = (uintptr_t)syn;
	        lsfp[K_CALLDELTA+1].ivalue = name;
	        KSETv(lsfp[K_CALLDELTA+2].a, tls);
	        lsfp[K_CALLDELTA+3].ivalue = s;
	        lsfp[K_CALLDELTA+4].ivalue = e;
	        KCALL(lsfp, 0, syn->ParseStmtNULL, 4, knull(CT_Int));
	        END_LOCAL();
	        RESET_GCSTACK();
	        return (int)lsfp[0].ivalue;
             * */
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
					string buf = "@" + tk.Text;
					// what is FN_NEWID?
					//KeywordType kw = keyword(ctx, buf, tk.Text.Length + 1, FN_NEWID);
					Token tk1 = tls[i + 1];
					object value = true;//UPCAST(K_TRUE);
					if (tk1.Type == TokenType.AST_PARENTHESIS)
					{
						//value = (kObject*)Stmt_newExpr2(_ctx, stmt, tk1.Sub, 0, tk1.Sub.Count);
						i++;
					}
					if (value != null)
					{
						//kObject_setObject(stmt, kw, value);
					}
				}
			}
			return i;
		}

		// Stmt_toERR
		public void toERR(Context ctx, uint estart)
		{
			//throw new NotImplementedException();
			this.syn = new Syntax();//ks.GetSyntax(parent, KeywordType.Err);
			//this.build = TSTMT_ERR;
			//kObject_setObject(stmt, KW_Err, kstrerror(eno));
		}
	}

}