using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Dynamic;

namespace IronKonoha.TyCheck
{
	public class ExprTyCheck
	{
		internal static KonohaExpr Int(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			return new ConstExpr<long>(long.Parse(expr.tk.Text))
			{
				syn = expr.syn,
				tk = expr.tk,
				build = expr.build,
				parent = expr.parent
			};
		}
		internal static KonohaExpr Float(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			return new ConstExpr<double>(double.Parse(expr.tk.Text))
			{
				syn = expr.syn,
				tk = expr.tk,
				build = expr.build,
				parent = expr.parent
			};
		}
		internal static KonohaExpr Text(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			return new ConstExpr<string>(expr.tk.Text)
			{
				syn = expr.syn,
				tk = expr.tk,
				build = expr.build,
				parent = expr.parent
			};
		}

		internal static KonohaExpr Expr(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			if ((expr.syn.Flag & SynFlag.ExprOp) != 0)
			{
				var cexpr = expr as ConsExpr;
				var ctx = gma.ks.ctx;
				cexpr.Cons[1] = (cexpr.Cons[1] as KonohaExpr).tyCheck(ctx, stmt, gma, KonohaType.Var, 0);
				cexpr.Cons[2] = (cexpr.Cons[2] as KonohaExpr).tyCheck(ctx, stmt, gma, KonohaType.Var, 0);
				cexpr.ty = (cexpr.Cons[1] as KonohaExpr).ty;
				return cexpr;
			}
			throw new NotImplementedException();
		}

		internal static KonohaExpr Block(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			KonohaExpr texpr = null;
			KStatement lastExpr = null;
			var uline = expr.tk.ULine;
			BlockExpr bk = expr as BlockExpr;
			Debug.Assert(bk != null);
			if(bk.blocks.Count > 0) {
				var lstmt = bk.blocks.Last();
				if(lstmt.syn.KeyWord.Type == KeywordType.Expr) {
					lastExpr = stmt;
				}
				uline = lstmt.ULine;
			}
			if(lastExpr != null) {
				int lvarsize = gma.lvar.Count;
				int i;
				int atop = gma.lvarlst.Count;
				//KonohaExpr lvar = new_Variable(LOCAL_, TY_var, addGammaStack(_ctx, &gma->genv->l, TY_var, 0/*FN_*/), gma);
				if(!bk.TyCheckAll(gma.ks.ctx, gma)) {
					return texpr;
				}
				var rexpr = lastExpr.Expr(gma.ks.ctx, KeywordType.Expr);
				Debug.Assert(rexpr != null);
				KonohaType ty = rexpr.ty;
				if (ty != KonohaType.Void)
				{
					var letexpr = new ConsExpr(gma.ks.ctx, gma.ks.GetSyntax(KeyWordTable.Return), null, gma.lvar, rexpr);
					letexpr.ty = KonohaType.Void;
					//var letexpr = new_TypedConsExpr(_ctx, TEXPR_LET, TY_void, 3, K_NULL, lvar, rexpr);
					lastExpr.map[IronKonoha.Symbol.Get(gma.ks.ctx, KeywordType.Expr)] = letexpr;
					//texpr = kExpr_setVariable(expr, BLOCK_, ty, lvarsize, gma);
				}
				for(i = atop; i < gma.lvarlst.Count; i++) {
					var v = gma.lvarlst[i];
					//if(v->build == TEXPR_LOCAL_ && v->index >= lvarsize) {
					//	v->build = TEXPR_STACKTOP; v->index = v->index - lvarsize;
						//DBG_P("v->index=%d", v->index);
					//}
				}
				//if(lvarsize < gma.lvar.Count) {
					//gma->genv->l.varsize = lvarsize;
				//}
			}
			if(texpr == null) {
				//kStmt_errline(stmt, uline);
				//kStmt_p(stmt, ERR_, "block has no value");
			}
			return texpr;
		}
		internal static KonohaExpr MethodCall(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			var texpr = expr.tyCheckAt(gma.ks.ctx, stmt, 1, gma, KonohaType.Var, 0);
			if (texpr != null)
			{
				var this_cid = texpr.ty;
				return expr.lookupMethod(gma.ks.ctx, stmt, this_cid, gma, reqty);
			}
			throw new NotImplementedException();
		}

		internal static KonohaExpr Symbol(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			var classes = gma.ks.Classes;
			var name = expr.tk.Text;
			if(classes.ContainsKey(name)){
				return new ConstExpr<IDynamicMetaObjectProvider>(classes[name])
				{
					syn = expr.syn,
					tk = expr.tk,
					build = expr.build,
					parent = expr.parent
				};
			}
			/*
			if (expr.tk.Text == "System")
			{
				expr.tk.Type = expr.ty = typeof(IronKonoha.Runtime.System);
				expr.typed(ExprType.CONST, expr.tk.Type);
				return expr;
			}
			 */
			throw new TypeAccessException();
		}

