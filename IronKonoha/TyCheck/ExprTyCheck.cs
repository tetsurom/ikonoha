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
			if (expr is ConsExpr && (expr.syn.Flag & SynFlag.ExprOp) != 0)
			{
				var cons = ((ConsExpr)expr).Cons;
				var ctx = gma.ks.ctx;
				cons[1] = ((KonohaExpr)cons[1]).tyCheck(ctx, stmt, gma, KonohaType.Var, 0);
				cons[2] = ((KonohaExpr)cons[2]).tyCheck(ctx, stmt, gma, KonohaType.Var, 0);
				if (cons[0] != null && cons[0] is Token)
				{
					switch (((Token)cons[0]).Keyword.Type)
					{
						case KeywordType.GT:
						case KeywordType.GTE:
						case KeywordType.LT:
						case KeywordType.LTE:
						case KeywordType.EQ:
						case KeywordType.NEQ:
							expr.ty = KonohaType.Boolean;
							break;
						case KeywordType.ADD:
						case KeywordType.SUB:
						case KeywordType.MUL:
						case KeywordType.DIV:
							var t1 = ((KonohaExpr)cons[1]).ty;
							var t2 = ((KonohaExpr)cons[2]).ty;
							if (t1 == t2)
							{
								expr.ty = t1;
							}
							else if (t1 == KonohaType.Float || t2 == KonohaType.Float)
							{
								expr.ty = KonohaType.Float;
							}
							break;
						case KeywordType.MOD:
							expr.ty = KonohaType.Int;
							break;
					}
				}
				return expr;
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
				int atop = gma.lvar.Count;
				//KonohaExpr lvar = new_Variable(LOCAL_, TY_var, addGammaStack(_ctx, &gma->genv->l, TY_var, 0/*FN_*/), gma);
				if(!bk.TyCheckAll(gma.ks.ctx, gma)) {
					return texpr;
				}
				var rexpr = lastExpr.Expr(gma.ks.ctx, gma.ks.Symbols.Expr);
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
				for (i = atop; i < gma.lvar.Count; i++)
				{
					var v = gma.lvar[i];
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
			if (texpr is ConstExpr<KonohaType> && texpr != null)
			{
				var this_cid = ((ConstExpr<KonohaType>)texpr).TypedData;
				return expr.lookupMethod(gma.ks.ctx, stmt, this_cid, gma, reqty);
			}
			if (texpr is ParamExpr)
			{
				var this_cid = texpr.ty;
				return expr.lookupMethod(gma.ks.ctx, stmt, this_cid, gma, reqty);
			}

			throw new NotImplementedException();
		}

		internal static KonohaExpr Symbol(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			Debug.Assert(gma != null);

			var name = expr.tk.Text;

			// search local variables
			foreach (var p in gma.lvar.Reverse<FuncParam>())
			{
				if (p.Name == name)
				{
					return new ParamExpr(p)
					{
						syn = expr.syn,
						tk = expr.tk,
						build = expr.build,
						parent = expr.parent
					};
				}
			}

			// search function parameter
			Debug.Assert(gma != null);
			if (gma.mtd != null)
			{
				Debug.Assert(gma.mtd.Parameters != null);
				var parameters = gma.mtd.Parameters;
				int i = 0;
				foreach(var p in parameters){
					if(p.Name == name){
						return new ParamExpr(i, p)
						{
							syn = expr.syn,
							tk = expr.tk,
							build = expr.build,
							parent = expr.parent
						};
					}
					++i;
				}
			}


			var classes = gma.ks.Classes;
			if(classes.ContainsKey(name)){
				expr.tk.TokenType = TokenType.TYPE;
				expr.tk.Keyword = KeyWordTable.Type;
				expr.syn = gma.ks.GetSyntax(KeyWordTable.Type);
				return new ConstExpr<KonohaType>(classes[name])
				{
					syn = gma.ks.GetSyntax(KeyWordTable.Type),
					tk = expr.tk,
					build = expr.build,
					parent = expr.parent
				};
			}
			var globals = ((KonohaClass)classes["System"]).StaticFields;
			if (globals.ContainsKey(name))
			{
				return new ConstExpr<object>(globals[name])
				{
					syn = expr.syn,
					tk = expr.tk,
					build = expr.build,
					parent = expr.parent
				};
			}
			var funcs = ((KonohaClass)classes["System"]).Methods;
			if (funcs.ContainsKey(name))
			{
				return new ConstExpr<object>(funcs[name])
				{
					syn = expr.syn,
					tk = expr.tk,
					build = expr.build,
					parent = expr.parent
				};
			}
			throw new InvalidOperationException(string.Format("undefined name: {0}", name));
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
			//throw new NotImplementedException();
			// FIXME: I don't know it is right way.....
			return Symbol(stmt, expr, gma, reqty);
		}

		internal static KonohaExpr FuncStyleCall(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType reqty)
		{
			Debug.Assert(expr is ConsExpr);
			var cons = ((ConsExpr)expr).Cons;
			//if (Expr_isSymbol(kExpr_at(expr, 0)))
			if (cons[0] is KonohaExpr)
			{
				var expr0 = cons[0] as KonohaExpr;
				// FIXME: I can't find it is right way to make createinstance tree. But it's looked works.
				if (expr0 is CreateInstanceExpr)
				{
					var newExpr = (CreateInstanceExpr)expr0;
					if (newExpr.ty == null && newExpr.TypeName != null)
					{
						KonohaType cls = null;
						if(gma.ks.Classes.TryGetValue(newExpr.TypeName, out cls)){
							newExpr.ty = cls;
						}
					}
					return expr0;
				}
				var mtd = expr.lookUpFuncOrMethod(gma.ks.ctx, gma, reqty);
				if (mtd != null)
				{
					expr.ty = mtd.ReturnType;
					return expr.tyCheckCallParams(gma.ks.ctx, stmt, mtd, gma, reqty);
				}
				throw new NotImplementedException();
				//if (expr0.ty != KonohaType.Func)
				//{
				//    var tk = expr.tk;
				//    return kToken_p(stmt, tk, ERR_, "undefined function: %s", kToken_s(tk)));
				//}
			}
			else
			{
				if (expr.tyCheckAt(gma.ks.ctx, stmt, 0, gma, KonohaType.Var, 0) != null)
				{
					throw new NotImplementedException();
					//if (!((KonohaType)((ConsExpr)expr).Cons).Type == KonohaType.Func)
					//{
					//    return kExpr_p(stmt, expr, ERR_, "function is expected");
					//}
				}
			}
			return expr.tyCheckFuncParams(gma.ks.ctx, stmt, (KonohaType)((ConsExpr)expr).Cons, gma);
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
