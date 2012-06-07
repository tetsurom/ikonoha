using System;
using System.Dynamic;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;

namespace IronKonoha
{
	/// <summary>
	/// Generate DLR Expression Tree from konoha AST.
	/// </summary>
	public class Converter
	{
		private Context ctx;
		private KonohaSpace ks;
		internal static readonly Expression KNull = Expression.Constant(null);
		internal static readonly Expression KTrue = Expression.Constant(true);
		internal static readonly Expression KFalse = Expression.Constant(false);
		internal static readonly Dictionary<KeywordType,ExpressionType> BinaryOperationType
			 = new Dictionary<KeywordType, ExpressionType>()
			{
				{KeywordType.ADD ,ExpressionType.Add},
				{KeywordType.SUB ,ExpressionType.Subtract},
				{KeywordType.MUL ,ExpressionType.Multiply},
				{KeywordType.DIV ,ExpressionType.Divide},
				{KeywordType.EQ  ,ExpressionType.Equal},
				{KeywordType.NEQ ,ExpressionType.NotEqual},
				{KeywordType.LT  ,ExpressionType.LessThan},
				{KeywordType.LTE ,ExpressionType.LessThanOrEqual},
				{KeywordType.GT  ,ExpressionType.GreaterThan},
				{KeywordType.GTE ,ExpressionType.GreaterThanOrEqual},
				{KeywordType.AND ,ExpressionType.And},
				{KeywordType.OR  ,ExpressionType.Or},
				{KeywordType.MOD ,ExpressionType.Modulo}
			};
		//internal static readonly Expression KEmptyString = Expression.Constant(null,typeof(KString));
		
		public Converter(Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
		}
		
		public Expression<Func<object>> Convert (BlockExpr block)
		{
			Expression<Func<object>> b = null;
//			try{
			var root = Expression.Convert(Expression.Block(ConvertToExprList(block)), typeof(object));
			b = Expression.Lambda<Func<object>>(root);
//			}catch(Exception e){
				//TODO : static error check
//			}
			return b;
		}

		public List<Expression> ConvertToExprList(BlockExpr block)
		{
			List<Expression> list = new List<Expression>();
			foreach (KStatement st in block.blocks)
			{
				if (st.syn.KeyWord == KeywordType.If)
				{
					list.Add(MakeIfExpression(st.map));
				}
				else if (st.syn.KeyWord == KeywordType.StmtMethodDecl)
				{
					list.Add(MakeStmtDeclExpression(st.map));
				}
				else
				{
					foreach (KonohaExpr kexpr in st.map.Values)
					{
						list.Add(MakeExpression(kexpr));
					}
				}
			}
			return list;
		}

		public Expression MakeStmtDeclExpression (Dictionary<object, KonohaExpr> map)
		{
			var scope = (IDictionary<string, dynamic>)this.ks.scope;
			string key = map[Symbol.Get(ctx,"SYMBOL")].tk.Text;
			CodeExpr block = map[Symbol.Get(ctx, "block")] as CodeExpr;

			Expression evalExpr = Expression.Call(
				typeof(Converter).GetMethod("RunEval"),
				Expression.Constant(block.tk.Text, typeof(string)),
				Expression.Constant(ctx, typeof(Context)),
				Expression.Constant(ks, typeof(KonohaSpace)),
				Expression.Constant(MakeParameterExpression(map[Symbol.Get(ctx,"params")]))
			);
			scope[key] = Expression.Lambda<Func<object[], object>>(evalExpr,
				key,true,
				MakeParameterExpression(map[Symbol.Get(ctx,"params")]));
			return scope[key];
		}

		public ParameterExpression[] MakeParameterExpression (KonohaExpr par1)
		{
			var block = par1 as BlockExpr;
			var parameters = from stmt in block.blocks
							 let name = stmt.map[Symbol.Get(ctx, "expr")].tk.Text
							 select Expression.Parameter(typeof(object), name);
			return parameters.ToArray();
		}

