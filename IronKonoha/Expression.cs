using System;
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
		public virtual string GetSourceView()
		{
			return GetSourceView(0);
		}
		public virtual string GetSourceView(int indent)
		{
			return GetDebugView(indent);
		}
	}

	public abstract class KonohaExpr : ExprOrStmt
	{
		public ExprOrStmt parent { get; set; }
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
		public override string GetSourceView(int indent)
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


		internal virtual KonohaExpr tyCheckAt(Context ctx, KStatement stmt, int pos, KGamma gma, KonohaType reqty, TPOL pol)
		{
			throw new NotSupportedException("tyCheckAt is supported only on ConsExpr");
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
				if (texpr.ty == reqty || texpr.ty.Name == reqty.Name)
				{
					//if (ctx.CT_(texpr.ty).isUnbox && !ctx.CT_(reqty).isUnbox)
					//{
					//	return KonohaExpr.BoxingExpr(ctx, this, reqty);
					//}
					return texpr;
				}
				// FIXME
				//var mtd = gma.ks.getCastMethod(texpr.ty, reqty);
				//Debug.WriteLine("finding cast {0} => {1}: {2}", texpr.ty, reqty, mtd);
				//if (mtd != null /*&& (mtd.isCoercion || (pol & TPOL.COERCION) != 0)*/)
				//{
				//    return KonohaExpr.TypedMethodCall(ctx, stmt, reqty, mtd, gma, 1, texpr);
				//}
				////return kExpr_p(stmt, expr, ERR_, "%s is requested, but %s is given", TY_t(reqty), TY_t(texpr->ty));
				//return null;
				return texpr;
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
		internal virtual KonohaExpr lookupMethod(Context ctx, KStatement stmt, KonohaType cid, KGamma gma, KonohaType reqty)
		{
			throw new NotSupportedException("lookupMethod is supported only on ConsExpr");
		}

		internal virtual KFunc lookUpFuncOrMethod(Context ctx, KGamma gma, KonohaType reqty)
		{
			throw new NotSupportedException("lookUpFuncOrMethod is supported only on ConsExpr");
		}

		internal virtual KonohaExpr tyCheckCallParams(Context context, KStatement stmt, KFunc mtd, KGamma gma, KonohaType reqty)
		{
			throw new NotSupportedException("tyCheckCallParams is supported only on ConsExpr");
		}

		internal virtual object GetConsAt(int index)
		{
			throw new NotSupportedException("GetConsAt is supported only on ConsExpr");
		}

		internal virtual T GetConsAt<T>(int index) where T : class
		{
			throw new NotSupportedException("GetConsAt is supported only on ConsExpr");
		}

		internal KonohaExpr tyCheckFuncParams(Context ctx, KStatement stmt, KonohaType ct, KGamma gma)
		{
			throw new NotImplementedException();
			/*
			ktype_t rtype = ct->p0;
			kParam* pa = CT_cparam(ct);
			size_t i, size = kArray_size(expr->cons);
			if (pa->psize + 2 != size)
			{
				return kExpr_p(stmt, expr, ERR_, "function %s takes %d parameter(s), but given %d parameter(s)", CT_t(ct), (int)pa->psize, (int)size - 2);
			}
			for (i = 0; i < pa->psize; i++)
			{
				size_t n = i + 2;
				kExpr* texpr = kExpr_tyCheckAt(stmt, expr, n, gma, pa->p[i].ty, 0);
				if (texpr == K_NULLEXPR)
				{
					return texpr;
				}
			}
			var mtd = gma.ks.getMethod(KonohaType.Func, "invoke");
			Debug.Assert(mtd != null);
			KSETv(expr->cons->exprs[1], expr->cons->exprs[0]);
			return expr.typedWithMethod(ctx, mtd, rtype);
			 * */
		}

		// static kbool_t ExprTerm_toVariable(CTX, kStmt *stmt, kExpr *expr, kGamma *gma, ktype_t ty)
		internal virtual bool toVariable(Context ctx, KStatement stmt, KGamma gma, KonohaType ty)
		{
			throw new NotSupportedException("toVariable is supported only on TermExpr");
		}

		public virtual object Data
		{
			get
			{
				throw new NotSupportedException("data property is supported only on ConstExpr");
			}
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
		public override string GetSourceView(int indent)
		{
			var builder = new StringBuilder();
			if (this.syn.KeyWord == KeyWordTable.DOT)
			{
				builder.Append(((KonohaExpr)Cons[1]).GetSourceView());
				builder.Append('.');
				builder.Append(((Token)Cons[0]).Text);
				return builder.ToString();
			}
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			foreach (var con in Cons)
			{
				builder.Append(System.Environment.NewLine);
				if (con == null)
				{
				}
				else if (con is ExprOrStmt)
				{
					builder.Append(((ExprOrStmt)con).GetSourceView());
				}
				else
				{
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

		internal override KonohaExpr lookupMethod(Context ctx, KStatement stmt, KonohaType cid, KGamma gma, KonohaType reqty)
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
				this.ty = mtd.ReturnType;
				return tyCheckCallParams(ctx, stmt, mtd, gma, reqty);
			}
			return null;
		}

		internal override KonohaExpr tyCheckCallParams(Context ctx, KStatement stmt, KFunc mtd, KGamma gma, KonohaType reqty)
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

		internal override KonohaExpr tyCheckAt(Context ctx, KStatement stmt, int pos, KGamma gma, KonohaType reqty, TPOL pol)
		{
			var consexpr = this as ConsExpr;
			if (pos < Cons.Count)
			{
				KonohaExpr expr = Cons[pos] as KonohaExpr;
				expr = expr.tyCheck(ctx, stmt, gma, reqty, pol);
				Cons[pos] = expr;
				return expr;
			}
			return null;
		}

		internal override KFunc lookUpFuncOrMethod(Context ctx, KGamma gma, KonohaType reqty)
		{
			var expr = (KonohaExpr)Cons[0];
			var tk = expr.tk;
			Debug.Assert(tk != null);
			var funcName = tk.Text;//ksymbolA(tk.Text, tk.Text.Length, gma.ks.Symbols.Noname);
			/*
			// search local variabls.
			for (int i = gma.lvar.Count - 1; i >= 0; i--)
			{
				if (gma.lvarlst[i].fn == funcName && gma.lvar[i].ty == KonohaType.Func)
				{
					expr.setVariable(LOCAL_, gma.lvar[i].ty, i, gma);
					return null;
				}
			}
			// search field variables
			for (int i = genv->f.varsize - 1; i >= 0; i--)
			{
				if (gma.fvar[i].fn == funcName && gma.lvar[i].ty == KonohaType.Func)
				{
					expr.setVariable(LOCAL, gma.lvar[i].ty, i, gma);
					return null;
				}
			}
			// search "this"'s fields (fvar[0] is "this")
			if (gma.fvar[0].ty != KonohaType.Void)
			{
				Debug.Assert(gma.this_cid == gma.fvar[0].ty);
				var mtd = gma.ks.getMethod(gma.this_cid, funcName);
				if (mtd != null)
				{
					Cons[1] = new_Variable(LOCAL, gma.this_cid, 0, gma);
					return mtd;
				}
				KonohaType ct = gma.this_cid;
				for (i = ct->fsize; i >= 0; i--)
				{
					if (ct.fields[i].fn == funcName && ct->fields[i].ty == KonohaType.Func)
					{
						expr.setVariable(FIELD, ct.fields[i].ty, i, gma);
						return null;
					}
				}
				mtd = gma.ks.getGetterMethod(ctx, gma.this_cid, funcName);
				if (mtd != null && mtd.ReturnType == KonohaType.Func)
				{
					Cons[0] = new_GetterExpr(ctx, tk, mtd, new_Variable(LOCAL, genv->this_cid, 0, gma));
					return null;
				}
			}
			 * */
			// search namespace-level functions
			/*
			var cid = gma.ks.scrobj.cid;
			KFunc mtd = gma.ks.getMethodN(cid, funcName);
			if (mtd != null)
			{
				Cons[1] = new_ConstValue(cid, gma.ks.scrobj);
				return mtd;
			}
			mtd = gma.ks.getGetterMethod(ctx, cid, funcName);
			if (mtd != null && mtd.ReturnType == KonohaType.Func)
			{
				Cons[0] = new_GetterExpr(ctx, tk, mtd, new_ConstValue(cid, gma.ks.scrobj));
				return null;
			}
			*/
			// search global functions:
			// if System class has searching method, call it.
			var sys = gma.ks.Classes["System"];
			var mtd = gma.ks.getMethod(sys, funcName);
			if (mtd != null)
			{
				//Cons[1] = new_Variable(null, KonohaType.System, 0, gma);
				Cons[1] = new ConstExpr<KonohaType>(sys);
			}
			if (mtd != null)
			{
				return mtd;
			}
			if (Cons[1] is ConstExpr<KonohaType>)
			{
				throw new InvalidOperationException(string.Format("undefined function or method: {0}.{1}", ((ConstExpr<KonohaType>)Cons[1]).TypedData.Name, funcName));
			}
			throw new InvalidOperationException(string.Format("undefined function or method: {0}", funcName));
		}

		internal KonohaExpr tyCheckCallParams(Context context, KStatement stmt, KonohaExpr expr, object mtd, KGamma gma, KonohaType reqty)
		{
			throw new NotImplementedException();
		}

		internal KonohaExpr tyCheckFuncParams(Context context, KStatement stmt, Type type, KGamma gma)
		{
			throw new NotImplementedException();
		}

		internal override object GetConsAt(int index)
		{
			return Cons[index];
		}

		internal override T GetConsAt<T>(int index)
		{
			return Cons[index] as T;
		}
	}

	[System.Diagnostics.DebuggerDisplay("{tk.Text} [{tk.Type}]")]
	public class TermExpr : KonohaExpr
	{
		public TermExpr()
		{
		}

		internal override bool toVariable(Context ctx, KStatement stmt, KGamma gma, KonohaType ty)
		{
			if (this.tk.TokenType == TokenType.SYMBOL)
			{
				Token tk = this.tk;
				if (tk.Keyword.Type != KeywordType.Symbol)
				{
					tk.Print(ctx, ReportLevel.ERR, "{0} is keyword", tk.Text);
					return false;
				}
				//ksymbol_t fn = ksymbolA(S_text(tk->text), S_size(tk->text), SYM_NEWID);
				//int index = addGammaStack(_ctx, &gma->genv->l, ty, fn);
				//kExpr_setVariable(expr, LOCAL_, ty, index, gma);
				gma.lvar.Add(new FuncParam(tk.Text, ty));
				return true;
			}
			return false;
		}
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
			TypedData = data;
			ty = new TypeWrapper(typeof(T));
		}
		public T TypedData { get; set; }

		public override string GetDebugView(int indent)
		{
			var builder = new StringBuilder();
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append(TypedData.ToString());
			return builder.ToString();
		}
		public override string GetSourceView(int indent)
		{
			var builder = new StringBuilder();
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append(TypedData.ToString());
			return builder.ToString();
		}
		public override object Data
		{
			get
			{
				return TypedData;
			}
		}
	}

	public class ParamExpr : KonohaExpr
	{
		public int Order { get; private set; }
		public string Name { get; private set; }
		public ParamExpr(int order, KonohaType type, string name)
		{
			Order = order;
			ty = type;
			Name = name;
		}
		public ParamExpr(int order, FuncParam param)
		{
			Order = order;
			ty = param.Type;
			Name = param.Name;
		}
		public ParamExpr(FuncParam param)
		{
			Order = -1;
			ty = param.Type;
			Name = param.Name;
		}
	}

	/// <summary>
	/// named 'NewExpr' in CKonoha
	/// </summary>
	public class CreateInstanceExpr : KonohaExpr
	{
		public ConsExpr paramExpr { get; set; }
		public string TypeName { get; private set; }

		public CreateInstanceExpr(KonohaType type, ConsExpr param)
		{
			this.ty = type;
			this.paramExpr = param;
		}
		public CreateInstanceExpr(string typename, ConsExpr param)
		{
			this.ty = null;
			this.TypeName = typename;
			this.paramExpr = param;
		}

		public override string GetDebugView(int indent)
		{
			var builder = new StringBuilder();
			for (int i = 1; i < indent; ++i)
			{
				builder.Append(' ');
			}
			builder.Append("new ");
			builder.Append(ty == null ? tk.Text : ty.Name);
			if (paramExpr != null)
			{
				builder.Append(System.Environment.NewLine);
				builder.Append(paramExpr.GetDebugView(indent + 4));
			}
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
		public void AddStatementLine(Context ctx, KNameSpace ks, IList<Token> tokens, int start, int end, out Token tkERR)
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
			//while (lvarsize < gma.lvar.Count)
			//{
			//    gma.lvar.Pop();
			//}
			return result;
		}

		internal void insertAfter(KStatement target, KStatement stmt)
		{
			this.blocks.Insert(this.blocks.IndexOf(target) + 1, stmt);
		}

		internal int checkFieldSize(Context ctx)
		{
			int c = 0;
			foreach (var stmt in blocks)
			{
				Debug.WriteLine("stmt->kw={0}", stmt.syn.KeyWord);
				if (stmt.syn.KeyWord == KeyWordTable.StmtTypeDecl)
				{
					ConsExpr expr = stmt.Expr(ctx.Symbols.Expr) as ConsExpr;
					if (expr != null)
					{
						if (expr.syn.KeyWord == KeyWordTable.COMMA)
						{
							// int a,b,c,...;
							c += (expr.Cons.Count - 1);
						}
						else if (expr.syn.KeyWord == KeyWordTable.LET || stmt.Expr(ctx.Symbols.Expr) is TermExpr)
						{
							// int a = 1; or int a;
							c++;
						}
					}
				}
			}
			return c;
		}
	}
}
