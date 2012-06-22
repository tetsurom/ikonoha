using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace IronKonoha
{
	public abstract class ExprOrStmt : KObject
	{

	}

	public abstract class KonohaExpr : ExprOrStmt
	{
		public ExprOrStmt parent { get; set; }
		/// <summary>
		/// 目的不明
		/// </summary>
		public Token tk { get; set; }
		public Syntax syn { get; set; }

		public KonohaExpr()
		{

		}

		public override string ToString()
		{
			if (tk != null)
			{
				return tk.ToString();
			}
			return string.Empty;
		}

		// tycheck.h
		// static kExpr *Expr_tyCheck(CTX, kStmt *stmt, kExpr *expr, kGamma *gma, ktype_t reqty, int pol)
		internal KonohaExpr tyCheck(Context ctx, KStatement stmt, KGamma gma, Type reqty, TPOL pol)
		{
			var texpr = this;
			if (stmt.isERR) texpr = null;
			if (this.ty == typeof(Variant))
			{
				ExprTyChecker fo = syn.ExprTyCheck;
				Debug.Assert(fo != null);
				ExprTyChecker[] a = fo.GetInvocationList() as ExprTyChecker[];
				if (a != null && a.Length > 1)
				{ // @Future
					for (int i = a.Length - 1; i > 0; --i)
					{
						texpr = a[i](stmt, this, gma, reqty);
						if (stmt.isERR) return null;
						if (texpr.ty != typeof(Variant)) return texpr;
					}
					fo = a[0];
				}
				texpr = fo(stmt, this, gma, reqty);
			}
			if (stmt.isERR) texpr = null;
			if (texpr != null)
			{
				//DBG_P("type=%s, reqty=%s", TY_t(expr->ty), TY_t(reqty));
				if (texpr.ty == typeof(void))
				{
					if ((pol & TPOL.ALLOWVOID) == 0)
					{
						//texpr = kExpr_p(stmt, expr, ERR_, "void is not acceptable")
						ctx.SUGAR_P(ReportLevel.ERR, new LineInfo(0, ""), 0, "void is not acceptable");
					}
					//return texpr;
					return null;
				}
				if (reqty == BCID.CLASS_Tvar || texpr.ty == reqty || (pol & TPOL.NOCHECK) != 0)
				{
					return texpr;
				}
				if (texpr.ty == reqty)
				{
					//if (ctx.CT_(texpr.ty).isUnbox && !ctx.CT_(reqty).isUnbox)
					//{
					//	return KonohaExpr.BoxingExpr(ctx, this, reqty);
					//}
					return texpr;
				}
				var mtd = gma.ks.getCastMethod(texpr.ty, reqty);
				Debug.WriteLine("finding cast {0} => {1}: {2}", texpr.ty, reqty, mtd);
				if (mtd != null /*&& (mtd.isCoercion || (pol & TPOL.COERCION) != 0)*/)
				{
					return KonohaExpr.TypedMethodCall(ctx, stmt, reqty, mtd, gma, 1, texpr);
				}
				//return kExpr_p(stmt, expr, ERR_, "%s is requested, but %s is given", TY_t(reqty), TY_t(texpr->ty));
				return null;
			}
			return texpr;
		}

		private static KonohaExpr TypedMethodCall(Context ctx, KStatement stmt, Type reqty, MethodInfo mtd, KGamma gma, int p, KonohaExpr texpr)
		{
			var expr = new ConsExpr(ctx, stmt.syn);
			expr.Cons.Add(mtd);
			throw new NotImplementedException();
		}

		private static KonohaExpr BoxingExpr(Context ctx, KonohaExpr konohaExpr, Type reqty)
		{
			throw new NotImplementedException();
		}

		public Type ty { get; set; }
	}

	[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
	public class ConsExpr : KonohaExpr
	{
		public IList<object> Cons { get; private set; }

		public ConsExpr(Context ctx, Syntax syn, params object[] param)
		{
			Cons = new List<object>(param);
		}

		public void Add(Context ctx, KonohaExpr expr)
		{
			if (expr != null)
			{
				Cons.Add(expr);
			}
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.Append("[");
			foreach (var con in Cons)
			{
				builder.Append(con.ToString());
				builder.Append(", ");
			}
			builder.Remove(builder.Length - 2, 2);
			builder.Append("]");

			return builder.ToString();
		}
	}
	[System.Diagnostics.DebuggerDisplay("{tk.Text} [{tk.Type}]")]
	public class TermExpr : KonohaExpr
	{

	}

	[System.Diagnostics.DebuggerDisplay("{tk.Text} [{tk.Type}]")]
	public class CodeExpr : KonohaExpr
	{
		public CodeExpr(Token tk)
		{
			this.tk = tk;
		}
	}

	[System.Diagnostics.DebuggerDisplay("{tk.Text} [{tk.Type}]")]
	public class SingleTokenExpr : KonohaExpr
	{
		public SingleTokenExpr(Token tk)
		{
			this.tk = tk;
		}
	}

	public class ConstExpr<T> : KonohaExpr
	{
		public ConstExpr(T data)
		{
			Data = data;
		}
		public T Data { get; set; }
	}

	public class BlockExpr : KonohaExpr
	{
		/// <summary>
		/// KStatement list which this block contains.
		/// </summary>
		public List<KStatement> blocks = new List<KStatement>();
		/// <summary>
		/// ?
		/// </summary>
		public KonohaExpr esp { get; set; }

		// static void Block_addStmtLine(CTX, kBlock *bk, kArray *tls, int s, int e, kToken *tkERR)
		public void AddStatementLine(Context ctx, KonohaSpace ks, IList<Token> tokens, int start, int end, out Token tkERR)
		{
			tkERR = null;
			KStatement stmt = new KStatement(tokens[start].ULine, ks);//new_W(Stmt, tls->toks[s]->uline);
			blocks.Add(stmt);
			stmt.parent = this;
			uint estart = ctx.KErrorNo;
			start = stmt.addAnnotation(ctx, tokens, start, end);
			if (!stmt.parseSyntaxRule(ctx, tokens, start, end))
			{
				stmt.toERR(ctx, estart);
				throw new ArgumentException("undefined syntax rule for");
			}
			Debug.Assert(stmt.syn != null);
		}

		// tycheck.h
		// static kbool_t Block_tyCheckAll(CTX, kBlock *bk, kGamma *gma)
		public bool TyCheckAll(Context ctx, KGamma gma)
		{
			bool result = true;
			int lvarsize = gma.lvar.Count;
			for (int i = 0; i < blocks.Count; i++)
			{
				var stmt = blocks[i];
				var syn = stmt.syn;
				//dumpStmt(_ctx, stmt);
				if (syn == null) continue; /* This means 'done' */
				if (stmt.isERR || !stmt.TyCheck(ctx, gma))
				{
					Debug.Assert(stmt.isERR);
					gma.setERROR(true);
					result = false;
					break;
				}
			}
			//kExpr_setVariable(this.esp, LOCAL_, KType.Void, gma.lvar.Count, gma);
			while (lvarsize < gma.lvar.Count)
			{
				gma.lvar.Pop();
			}
			return result;
		}
	}
}