		public Expression MakeExpression(KonohaExpr kexpr)
		{
			if(kexpr is ConsExpr) {
				return MakeConsExpression((ConsExpr)kexpr);
			} else if(kexpr is TermExpr) {
				switch(kexpr.tk.Type) {
				case TokenType.INT:
					return Expression.Constant(long.Parse(kexpr.tk.Text));
				case TokenType.FLOAT:
					return Expression.Constant(double.Parse(kexpr.tk.Text));
				case TokenType.TEXT:
					return Expression.Constant(kexpr.tk.Text);
				case TokenType.SYMBOL:
					return Expression.Parameter(typeof(object), kexpr.tk.Text);
				}
			} /*else if(kexpr is CodeExpr) {
				switch(kexpr.tk.Type) {
				case TokenType.CODE:
					return Expression.Call(typeof(Converter).GetMethod("RunEval"),
						Expression.Constant(kexpr.tk.Text,typeof(string)),
						Expression.Constant(ctx,typeof(Context)),
						Expression.Constant(ks,typeof(KonohaSpace)));
				}
			}*/
			return null;
		}

		public Expression MakeIfExpression (Dictionary<dynamic, KonohaExpr> map)
		{
			if(map.Count() == 3) {
				return Expression.Condition(Expression.Convert(MakeExpression(map[Symbol.Get(ctx,"expr")]),typeof(bool)),
					MakeExpression(map[Symbol.Get(ctx,"block")]),
					MakeExpression(map[Symbol.Get(ctx,"else")]));
			}
			return Expression.Condition(Expression.Convert(MakeExpression(map[Symbol.Get(ctx,"expr")]),typeof(bool)),
				MakeExpression(map[Symbol.Get(ctx,"block")]),KNull);
		}

		public Expression MakeConsExpression (ConsExpr expr)
		{
			if(expr.Cons[0] is Token){
				Token tk = expr.Cons[0] as Token;
				var param = expr.Cons.Skip(1).Select(p => MakeExpression (p as KonohaExpr));
				switch(tk.Type) {
				case TokenType.OPERATOR:
					return Expression.Dynamic(GetBinaryBinder(BinaryOperationType[tk.Keyword]),
						typeof(object),
						Expression.Convert(param.ElementAt(0),typeof(object)),
						Expression.Convert(param.ElementAt(1),typeof(object)));
				case TokenType.SYMBOL:
					return SymbolASM(tk.Keyword, param);
				}
			}else{
				Token tk = ((KonohaExpr)expr.Cons[0]).tk;
				var f = ((IDictionary<string,dynamic>)ks.scope)[tk.Text];
				if (expr.Cons.Count > 2)
				{
					Expression[] args = new[] { Expression.Convert(MakeExpression((KonohaExpr)expr.Cons[2]), typeof(object)) };
					return Expression.Invoke(f, args);
				}
				else
				{
					return Expression.Invoke(f, null);
				}
				//MakeExpression((KonohaExpr)expr.Cons[2])); //TODO parameters.
			}
			return null;
		}

		public dynamic GetBinaryBinder(ExpressionType et)
		{
			return Binder.BinaryOperation(
				CSharpBinderFlags.None, et, typeof(Converter),
				new CSharpArgumentInfo[] {
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
		}

		public Expression SymbolASM (KeywordType keyword, IEnumerable<Expression> param)
		{
			switch(keyword) {
			case KeywordType.Null:
				return KNull;
			}
			return null;
		}

		public static BlockExpression RunEval(string script,Context ctx, KonohaSpace ks, ParameterExpression[] args)
		{
//			KonohaSpace ks = new KonohaSpace(ctx,1);
//			ks.parent = ksparent;
			var tokenizer = new Tokenizer(ctx, ks);
			var parser = new Parser(ctx, ks);
			var converter = new Converter(ctx,ks);
			var tokens = tokenizer.Tokenize(script);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
			var list = converter.ConvertToExprList(block);
			var funcbody = Expression.Convert(Expression.Block(list), typeof(object));
			var funcdecl = Expression.Block(typeof(object), args, funcbody);
			return funcdecl;
		}

	}
}
