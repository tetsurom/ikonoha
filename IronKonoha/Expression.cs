﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace IronKonoha
{

	public enum ExprType
	{
		CONST = 0,
		NEW = 1,
		NULL = 2,
		NCONST = 3,
		LOCAL = 4,
		BLOCK = 5,
		FIELD = 6,
		BOX = 7,
		UNBOX = 8,
		CALL = 9,
		AND = 10,
		OR = 11,
		LET = 12,
		STACKTOP = 13,
		MAX = 14,
	}

	public abstract class ExprOrStmt : KObject
	{
		public string GetDebugView()
		{
			return GetDebugView(0);
		}
		public abstract string GetDebugView(int indent);
	}

	public abstract class KonohaExpr : ExprOrStmt
	{
		public ExprOrStmt parent { get; set; }
		/// <summary>
		/// 目的不明
		/// </summary>
		public Token tk { get; set; }
		public Syntax syn { get; set; }
		public ExprType build { get; set; }
		public bool IsSymbol
		{
			get
			{
				return tk.TokenType == TokenType.USYMBOL || tk.TokenType == TokenType.SYMBOL;
			}
		}

		public KonohaExpr()
		{
			this.ty = KonohaType.Var;
		}

		public override string ToString()
		{
			if (tk != null)
			{
				return tk.ToString();
			}
			return string.Empty;
		}

		public override string GetDebugView(int indent)
		{
			var builder = new StringBuilder();
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append(this.ToString());
			return builder.ToString();
		}

		public void typed(ExprType bld, KonohaType ty)
		{
			this.build = bld;
			this.ty = ty;
		}


		internal KonohaExpr tyCheckAt(Context ctx, KStatement stmt, int pos, KGamma gma, KonohaType reqty, TPOL pol)
		{
			var consexpr = this as ConsExpr;
			if (consexpr != null && pos < consexpr.Cons.Count)
			{
				KonohaExpr expr = consexpr.Cons[pos] as KonohaExpr;
				expr = expr.tyCheck(ctx, stmt, gma, reqty, pol);
				consexpr.Cons[pos] = expr;
				return expr;
			}
			return null;
		}

		// tycheck.h
		// static kExpr *Expr_tyCheck(CTX, kStmt *stmt, kExpr *expr, kGamma *gma, ktype_t reqty, int pol)
		internal KonohaExpr tyCheck(Context ctx, KStatement stmt, KGamma gma, KonohaType reqty, TPOL pol)
		{
			var texpr = this;
			if (stmt.isERR) texpr = null;
			if (this.syn == null)
			{
				this.syn = gma.ks.GetSyntax(this.tk.Keyword);
			}
			if (this.ty == KonohaType.Var)
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
						if (texpr.ty != KonohaType.Var) return texpr;
					}
					fo = a[0];
				}
				texpr = fo(stmt, this, gma, reqty);
			}
			if (stmt.isERR) texpr = null;
			if (texpr != null)
			{
				//DBG_P("type=%s, reqty=%s", TY_t(expr->ty), TY_t(reqty));
				if (texpr.ty == KonohaType.Void)
				{
					if ((pol & TPOL.ALLOWVOID) == 0)
					{
						//texpr = kExpr_p(stmt, expr, ERR_, "void is not acceptable")
						ctx.SUGAR_P(ReportLevel.ERR, new LineInfo(0, ""), 0, "void is not acceptable");
					}
					//return texpr;
					return null;
				}
				if (reqty == KonohaType.Var || texpr.ty == reqty || (pol & TPOL.NOCHECK) != 0)
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

		private static KonohaExpr TypedMethodCall(Context ctx, KStatement stmt, KonohaType reqty, MethodInfo mtd, KGamma gma, int p, KonohaExpr texpr)
		{
			var expr = new ConsExpr(ctx, stmt.syn);
			expr.Cons.Add(mtd);
			throw new NotImplementedException();
		}

		private static KonohaExpr BoxingExpr(Context ctx, KonohaExpr konohaExpr, KonohaType reqty)
		{
			throw new NotImplementedException();
		}

		public KonohaType ty { get; set; }

		// static kExpr *Expr_lookupMethod(CTX, kStmt *stmt, kExpr *expr, kcid_t this_cid, kGamma *gma, ktype_t reqty)
		public virtual KonohaExpr lookupMethod(Context ctx, KStatement stmt, KonohaType cid, KGamma gma, KonohaType reqty)
		{
			throw new NotSupportedException();
		}
	}

	[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
	public class ConsExpr : KonohaExpr
	{
		public IList<object> Cons { get; private set; }

		public ConsExpr(Context ctx, Syntax syn, params object[] param)
		{
			Cons = new List<object>(param);
			this.syn = syn;
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

		public override string GetDebugView(int indent)
		{
			var builder = new StringBuilder();
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append(this.syn == null ? "!null" : this.syn.KeyWord.Name);
			builder.Append('(');
			foreach (var con in Cons)
			{
				builder.Append(System.Environment.NewLine);
				if (con == null)
				{
					for (int i = 1; i < indent + 4; ++i)
					{
						builder.Append(' ');
					}
					builder.Append("null");
				}
				else if (con is ExprOrStmt)
				{
					builder.Append(((ExprOrStmt)con).GetDebugView(indent + 4));
				}
				else
				{
					for (int i = 1; i < indent + 4; ++i)
					{
						builder.Append(' ');
					}
					builder.Append(con.ToString());
				}
			}
			builder.Append(System.Environment.NewLine);
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append(')');
			return builder.ToString();
		}

		public override KonohaExpr lookupMethod(Context ctx, KStatement stmt, KonohaType cid, KGamma gma, KonohaType reqty)
		{
			var ks = gma.ks;
			var message = this.Cons[0] as Token;
			Debug.Assert(message != null);
			if (message.TokenType == TokenType.SYMBOL || message.TokenType == TokenType.USYMBOL)
			{
				//kToken_setmn(tkMN, ksymbolA(S_text(tkMN->text), S_size(tkMN->text), SYM_NEWID), MNTYPE_method);
				message.TokenType = TokenType.MethodName;
			}
			KFunc mtd = null;
			if (message.TokenType == TokenType.MethodName)
			{
				mtd = gma.ks.getMethod(cid, message.Text);
				if (mtd == null)
				{
					throw new NotImplementedException();
					/*
					if (message.Text != string.Empty)
					{
						mtd = kKonohaSpace_getMethodNULL(ks, cid, 0);
						if (mtd != null)
						{
							return tyCheckDynamicCallParams(ctx, stmt, mtd, gma, message.Text, reqty);
						}
					}
					if (tkMN->mn == MN_new && kArray_size(expr->cons) == 2 && CT_(kExpr_at(expr, 1)->ty)->bcid == TY_Object)
					{
						//DBG_P("bcid=%s", TY_t(CT_(kExpr_at(expr, 1)->ty)->bcid));
						DBG_ASSERT(kExpr_at(expr, 1)->ty != TY_var);
						return kExpr_at(expr, 1);  // new Person(); // default constructor
					}
					 * */
					//kToken_p(stmt, tkMN, ERR_, "undefined %s: %s.%s", T_mntype(tkMN->mn_type), TY_t(cid), kToken_s(tkMN));
				}
			}
			if (mtd != null)
			{
				return tyCheckCallParams(ctx, stmt, mtd, gma, reqty);
			}
			return null;
		}

		private KonohaExpr tyCheckCallParams(Context ctx, KStatement stmt, KFunc mtd, KGamma gma, KonohaType reqty)
		{
			var cons = this.Cons;
			var expr1 = Cons[1] as KonohaExpr;
			var this_ct = expr1.ty;
			//DBG_ASSERT(IS_Method(mtd));
			Debug.Assert(this_ct != KonohaClass.Var);
			/*if (!TY_isUnbox(mtd.cid) && CT_isUnbox(this_ct))
			{
				expr1 = new_BoxingExpr(_ctx, cons->exprs[1], this_ct->cid);
				KSETv(cons->exprs[1], expr1);
			}
			 * */
			bool isConst = false;// (Expr_isCONST(expr1)) ? 1 : 0;
			//	if(rtype == TY_var && gma->genv->mtd == mtd) {
			//		return ERROR_Unsupported(_ctx, "type inference of recursive calls", TY_unknown, NULL);
			//	}
			for (int i = 2; i < Cons.Count; i++)
			{
				var texpr = tyCheckAt(ctx, stmt, i, gma, KonohaClass.Var, 0);
				if (texpr == null)
				{
					return texpr;
				}
			}
			//	mtd = kExpr_lookUpOverloadMethod(_ctx, expr, mtd, gma, this_ct);
			var pa = mtd.Parameters;
			/*if (pa.Count() + 2 != Cons.Count)
			{
				return kExpr_p(stmt, expr, ERR_, "%s.%s%s takes %d parameter(s), but given %d parameter(s)", CT_t(this_ct), T_mn(mtd->mn), (int)pa->psize, (int)size - 2);
			}
			for (i = 0; i < pa->psize; i++)
			{
				size_t n = i + 2;
				ktype_t ptype = ktype_var(_ctx, pa->p[i].ty, this_ct);
				int pol = param_policy(pa->p[i].fn);
				kExpr* texpr = kExpr_tyCheckAt(stmt, expr, n, gma, ptype, pol);
				if (texpr == K_NULLEXPR)
				{
					return kExpr_p(stmt, expr, ERR_, "%s.%s%s accepts %s at the parameter %d", CT_t(this_ct), T_mn(mtd->mn), TY_t(ptype), (int)i + 1);
				}
				if (!Expr_isCONST(expr)) isConst = 0;
			}
			var expr = Expr_typedWithMethod(_ctx, expr, mtd, reqty);
			if (isConst && kMethod_isConst(mtd))
			{
				ktype_t rtype = ktype_var(_ctx, pa->rtype, this_ct);
				return ExprCall_toConstValue(_ctx, expr, cons, rtype);
			}*/
			//return expr;
			return this;
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

	[System.Diagnostics.DebuggerDisplay("{Data} [{ty}]")]
	public class ConstExpr<T> : KonohaExpr
	{
		public ConstExpr(T data)
		{
			Data = data;
			ty = new TypeWrapper(typeof(T));
		}
		public T Data { get; set; }

		public override string GetDebugView(int indent)
		{
			var builder = new StringBuilder();
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append(Data.ToString());
			return builder.ToString();
		}
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

		public override string GetDebugView(int indent)
		{
			var builder = new StringBuilder();
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append('{');
			foreach (var block in blocks)
			{
				builder.Append(System.Environment.NewLine);
				builder.Append(block.GetDebugView(indent + 4));
			}
			builder.Append(System.Environment.NewLine);
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append('}');
			return builder.ToString();
		}

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