		internal static KonohaExpr USymbol(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			/*
			var tk = expr.tk;
			var ukey = ksymbolA(S_text(tk->text), S_size(tk->text), SYM_NONAME);
			if (ukey != SYM_NONAME)
			{
				kvs_t* kv = KonohaSpace_getConstNULL(_ctx, gma->genv->ks, ukey);
				if (kv != NULL)
				{
					if (SYMKEY_isBOXED(kv->key))
					{
						kExpr_setConstValue(expr, kv->ty, kv->oval);
					}
					else
					{
						kExpr_setNConstValue(expr, kv->ty, kv->uval);
					}
					RETURN_(expr);
				}
			}
			kObject* v = KonohaSpace_getSymbolValueNULL(_ctx, gma->genv->ks, S_text(tk->text), S_size(tk->text));
			kExpr* texpr = (v == NULL) ? kToken_p(stmt, tk, ERR_, "undefined name: %s", kToken_s(tk)) : kExpr_setConstValue(expr, O_cid(v), v);
			RETURN_(texpr);
			*/
			throw new NotImplementedException();
		}

		internal static KonohaExpr Type(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			//Debug.Assert(expr.tk.isType);
			//return kExpr_setVariable(expr, NULL, expr.tk.ty, 0, gma);
			throw new NotImplementedException();
		}

		internal static KonohaExpr FuncStyleCall(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			//Debug.Assert(IS_Expr(kExpr_at(expr, 0)));
			//Debug.Assert(expr.cons[1] == null);
			/*
			if (Expr_isSymbol(kExpr_at(expr, 0)))
			{
				kMethod* mtd = Expr_lookUpFuncOrMethod(_ctx, expr, gma, reqty);
				if (mtd != NULL)
				{
					RETURN_(Expr_tyCheckCallParams(_ctx, stmt, expr, mtd, gma, reqty));
				}
				if (!TY_isFunc(kExpr_at(expr, 0)->ty))
				{
					kToken* tk = kExpr_at(expr, 0)->tk;
					RETURN_(kToken_p(stmt, tk, ERR_, "undefined function: %s", kToken_s(tk)));
				}
			}
			else
			{
				if (Expr_tyCheckAt(_ctx, stmt, expr, 0, gma, TY_var, 0) != K_NULLEXPR)
				{
					if (!TY_isFunc(expr->cons->exprs[0]->ty))
					{
						RETURN_(kExpr_p(stmt, expr, ERR_, "function is expected"));
					}
				}
			}
			RETURN_(Expr_tyCheckFuncParams(_ctx, stmt, expr, CT_(kExpr_at(expr, 0)->ty), gma));
			*/
			throw new NotImplementedException();
		}

		internal static KonohaExpr And(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			if (expr.tyCheckAt(gma.ks.ctx, stmt, 1, gma, KonohaType.Boolean, 0) != null)
			{
				if (expr.tyCheckAt(gma.ks.ctx, stmt, 2, gma, KonohaType.Boolean, 0) != null)
				{
					expr.typed(ExprType.AND, KonohaType.Boolean);
					return expr;
				}
			}
			return null;
		}

		internal static KonohaExpr Or(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			if (expr.tyCheckAt(gma.ks.ctx, stmt, 1, gma, KonohaType.Boolean, 0) != null)
			{
				if (expr.tyCheckAt(gma.ks.ctx, stmt, 2, gma, KonohaType.Boolean, 0) != null)
				{
					expr.typed(ExprType.OR, KonohaType.Boolean);
					return expr;
				}
			}
			return null;
		}

		internal static KonohaExpr True(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			return new ConstExpr<bool>(true)
			{
				syn = expr.syn,
				tk = expr.tk,
				build = expr.build,
				parent = expr.parent
			};
		}

		internal static KonohaExpr False(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			return new ConstExpr<bool>(false)
			{
				syn = expr.syn,
				tk = expr.tk,
				build = expr.build,
				parent = expr.parent
			};
		}

		internal static KonohaExpr Dot(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			var cexpr = expr as ConsExpr;
			KonohaExpr receiver = cexpr.Cons[1] as KonohaExpr;
			Token message = cexpr.Cons[0] as Token;
			var ctx = gma.ks.ctx;
			receiver = receiver.tyCheck(ctx, stmt, gma, KonohaType.Var, 0);
			//message = message.tyCheck(ctx, stmt, gma, KonohaType.Var, 0);
			/*var meminfo = receiver.ty.GetMember(message.Text);
			if (meminfo.Length == 0)
			{
				throw new MemberAccessException();
			}
			/*if (meminfo[0].MemberType == MemberTypes.Method)
			{
				return new ConsExpr(ctx, gma.ks.GetSyntax(KeyWordTable.Parenthesis), receiver.ty.GetMethod(message.Text));
			}*/
			//*/
			cexpr.Cons[1] = receiver;
			/*
			cexpr.Cons[0] = new ConstExpr<MemberInfo>(meminfo[0])
			{
				syn = expr.syn,
				tk = expr.tk,
				build = expr.build,
				parent = expr.parent,
			};
			 * */
			return cexpr;
		}
	}
}
