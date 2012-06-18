using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha.TyCheck
{
	internal class TopStmtTyCheck
	{
		internal static bool MethodDecl(KStatement stmt, Syntax syn, KGamma gma)
		{
			bool r = false;
			var ks = gma.ks;
			KFuncFlag flag = 0;// Stmt_flag(_ctx, stmt, MethodDeclFlag, 0);
			KType cid = stmt.getcid(KeywordType.Usymbol, null);//ks.scrobj.Type);
			//kmethodn_t mn = Stmt_getmn(_ctx, stmt, ks, KW_Symbol, MN_new);
			string name = stmt.map[ks.Symbols.SYMBOL].tk.Text;
			string body = stmt.map[ks.Symbols.Block].tk.Text;
			//kParam* pa = Stmt_newMethodParamNULL(_ctx, stmt, gma);
			var pa = (stmt.map[ks.Symbols.Params] as BlockExpr).blocks;
			//if (TY_isSingleton(cid)) flag |= kMethod_Static;
			if (pa != null)
			{
				var mtd = new KFunc(ks, flag, cid, name, pa, body);
				if (ks.DefineMethod(mtd, stmt.ULine))
				{
					r = true;
					stmt.MethodFunc = mtd;
					stmt.done();
				}
			}
			return r;
		}

		internal static bool Expr(KStatement stmt, Syntax syn, KGamma gma)
		{
			bool r = stmt.tyCheckExpr(gma.ks.ctx, KeywordType.Expr, gma, KType.TVar, TPOL.ALLOWVOID);
			stmt.typed(stmt, StmtType.EXPR);
			return r;
		}

		internal static bool ParamsDecl(KStatement stmt, Syntax syn, KGamma gma)
		{
			throw new NotImplementedException();
		}

		internal static bool If(KStatement stmt, Syntax syn, KGamma gma)
		{
			throw new NotImplementedException();
		}

		internal static bool Else(KStatement stmt, Syntax syn, KGamma gma)
		{
			throw new NotImplementedException();
		}

		internal static bool ConstDecl(KStatement stmt, Syntax syn, KGamma gma)
		{
			throw new NotImplementedException();
		}
	}
}
