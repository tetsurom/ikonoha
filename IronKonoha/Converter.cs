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
	public delegate object BlockLambda();

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

	class BlockCache : Cache
	{
		public FuncLambda Lambda { get; protected set; }

		public BlockCache(Converter converter, string body, ParameterExpression paramExpr, IList<string> param)
			: base(converter, body, paramExpr, param)
		{

		}

		public object Invoke(params object[] args)
		{
			if (Lambda == null)
			{
				Console.WriteLine("compile block...");
				var e = converter.ConvertNoNameBlock(BlockBody, paramExpr, Params);
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

		public Converter(Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
			Symbols = new SymbolConst(ctx);
		}
		
		public Expression<Func<object>> Convert (BlockExpr block)
		{
			Expression<Func<object>> b = null;
//			try{
			var root = Expression.Convert(Expression.Block(ConvertToExprList(block, null, null)), typeof(object));
			b = Expression.Lambda<Func<object>>(root);
//			}catch(Exception e){
				//TODO : static error check
//			}
			return b;
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
			var root = Expression.Convert(Expression.Block(list), typeof(object));
			return Expression.Lambda<FuncLambda>(root, paramExpr);
		}

		public Expression<FuncLambda> ConvertNoNameBlock(string body, ParameterExpression paramExpr, IList<string> param)
		{
			var args = Expression.Parameter(typeof(object[]));
			var list = ConvertBlock(body, args, param);
			var root = Expression.Convert(Expression.Block(list), typeof(object));
			return Expression.Lambda<FuncLambda>(root, args);
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
			string key = map[Symbols.SYMBOL].tk.Text;
			var scope = this.ks.scope as IDictionary<string, object>;

			var args = Expression.Parameter(typeof(object[]), "args");

			CodeExpr block = map[Symbols.Block] as CodeExpr;
			var cache = new FuncCache(this, block.tk.Text, args, GetParamList(map[Symbols.Params] as BlockExpr).ToList());

			FuncLambda lambda = (object[] p) =>
			{
				return cache.Invoke(p);
			};

			scope[key] = lambda;

			return Expression.Constant(lambda);
		}

		public Expression MakeBlockExpression(KonohaExpr expr, ParameterExpression paramExpr, IList<String> param)
		{
			var cache = new BlockCache(this, expr.tk.Text, paramExpr, param);
			FuncLambda lambda = (object[] p) =>
			{
				return cache.Invoke(p);
			};
			return Expression.Constant(lambda);
		}

		public IEnumerable<string> GetParamList(BlockExpr args)
		{
			return from stmt in args.blocks
				   select stmt.map[Symbols.Expr].tk.Text;
		}

		/*
		public ParameterExpression[] MakeParameterExpression (KonohaExpr par1)
		{
			var block = par1 as BlockExpr;
			var parameters = from stmt in block.blocks
							 let name = stmt.map[Symbols.Expr].tk.Text
							 select Expression.Parameter(typeof(object), name);
			return parameters.ToArray();
		}
		*/

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
					//return Expression.Parameter(typeof(object), kexpr.tk.Text);
				}
			}
			return null;
		}

		public Expression MakeIfExpression(Dictionary<dynamic, KonohaExpr> map, ParameterExpression paramExpr, IList<String> args)
		{
			if(map.Count() == 3) {
				return Expression.Condition(
					Expression.Convert(MakeExpression(map[Symbols.Expr], paramExpr, args), typeof(bool)),
					Expression.Invoke(MakeBlockExpression(map[Symbols.Block], null, args), paramExpr),
					Expression.Invoke(MakeBlockExpression(map[Symbols.Else], null, args), paramExpr)
				);
			}
			return Expression.Condition(
				Expression.Convert(MakeExpression(map[Symbols.Expr], paramExpr, args), typeof(bool)),
				Expression.Invoke(MakeBlockExpression(map[Symbols.Block], null, args), paramExpr),
				KNull);
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
				var f = (ks.scope as IDictionary<string, object>)[tk.Text];
				Expression[] prm;
				if (expr.Cons.Count > 2)
				{
					Expression[] p = new[] { Expression.Convert(MakeExpression((KonohaExpr)expr.Cons[2], paramExpr, args), typeof(object)) };
					var pa = Expression.NewArrayInit(typeof(object), p);
					prm = new[] { pa };
				}
				else
				{
					prm = new[] { Expression.Constant(new object[] { }) };
				}
				return Expression.Invoke(Expression.Constant(f), prm);
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

	}
}
