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
		
		public Converter(Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
		}
		
		public Expression<Func<object,object>> Convert(BlockExpr block) 
		{
			List<Expression> list = new List<Expression>();
			foreach(KStatement st in block.blocks) {
				foreach(KonohaExpr kexpr in st.map.Values) {
					list.Add(MakeExpression(kexpr));
				}
			}
			var root = Expression.Convert(Expression.Block(list),typeof(object));
			var e = Expression.Lambda<Func<object, object>>(root,Expression.Parameter(typeof(object)));
			return e;
		}

		public Expression MakeExpression (KonohaExpr kexpr)
		{
			if(kexpr is ConsExpr){
				ConsExpr cexpr = kexpr as ConsExpr;
				Token tk = cexpr.Cons[0] as Token;
				var param = cexpr.Cons.Skip(1).Select(p=>MakeExpression(p as KonohaExpr));
				switch(tk.Keyword) {
				case KeywordType.ADD:
					return Expression.Add(param.ElementAt(0),param.ElementAt(1));
				case KeywordType.EQ:
					return Expression.Equal(param.ElementAt(0),param.ElementAt(1));
				}
			}else if(kexpr is TermExpr) {
				int i = int.Parse(kexpr.tk.Text);
				return Expression.Constant(i);
			}
			return null;
		}
	}
}
