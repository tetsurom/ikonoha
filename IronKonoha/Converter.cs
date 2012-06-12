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
	//public delegate object FuncLambda(params object[] args);

	abstract class Cache
	{
		protected string BlockBody;
		protected IList<string> Params;
		protected Converter converter;

		public Cache(Converter converter, string body, IList<string> param)
		{
			this.converter = converter;
			this.BlockBody = body;
			this.Params = param;
		}
	}

	public class FunctionEnvironment
	{
		public ParameterExpression[] Params { get; set; }
		public LabelTarget ReturnLabel { get; set; }
	}

	class FuncCache<T> : Cache
	{
		private T _lambda;
		public T Lambda
		{
			get
			{
				if (_lambda == null)
				{
					var e = converter.ConvertFunc<T>(BlockBody, Params);
					var f = e.Compile();
					_lambda = f;
				}
				return _lambda;
			}
		}

		public T Invoke { get; private set; }

		public FuncCache(Converter converter, string body, IList<string> param)
			:base(converter, body, param){
			var paramexprs = Params.Select(p => Expression.Parameter(typeof(object), p)).ToArray();
			var bodyexpr = Expression.Block(
				Expression.Invoke(
					Expression.MakeMemberAccess(Expression.Constant(this), this.GetType().GetProperty("Lambda")),
					paramexprs));
			var lmd = Expression.Lambda<T>(bodyexpr, paramexprs);
			Invoke = lmd.Compile();
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
		
		public Expression Convert (BlockExpr block)
		{
			return Expression.Lambda(Expression.Block(ConvertToExprList(block, null, null)));
		}

		private IList<Expression> ConvertBlock(string body, FunctionEnvironment environment, IList<string> param)
		{

			var tokenizer = new Tokenizer(ctx, ks);
			var parser = new Parser(ctx, ks);
			var tokens = tokenizer.Tokenize(body);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
			return ConvertToExprList(block, environment, param);
		}

		public dynamic ConvertFunc<T>(string body, IList<string> param)
		{
			var env = new FunctionEnvironment()
			{
				Params = param.Select(p=>Expression.Parameter(typeof(object), p)).ToArray(),
				ReturnLabel = Expression.Label(typeof(object))
			};
			var list = ConvertBlock(body, env, param);
			list.Add(Expression.Label(env.ReturnLabel, KNull));
			Expression root = Expression.Block(list);
			return Expression.Lambda<T>(root, env.Params);
		}

		public List<Expression> ConvertToExprList(BlockExpr block, FunctionEnvironment environment, IList<string> funcargs)
		{
			List<Expression> list = new List<Expression>();
			foreach (KStatement st in block.blocks)
			{
				if (st.syn.KeyWord == KeywordType.If)
				{
					list.Add(MakeIfExpression(st.map, environment, funcargs));
				}
				else if (st.syn.KeyWord == KeywordType.StmtMethodDecl)
				{
					list.Add(MakeFuncDeclExpression(st.map));
				}
				else if (st.syn.KeyWord == KeywordType.Return)
				{
					var exp = MakeExpression(st.map.Values.First(), environment, funcargs);
					if(exp.Type != typeof(object)){
						exp = Expression.Convert(exp, typeof(object));
					}
					list.Add(Expression.Return(environment.ReturnLabel, exp));
				}
				else
				{
					foreach (KonohaExpr kexpr in st.map.Values)
					{
						list.Add(MakeExpression(kexpr, environment, funcargs));
					}
				}
			}
			return list;
		}

		public Expression MakeFuncDeclExpression (Dictionary<object, KonohaExpr> map)
		{
			CodeExpr block = map[Symbols.Block] as CodeExpr;

			var ftype = typeof(MulticastDelegate);
			var gtype = ftype.MakeGenericType(new []{typeof(object), typeof(object)});
			
			var cache = new FuncCache<Func<object, object>>(this, block.tk.Text, GetParamList(map[Symbols.Params] as BlockExpr).ToList());

			string key = map[Symbols.SYMBOL].tk.Text;
			Scope[key] = cache.Invoke;

			var t = typeof(int);

			return Expression.Constant(Scope[key]);
		}

		public Expression MakeBlockExpression(KonohaExpr expr, FunctionEnvironment environment, IList<String> param)
		{
			return Expression.Block(ConvertBlock(expr.tk.Text, environment, param));
		}

		public IEnumerable<string> GetParamList(BlockExpr args)
		{
			return from stmt in args.blocks
				   select stmt.map[Symbols.Expr].tk.Text;
		}

		public Expression MakeExpression(KonohaExpr kexpr, FunctionEnvironment environment, IList<String> args)
		{
			if(kexpr is ConsExpr) {
				return MakeConsExpression((ConsExpr)kexpr, environment, args);
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
					if (args.Contains(text))
					{
						return environment.Params[args.IndexOf(text)];
					}
					return KNull;
				}
			}
			return null;
		}

		public ConditionalExpression MakeIfExpression(Dictionary<dynamic, KonohaExpr> map, FunctionEnvironment environment, IList<String> args)
		{
			Expression cond = MakeExpression(map[Symbols.Expr], environment, args);
			if(cond.Type != typeof(bool)){
				cond = Expression.Convert(cond, typeof(bool));
			}
			var onTrue = MakeBlockExpression(map[Symbols.Block], environment, args);

			if(map.Count() < 3){
				return Expression.IfThen(cond, onTrue);
			}
			return Expression.IfThenElse(cond, onTrue, MakeBlockExpression(map[Symbols.Else], environment, args));
		}

		public Expression MakeConsExpression(ConsExpr expr, FunctionEnvironment environment, IList<String> args)
		{
			if(expr.Cons[0] is Token){
				Token tk = expr.Cons[0] as Token;
				var param = expr.Cons.Skip(1).Select(p => MakeExpression(p as KonohaExpr, environment, args));
				switch(tk.Type) {
				case TokenType.OPERATOR:
					return Expression.Dynamic(GetBinaryBinder(BinaryOperationType[tk.Keyword]),
						typeof(object),
						param.ElementAt(0),
						param.ElementAt(1)
					);
				}
			}else{
				Token tk = ((KonohaExpr)expr.Cons[0]).tk;
				var f = Scope[tk.Text];
				//Type ty = f.GetType();
				/*
				if (ty == typeof(FuncLambda))
				{
					Expression paramExpressions;
					if (expr.Cons.Count > 2)
					{
						Expression[] p = new[] { MakeExpression((KonohaExpr)expr.Cons[2], environment, args) };
						paramExpressions = Expression.NewArrayInit(typeof(object), p);
					}
					else
					{
						paramExpressions = Expression.Constant(new object[] { });
					}
					return Expression.Invoke(Expression.Constant(f), new[] { paramExpressions });
				}
				else
				{*/
					if (expr.Cons.Count > 2)
					{
						//var tyarg = ty.GetGenericArguments();
						Expression p = MakeExpression((KonohaExpr)expr.Cons[2], environment, args);
						return Expression.Invoke(Expression.Constant(f), p);
					}
					else
					{
						return Expression.Invoke(Expression.Constant(f));
					}
				//}
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
