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
	public delegate object FuncLambda(params object[] args);

	abstract class Cache
	{
		protected string BlockBody;
		protected IList<string> Params;
		protected Converter converter;
		protected ParameterExpression paramExpr;

		public Cache(Converter converter, string body, ParameterExpression paramExpr, IList<string> param)
		{
			this.converter = converter;
			this.BlockBody = body;
			this.paramExpr = paramExpr;
			this.Params = param;
		}
	}

	class FuncCache : Cache
	{
		public FuncLambda Lambda { get; protected set; }

		public FuncCache(Converter converter, string body, ParameterExpression paramExpr, IList<string> param)
			:base(converter, body, paramExpr, param){

		}

		public object Invoke(params object[] args)
		{
			if (Lambda == null)
			{
				Console.WriteLine("compile block...");
				var e = converter.ConvertFunc(BlockBody, paramExpr, Params);
				var f = e.Compile();
				Lambda = f;
			}
			else
			{
				//Console.WriteLine("cache hitted & compile skipped.");
			}
			return Lambda(args);
		}
	}

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

		private class SymbolConst
		{
			public readonly Symbol Expr;
			public readonly Symbol Block;
			public readonly Symbol If;
			public readonly Symbol Else;
			public readonly Symbol SYMBOL;
			public readonly Symbol Params;
			internal SymbolConst(Context ctx)
			{
				Expr = Symbol.Get(ctx, "expr");
				Block = Symbol.Get(ctx, "block");
				If = Symbol.Get(ctx, "if");
				Else = Symbol.Get(ctx, "else");
				SYMBOL = Symbol.Get(ctx, "SYMBOL");
				Params = Symbol.Get(ctx, "params");
			}
		}

		readonly SymbolConst Symbols;

		private IDictionary<string, object> Scope { get { return ks.scope as IDictionary<string, object>; } }

		public Converter(Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
			Symbols = new SymbolConst(ctx);
		}
		
		public Expression<Func<object>> Convert (BlockExpr block)
		{
			var root = Expression.Convert(
				Expression.Block(ConvertToExprList(block, null, null)),
				typeof(object));
			return Expression.Lambda<Func<object>>(root);
		}

		private IList<Expression> ConvertBlock(string body, ParameterExpression paramExpr, IList<string> param)
		{
			var tokenizer = new Tokenizer(ctx, ks);
			var parser = new Parser(ctx, ks);
			var tokens = tokenizer.Tokenize(body);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
			return ConvertToExprList(block, paramExpr, param);
		}

		public Expression<FuncLambda> ConvertFunc(string body, ParameterExpression paramExpr, IList<string> param)
		{
			var list = ConvertBlock(body, paramExpr, param);
			var root = Expression.Block(list);
			if (root.Type == typeof(void))
			{
				list.Add(KNull);
				root = Expression.Block(list);
			}
			return Expression.Lambda<FuncLambda>(root, paramExpr);
		}

		public List<Expression> ConvertToExprList(BlockExpr block, ParameterExpression paramExpr, IList<string> funcargs)
		{
			List<Expression> list = new List<Expression>();
			foreach (KStatement st in block.blocks)
			{
				if (st.syn.KeyWord == KeywordType.If)
				{
					list.Add(MakeIfExpression(st.map, paramExpr, funcargs));
				}
				else if (st.syn.KeyWord == KeywordType.StmtMethodDecl)
				{
					list.Add(MakeFuncDeclExpression(st.map));
				}
				else
				{
					foreach (KonohaExpr kexpr in st.map.Values)
					{
						list.Add(MakeExpression(kexpr, paramExpr, funcargs));
					}
				}
			}
			return list;
		}

		public Expression MakeFuncDeclExpression (Dictionary<object, KonohaExpr> map)
		{
			CodeExpr block = map[Symbols.Block] as CodeExpr;

			var args = Expression.Parameter(typeof(object[]), "args");
			var cache = new FuncCache(this, block.tk.Text, args, GetParamList(map[Symbols.Params] as BlockExpr).ToList());

			FuncLambda lambda = p => cache.Invoke(p);

			string key = map[Symbols.SYMBOL].tk.Text;
			Scope[key] = lambda;

			return Expression.Constant(lambda);
		}

		public Expression MakeBlockExpression(KonohaExpr expr, ParameterExpression paramExpr, IList<String> param)
		{
			return Expression.Convert(
				Expression.Block(ConvertBlock(expr.tk.Text, paramExpr, param)),
				typeof(object));
		}

		public IEnumerable<string> GetParamList(BlockExpr args)
		{
			return from stmt in args.blocks
				   select stmt.map[Symbols.Expr].tk.Text;
		}

		public Expression MakeExpression(KonohaExpr kexpr, ParameterExpression paramExpr, IList<String> args)
		{
			if(kexpr is ConsExpr) {
				return MakeConsExpression((ConsExpr)kexpr, paramExpr, args);
			} else if(kexpr is TermExpr) {
				var text = kexpr.tk.Text;
				switch(kexpr.tk.Type) {
				case TokenType.INT:
					return Expression.Constant(long.Parse(text));
				case TokenType.FLOAT:
					return Expression.Constant(double.Parse(text));
				case TokenType.TEXT:
					return Expression.Constant(text);
				case TokenType.SYMBOL:
					return Expression.ArrayIndex(paramExpr, Expression.Constant(args.IndexOf(text)));
				}
			}
			return null;
		}

		public ConditionalExpression MakeIfExpression(Dictionary<dynamic, KonohaExpr> map, ParameterExpression paramExpr, IList<String> args)
		{
			var cond = Expression.Convert(MakeExpression(map[Symbols.Expr], paramExpr, args), typeof(bool));
			var onTrue = MakeBlockExpression(map[Symbols.Block], paramExpr, args);

			if(map.Count() < 3){
				return Expression.Condition(cond, onTrue, KNull);
			}
			return Expression.Condition(cond, onTrue, MakeBlockExpression(map[Symbols.Else], paramExpr, args));
		}

		public Expression MakeConsExpression(ConsExpr expr, ParameterExpression paramExpr, IList<String> args)
		{
			if(expr.Cons[0] is Token){
				Token tk = expr.Cons[0] as Token;
				var param = expr.Cons.Skip(1).Select(p => MakeExpression(p as KonohaExpr, paramExpr, args));
				switch(tk.Type) {
				case TokenType.OPERATOR:
					return Expression.Dynamic(GetBinaryBinder(BinaryOperationType[tk.Keyword]),
						typeof(object),
						Expression.Convert(param.ElementAt(0),typeof(object)),
						Expression.Convert(param.ElementAt(1),typeof(object)));
				}
			}else{
				Token tk = ((KonohaExpr)expr.Cons[0]).tk;
				var f = Scope[tk.Text];
				Expression pa;
				if (expr.Cons.Count > 2)
				{
					Expression[] p = new[] { Expression.Convert(MakeExpression((KonohaExpr)expr.Cons[2], paramExpr, args), typeof(object)) };
					pa = Expression.NewArrayInit(typeof(object), p);
				}
				else
				{
					pa = Expression.Constant(new object[] { });
				}
				return Expression.Invoke(Expression.Constant(f), new[] { pa });
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

	}
}
