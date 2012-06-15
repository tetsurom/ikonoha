using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

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
