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
			if ((r = stmt.tyCheckExpr(gma.ks.ctx, KeywordType.Expr, gma, KType.Boolean, 0)))
			{
				BlockExpr bkThen = stmt.map[Symbol.Get(gma.ks.ctx, KeywordType.Block)] as BlockExpr;
				BlockExpr bkElse = stmt.map[Symbol.Get(gma.ks.ctx, KeywordType.Else)] as BlockExpr;
				r = bkThen.TyCheckAll(gma.ks.ctx, gma);
				r = r & bkElse.TyCheckAll(gma.ks.ctx, gma);
				stmt.typed(stmt, StmtType.IF);
			}
			return r;
		}

		internal static bool Else(KStatement stmt, Syntax syn, KGamma gma)
		{
			throw new NotImplementedException();
		}

		internal static bool Return(KStatement stmt, Syntax syn, KGamma gma)
		{
			throw new NotImplementedException();
		}

		internal static bool TypeDecl(KStatement stmt, Syntax syn, KGamma gma)
		{
			throw new NotImplementedException();
		}
	}
}
