using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha.TyCheck
{
	public class ExprTyCheck
	{
		internal static KonohaExpr Int(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}
		internal static KonohaExpr Float(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}
		internal static KonohaExpr Text(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}

		internal static KonohaExpr Expr(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}

		internal static KonohaExpr Block(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
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
				Type ty = rexpr.ty;
				if(ty != typeof(void)) {
					var letexpr = new ConsExpr(gma.ks.ctx, gma.ks.GetSyntax(KeyWordTable.Return), null, gma.lvar, rexpr);
					letexpr.ty = typeof(void);
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
		internal static KonohaExpr MethodCall(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}

		internal static KonohaExpr Symbol(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}

		internal static KonohaExpr USymbol(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}

		internal static KonohaExpr Type(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}

		internal static KonohaExpr FuncStyleCall(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}

		internal static KonohaExpr And(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			if (expr.tyCheckAt(gma.ks.ctx, stmt, 1, gma, typeof(bool), 0) != null)
			{
				if (expr.tyCheckAt(gma.ks.ctx, stmt, 2, gma, typeof(bool), 0) != null)
				{
					expr.typed(ExprType.AND, typeof(bool));
					return expr;
				}
			}
			return null;
		}

		internal static KonohaExpr Or(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			if (expr.tyCheckAt(gma.ks.ctx, stmt, 1, gma, typeof(bool), 0) != null)
			{
				if (expr.tyCheckAt(gma.ks.ctx, stmt, 2, gma, typeof(bool), 0) != null)
				{
					expr.typed(ExprType.OR, typeof(bool));
					return expr;
				}
			}
			return null;
		}

		internal static KonohaExpr True(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}

		internal static KonohaExpr False(KStatement stmt, KonohaExpr expr, KGamma gma, Type reqty)
		{
			throw new NotImplementedException();
		}
	}
}
