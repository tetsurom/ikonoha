using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha.TyCheck
{
	public class StmtTyCheck
	{
		internal static bool If(KStatement stmt, Syntax syn, KGamma gma)
		{
			bool r = true;
			if ((r = stmt.tyCheckExpr(gma.ks.ctx, KeywordType.Expr, gma, typeof(bool), 0)))
			{
				BlockExpr bkThen = stmt.map[gma.ks.Symbols.Block] as BlockExpr;
				BlockExpr bkElse = null;
				if (stmt.map.ContainsKey(gma.ks.Symbols.Else))
				{
					bkElse = stmt.map[gma.ks.Symbols.Else] as BlockExpr;
				}
				r = bkThen.TyCheckAll(gma.ks.ctx, gma);
				if (bkElse != null)
				{
					r = r & bkElse.TyCheckAll(gma.ks.ctx, gma);
				}
				stmt.typed(StmtType.IF);
			}
			return r;
		}

		internal static bool Else(KStatement stmt, Syntax syn, KGamma gma)
		{
			bool r = true;
			var stmtIf = stmt.LookupIfStmt(gma.ks.ctx);
			if (stmtIf != null)
			{
				BlockExpr bkElse = stmt.map[gma.ks.Symbols.Else] as BlockExpr;
				stmtIf.map[Symbol.Get(gma.ks.ctx, KeywordType.Else)] = bkElse;

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
			Type rtype = gma.mtd.ReturnType;
			stmt.typed(StmtType.RETURN);
			if (rtype != typeof(void))
			{
				r = stmt.tyCheckExpr(gma.ks.ctx, KeywordType.Expr, gma, rtype, 0);
			}
			else
			{
				var expr = stmt.map.Values.ElementAt(1);
				if (expr != null)
				{
					//kStmt_p(stmt, WARN_, "ignored return value");
					stmt.tyCheckExpr(gma.ks.ctx, KeywordType.Expr, gma, typeof(Variant), 0);
					stmt.map.Remove(stmt.map.Keys.ElementAt(1));
				}
			}
			return r;
		}

		internal static bool TypeDecl(KStatement stmt, Syntax syn, KGamma gma)
		{
			throw new NotImplementedException();
		}
	}
}
