using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha.TyCheck
{
	public class StmtTyCheck
	{
		internal static bool If(KStatement stmt, Syntax syn, KGamma gma)
		{
			Context ctx = gma.ks.ctx;
			bool r = true;
			if ((r = stmt.tyCheckExpr(gma.ks.ctx, KeywordType.Expr, gma, KonohaType.Boolean, 0)))
			{
				BlockExpr bkThen = stmt.toBlock(ctx, ctx.Symbols.Block, null);
				Debug.Assert(bkThen != null);
				BlockExpr bkElse = null;
				if (stmt.map.ContainsKey(ctx.Symbols.Else))
				{
					bkElse = stmt.toBlock(ctx, ctx.Symbols.Else, null);
				}
				r = bkThen.TyCheckAll(gma.ks.ctx, gma);
				if (bkElse != null)
				{
					r = r & bkElse.TyCheckAll(ctx, gma);
				}
				stmt.typed(StmtType.IF);
			}
			return r;
		}

		internal static bool Else(KStatement stmt, Syntax syn, KGamma gma)
		{
			Context ctx = gma.ks.ctx;
			bool r = true;
			var stmtIf = stmt.LookupIfStmt(gma.ks.ctx);
			if (stmtIf != null)
			{
				BlockExpr bkElse = stmt.toBlock(ctx, ctx.Symbols.Else, null);
				stmtIf.map[ctx.Symbols.Else] = bkElse;

				stmt.done();
				r = bkElse.TyCheckAll(gma.ks.ctx, gma);
			}
			else
			{
				//kStmt_p(stmt, ERR_, "else is not statement");
				r = false;
			}
			return r;
		}

		internal static bool Return(KStatement stmt, Syntax syn, KGamma gma)
		{
			bool r = true;
			if (gma.mtd == null)
			{
				return true;
			}
			KonohaType rtype = gma.mtd.ReturnType;
			stmt.typed(StmtType.RETURN);
			if (rtype != KonohaType.Void)
			{
				r = stmt.tyCheckExpr(gma.ks.ctx, KeywordType.Expr, gma, rtype, 0);
			}
			else
			{
				var expr = stmt.map.Values.ElementAt(1);
				if (expr != null)
				{
					//kStmt_p(stmt, WARN_, "ignored return value");
					stmt.tyCheckExpr(gma.ks.ctx, KeywordType.Expr, gma, KonohaType.Var, 0);
					stmt.map.Remove(stmt.map.Keys.ElementAt(1));
				}
			}
			return r;
		}

		internal static bool Import(KStatement stmt, Syntax syn, KGamma gma)
		{
			bool r = true;
			var cons = stmt.map.Values.First() as ConsExpr;
			string packageName = cons.GetSourceView();
			stmt.typed(StmtType.EXPR);
			stmt.syn = gma.ks.GetSyntax(KeyWordTable.Expr);
			stmt.map.Clear();
			stmt.map[gma.ks.Symbols.Expr] = new ConsExpr(
				gma.ks.ctx,
				gma.ks.GetSyntax(KeyWordTable.DOT),
				new Token(TokenType.SYMBOL, "import", 0),
				new ConstExpr<KonohaType>(new TypeWrapper(typeof(IronKonoha.KNameSpace))) { tk = new Token(TokenType.SYMBOL, "K", 0), build = ExprType.CONST },
				new ConstExpr<string>(packageName)) {
					tk = new Token(TokenType.TEXT, packageName, 0),
					build = ExprType.CONST,
					syn = gma.ks.GetSyntax(KeyWordTable.Params)
			};
			return r;
		}

		static bool declType(KStatement stmt, KonohaExpr expr, KGamma gma, KonohaType ty, KStatement lastStmtRef)
		{
			Debug.Assert(expr is ConsExpr);
			var ctx = gma.ks.ctx;
			if(expr is TermExpr) {
				throw new NotImplementedException();
				/*
				TermExpr te = expr as TermExpr;
				if (te.toVariable(ctx, stmt, gma, ty))
				{
					expr = new ParamExpr(gma.vars.Last());
					//var vexpr = new VariableExp(null, ty, 0, gma);
					var vexpr = new ParamExpr(gma.vars.Last());
					expr = new ConsExpr(ctx, gma.ks.GetSyntax(KeyWordTable.LET), 3, null, expr, vexpr);
					return appendAssignmentStmt(ctx, expr, lastStmtRef);
				}
				 * */
			}
			else if(expr.syn.KeyWord == KeyWordTable.LET) {
				var lexpr = expr.GetConsAt<KonohaExpr>(1);
				if(expr.tyCheckAt(gma.ks.ctx, stmt, 2, gma, KonohaType.Var, 0) == null) {
					// this is neccesarry to avoid 'int a = a + 1;';
					return false;
				}
				if (lexpr.toVariable(ctx, stmt, gma, ty))
				{
					((ConsExpr)expr).Cons[1] = new ParamExpr(gma.lvar.Last());
					if (expr.tyCheckAt(ctx, stmt, 2, gma, ty, 0) != null)
					{
						return appendAssignmentStmt(ctx, expr, lastStmtRef);
					}
					return false;
				}
			} else if(expr.syn.KeyWord == KeyWordTable.COMMA) {
				for(int i = 1; i < ((ConsExpr)expr).Cons.Count; ++i) {
					if(!declType(stmt, expr.GetConsAt<KonohaExpr>(i), gma, ty, lastStmtRef)) return false;
				}
				return true;
			}
			//kStmt_p(stmt, ERR_, "variable name is expected");
			return false;
		}

		private static bool appendAssignmentStmt(Context ctx, KonohaExpr expr, KStatement lastStmtRef)
		{
			return true;
		}

		internal static bool TypeDecl(KStatement stmt, Syntax syn, KGamma gma)
		{
			SingleTokenExpr stk = stmt.map[Symbol.Get(gma.ks.ctx, KeywordType.Type)] as SingleTokenExpr;
			KonohaExpr expr = stmt.map[Symbol.Get(gma.ks.ctx, KeywordType.Expr)];
			if(stk == null || stk.tk.TokenType != TokenType.TYPE || expr == null) {
				return false;
			}
			stmt.done();
			return declType(stmt, expr, gma, stk.tk.Type, stmt);
		}

		internal static bool While(KStatement stmt, Syntax syn, KGamma gma)
		{
			Debug.WriteLine("while statement ...");
			bool ret = false;
			Context ctx = gma.ks.ctx;
			if(stmt.tyCheckExpr(ctx,KeywordType.Expr,gma, KonohaType.Boolean, 0)) {
				BlockExpr bk = stmt.toBlock(ctx,ctx.Symbols.Block,null);
				ret = bk.TyCheckAll(gma.ks.ctx,gma);
				stmt.done();
			}
			return ret;
		}

		internal static bool Class(KStatement stmt, Syntax syn, KGamma gma)
		{
			Debug.WriteLine("class decl ...");
			Token tkC = stmt.map[stmt.ks.Symbols.SYMBOL].tk;
			var extendsExpr = stmt.Expr(stmt.ks.Symbols.Extends);
			Token tkE = extendsExpr != null ? extendsExpr.tk : null;
			int cflag = 0;
			KonohaType supct = null;
			if (tkE != null) {
				Debug.Assert(tkE.Keyword == KeyWordTable.Usymbol);
				supct = tkE.Type;
				throw new NotImplementedException("Class extending has not supported yet.");
				//if(supct.isFinal) {
				//    throw new InvalidOperationException(string.Format("{0} is final", supct.Name));
				//    return false;
				//}
				//if(!supct.isDefined) {
				//    //SUGAR Stmt_p(_ctx, stmt, null, ERR_, "%s has undefined field(s)", CT_t(supct));
				//    throw new InvalidOperationException(string.Format("{0} has undefined field(s)", supct.Name));
				//    return false;
				//}
			}
			KonohaType ct = gma.ks.defineClassName(cflag, tkC.Text, supct, stmt.ULine);
			tkC.Keyword = KeyWordTable.Type;
			tkC.Type = ct;
			//stmt.parseClassBlock(tkC);
			BlockExpr bk = stmt.Block(stmt.ks.Symbols.Block);
			//ct.setField(ctx, ct, supct, checkFieldSize(ctx, bk));
			//if(!ct.addClassFields(ctx, ct, gma, bk, stmt.ULine)) {
			//    return false;
			//}
			stmt.done();
			//ct.checkMethodDecl(ctx, tkC, bk, stmt);
			return true;
		}
	}
}
