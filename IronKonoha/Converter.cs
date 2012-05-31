using System;
using System.Dynamic;
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
		internal static readonly Expression KNull = Expression.Constant(null);
		internal static readonly Expression KTrue = Expression.Constant(true);
		internal static readonly Expression KFalse = Expression.Constant(false);
		//internal static readonly Expression KEmptyString = Expression.Constant(null,typeof(KString));
		
		public Converter (Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
		}
		
		public Expression<Func<object>> Convert (BlockExpr block)
		{
			Expression<Func<object>> b = null;
//			try{
				List<Expression> list = new List<Expression> ();
				foreach(KStatement st in block.blocks) {
					foreach(KonohaExpr kexpr in st.map.Values) {
						list.Add(MakeExpression(kexpr));
					}
				}

				var root = Expression.Convert(Expression.Block(list), typeof(object));
				b = Expression.Lambda<Func<object>>(root);
//			}catch(Exception e){
				//TODO : static error check
//			}
			return b;
		}

		public Expression MakeExpression (KonohaExpr kexpr)
		{
			if(kexpr is ConsExpr) {
				return MakeConsExpression((ConsExpr)kexpr);
			} else if(kexpr is TermExpr) {
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

		public Expression MakeConsExpression (ConsExpr expr)
		{
			Token tk = expr.Cons[0] as Token;
			var param = expr.Cons.Skip(1).Select(p => MakeExpression (p as KonohaExpr));
			switch(tk.Type) {
			case TokenType.OPERATOR:
				return OperatorASM(tk.Keyword,param.ElementAt(0),param.ElementAt(1));
			case TokenType.SYMBOL:
				return SymbolASM(tk.Keyword, param);
			case TokenType.CODE:
				return Expression.Call(typeof(KonohaSpace).GetMethod("RunEval"),param.ElementAt(0)); //TODO
			}
			return null;
		}

		public Expression OperatorASM (KeywordType keyword, Expression left, Expression right)
		{
			switch (keyword) {
			case KeywordType.ADD:
				return Expression.Add(left,right);
			case KeywordType.SUB:
				return Expression.Subtract(left,right);
			case KeywordType.MUL:
				return Expression.Multiply(left,right);
			case KeywordType.DIV:
				return Expression.Divide(left,right);
			case KeywordType.EQ:
				return Expression.Equal(left,right);
			case KeywordType.NEQ:
				return Expression.NotEqual(left,right);
			case KeywordType.LT:
				return Expression.LessThan(left,right);
			case KeywordType.LTE:
				return Expression.LessThanOrEqual(left,right);
			case KeywordType.GT:
				return Expression.GreaterThan(left,right);
			case KeywordType.GTE:
				return Expression.GreaterThanOrEqual(left,right);
			case KeywordType.AND:
				return Expression.And(left,right);
			case KeywordType.OR:
				return Expression.Or(left,right);
			case KeywordType.MOD:
				return Expression.Modulo(left,right);
			case KeywordType.Parenthesis:
				return null; // It will not use in here.
			}
		return null;
		}

		public Expression SymbolASM (KeywordType keyword, IEnumerable<Expression> param)
		{
			switch(keyword) {
			case KeywordType.If:
				return Expression.Condition(param.ElementAt(0), param.ElementAt(1), param.ElementAt(2));
			case KeywordType.Null:
				return KNull;
			}
			return null;
		}
	}
}
