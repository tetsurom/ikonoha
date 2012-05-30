using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IronKonoha
{
	/// <summary>
	/// Generate DLR Expression Tree from konoha AST.
	/// </summary>
	class Converter
	{
		private Context ctx;
		private KonohaSpace ks;
		
		public Converter (Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
		}
		
		public Expression<Func<object,object>> Convert (BlockExpr block)
		{
			List<Expression> list = new List<Expression> ();
			foreach (KStatement st in block.blocks) {
				foreach (KonohaExpr kexpr in st.map.Values) {
					list.Add (MakeExpression (kexpr));
				}
			}
			var root = Expression.Convert (Expression.Block (list), typeof(object));
			var e = Expression.Lambda<Func<object, object>> (root, Expression.Parameter (typeof(object)));
			return e;
		}

		public Expression MakeExpression (KonohaExpr kexpr)
		{
			if (kexpr is ConsExpr) {
				ConsExpr cexpr = kexpr as ConsExpr;
				Token tk = cexpr.Cons [0] as Token;
				var param = cexpr.Cons.Skip (1).Select (p => MakeExpression (p as KonohaExpr));
				switch (tk.Keyword) {
				case KeywordType.ADD:
					return Expression.Add(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.SUB:
					return Expression.Subtract(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.MUL:
					return Expression.Multiply(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.DIV:
					return Expression.Divide(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.EQ:
					return Expression.Equal(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.NEQ:
					return Expression.NotEqual(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.LT:
					return Expression.LessThan(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.LTE:
					return Expression.LessThanOrEqual(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.GT:
					return Expression.GreaterThan(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.GTE:
					return Expression.GreaterThanOrEqual(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.AND:
					return Expression.And(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.OR:
					return Expression.Or(param.ElementAt (0), param.ElementAt (1));
				case KeywordType.MOD:
					return Expression.Modulo(param.ElementAt (0), param.ElementAt (1));
				}
			} else if (kexpr is TermExpr) {
				switch(kexpr.tk.Type) {
				case TokenType.INT:
					return Expression.Constant(int.Parse(kexpr.tk.Text));
				case TokenType.FLOAT:
					return Expression.Constant(float.Parse(kexpr.tk.Text));
				case TokenType.TEXT:
					return Expression.Constant(kexpr.tk.Text);
				}
			}
			return null;
		}
	}
}
