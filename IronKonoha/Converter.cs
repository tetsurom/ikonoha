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
                    _lambda = converter.ConvertFunc<T>(BlockBody, Params).Compile();
                    this.Invoke = _lambda;
                    Scope[key] = _lambda;
                    Scope = null;
                    key = null;
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

        public IDictionary<string, object> Scope { private get; set; }
        public string key { private get; set; }
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

        private IEnumerable<Expression> ConvertTextBlock(string body, FunctionEnvironment environment, IList<string> param)
		{

			var tokenizer = new Tokenizer(ctx, ks);
			var parser = new Parser(ctx, ks);
			var tokens = tokenizer.Tokenize(body);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
			return ConvertToExprList(block, environment, param);
		}

        public Expression<T> ConvertFunc<T>(string body, IList<string> param)
		{
			var env = new FunctionEnvironment()
			{
				Params = param.Select(p=>Expression.Parameter(typeof(object), p)).ToArray(),
				ReturnLabel = Expression.Label(typeof(object))
			};
			var list = ConvertTextBlock(body, env, param).ToList();
			list.Add(Expression.Label(env.ReturnLabel, KNull));
            return Expression.Lambda<T>(Expression.Block(list), env.Params);
		}

		private Dictionary<int, Type> FuncTypes = new Dictionary<int, Type>
		{
			{0, typeof(Func<>)},
			{1, typeof(Func<,>)},
			{2, typeof(Func<,,>)},
			{3, typeof(Func<,,,>)},
			{4, typeof(Func<,,,,>)},
			{5, typeof(Func<,,,,,>)},
			{6, typeof(Func<,,,,,,>)},
			{7, typeof(Func<,,,,,,,>)},
			{8, typeof(Func<,,,,,,,,>)},
			{9, typeof(Func<,,,,,,,,,>)},
			{10, typeof(Func<,,,,,,,,,,>)},
			{11, typeof(Func<,,,,,,,,,,,>)},
			{12, typeof(Func<,,,,,,,,,,,,>)},
			{13, typeof(Func<,,,,,,,,,,,,,>)},
			{14, typeof(Func<,,,,,,,,,,,,,,>)},
			{15, typeof(Func<,,,,,,,,,,,,,,,>)},
			{16, typeof(Func<,,,,,,,,,,,,,,,,>)},
		};

		private Dictionary<int, Type> ActionTypes = new Dictionary<int, Type>
		{
			{0, typeof(Action)},
			{1, typeof(Action<>)},
			{2, typeof(Action<,>)},
			{3, typeof(Action<,,>)},
			{4, typeof(Action<,,,>)},
			{5, typeof(Action<,,,,>)},
			{6, typeof(Action<,,,,,>)},
			{7, typeof(Action<,,,,,,>)},
			{8, typeof(Action<,,,,,,,>)},
			{9, typeof(Action<,,,,,,,,>)},
			{10, typeof(Action<,,,,,,,,,>)},
			{11, typeof(Action<,,,,,,,,,,>)},
			{12, typeof(Action<,,,,,,,,,,,>)},
			{13, typeof(Action<,,,,,,,,,,,,>)},
			{14, typeof(Action<,,,,,,,,,,,,,>)},
			{15, typeof(Action<,,,,,,,,,,,,,,>)},
			{16, typeof(Action<,,,,,,,,,,,,,,,>)},
		};

		public Expression MakeFuncDeclExpression (Dictionary<object, KonohaExpr> map)
		{
			CodeExpr block = map[Symbols.Block] as CodeExpr;

			var retType = map[Symbol.Get(ctx, "type")].tk;

			var args = GetParamList(map[Symbols.Params] as BlockExpr).ToList();
			var argtypes = args.Select(a => typeof(object)).ToList();

			Type ftype = null;

			if (retType.Keyword == KeywordType.Void)
			{
                if (args.Count > 0)
                {
                    ftype = ActionTypes[args.Count].MakeGenericType(argtypes.ToArray());
                }
                else
                {
                    ftype = ActionTypes[0];
                }
			}
			else
			{
				argtypes.Add(typeof(object));
				ftype = FuncTypes[args.Count].MakeGenericType(argtypes.ToArray());
			}

			Type fctype = typeof(FuncCache<>).MakeGenericType(ftype);

			dynamic cache = fctype.InvokeMember("", System.Reflection.BindingFlags.CreateInstance, null, null,
				new object[] { this, block.tk.Text, args });

			string key = map[Symbols.SYMBOL].tk.Text;
			Scope[key] = cache.Invoke;

            cache.Scope = Scope;
            cache.key = key;

			var t = typeof(int);

			return Expression.Constant(Scope[key]);
		}

		public Expression KStatementToExpr(KStatement st, FunctionEnvironment environment, IList<string> funcargs)
		{
			if (st.syn.KeyWord == KeywordType.If)
			{
				return MakeIfExpression(st.map, environment, funcargs);
			}
			if (st.syn.KeyWord == KeywordType.StmtMethodDecl)
			{
				return MakeFuncDeclExpression(st.map);
			}
			if (st.syn.KeyWord == KeywordType.Return)
			{
				var exp = MakeExpression(st.map.Values.First(), environment, funcargs);
				if (exp.Type != typeof(object))
				{
					exp = Expression.Convert(exp, typeof(object));
				}
				return Expression.Return(environment.ReturnLabel, exp);
			}
			return MakeExpression(st.map.Values.First(), environment, funcargs);
		}

        public IEnumerable<Expression> ConvertToExprList(BlockExpr block, FunctionEnvironment environment, IList<string> funcargs)
		{
            return block.blocks.Select(s => KStatementToExpr(s, environment, funcargs));
		}

		public Expression MakeBlockExpression(KonohaExpr expr, FunctionEnvironment environment, IList<String> param)
		{
			return Expression.Block(ConvertTextBlock(expr.tk.Text, environment, param));
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
			if (expr.Cons[0] is Token)
			{
				Token tk = expr.Cons[0] as Token;
				var param = expr.Cons.Skip(1).Select(p => MakeExpression(p as KonohaExpr, environment, args));
				switch (tk.Type)
				{
					case TokenType.OPERATOR:
						return Expression.Dynamic(GetBinaryBinder(BinaryOperationType[tk.Keyword]),
							typeof(object),
							param.ElementAt(0),
							param.ElementAt(1)
						);
				}
			}
			else
			{
				Token tk = ((KonohaExpr)expr.Cons[0]).tk;
				var f = Scope[tk.Text];

				if (expr.Cons.Count > 2)
				{
					Expression p = MakeExpression((KonohaExpr)expr.Cons[2], environment, args);
					return Expression.Invoke(Expression.Constant(f), p);
				}
				else
				{
					return Expression.Invoke(Expression.Constant(f));
				}
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
